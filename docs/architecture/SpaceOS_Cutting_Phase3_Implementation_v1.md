# SpaceOS — Cutting Phase 3 Implementation Spec
## Rendelés ingestion · Geometry bin-packing · Inventory-aware nesting

> **Verzió:** v1.0 — 2026-04-25
> **Státusz:** DRAFT — Architect spec, Gábor approval szükséges
> **Előfeltétel:** Cutting Phase 1+2 DEPLOYED (284 teszt), Nesting.Algorithms NuGet v1.1.0
> **Alapspec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (Sessions B+C)
> **Referencia:** CUTTING_CONTEXT.md, Nesting.Algorithms modellek, Contracts v1.3.0

---

## 0. Jelenlegi állapot és gap elemzés

### Ami DEPLOYED (Phase 1+2)

| Komponens | Állapot | Fájl |
|-----------|---------|------|
| CuttingPlan FSM (Draft→Published→Frozen→Closed) | DEPLOYED | `CuttingPlan.cs` |
| DaySlot entity (Open→Locked→Closed) | DEPLOYED | `DaySlot.cs` |
| CuttingJob (`OrderId`, `WidthMm=0`, `HeightMm=0`) | DEPLOYED | `CuttingJob.cs` |
| 4 IPlanningStrategy (MaxCut, FIFO, Priority, Custom) | DEPLOYED | `Application/Strategies/` |
| INestingStrategy integráció (FFDH + Guillotine) | DEPLOYED | via NuGet |
| PlanNestingSnapshot (JSONB nesting result) | DEPLOYED | `PlanNestingSnapshot.cs` |
| RegisterOffcutsOnPlanFrozenHandler | DEPLOYED | offcut regisztráció Inventory-ba |
| ContractsInventoryHttpAdapter (ReserveAsync, ReleaseAsync) | DEPLOYED | Inventory HTTP adapter |
| PanelReservation aggregate | DEPLOYED | Reservation tracking |
| CuttingJobCompletedEvent → Inventory | DEPLOYED | HTTP event bus |

### Ami HIÁNYZIK (Phase 3)

| Gap | Probléma | Hatás |
|-----|----------|-------|
| **Nincs rendelés ingestion** | CuttingJob-okat manuálisan kell létrehozni, `WidthMm=0, HeightMm=0` | Nesting geometria nélkül nem tud valós optimalizálást futtatni |
| **Nincs order→CuttingJob mapping** | Joinery DoorOrder tételek nem jutnak el a Cutting modulba | Nincs cross-module adatáramlás |
| **Nesting geometria nincs bekötve** | `INestingStrategy.ComputeAsync()` csak üres dimenziókat kap | A `PlanNestingSnapshot` tartalom üres/dummy |
| **Panel forrás nincs bekötve** | A nesting nem kap `AvailablePanel`-eket az Inventory-ból | Nesting nem tud táblákat allokálni |

---

## 1. Rendelés ingestion flow (Q1)

### 1.1 Döntés: API-based (nem event-based)

| Opció | Leírás | Előny | Hátrány |
|-------|--------|-------|---------|
| **A) API-based: internal endpoint** | Orchestrator hívja `POST /internal/ingest-order` → Cutting | Szinkron, közvetlen, hibakezelés egyszerű; meglévő `X-SpaceOS-Internal` guard minta | Szoros coupling (de loopback-on) |
| B) Event-based (MediatR cross-module) | Joinery domain event → Orchestrator közvetít → Cutting handler | Lazy coupling | Nincs cross-module event bus infrastruktúra; Orchestrator nem MediatR |
| C) Shared outbox/event bus | Kafka/RabbitMQ style | Robusztus | Overkill a jelenlegi skálánál; új infrastruktúra |

**Döntés: Opció A** — a meglévő `InternalEndpoints.cs` mintáját követi (`X-SpaceOS-Internal` header guard + tenant-aware GUC).

### 1.2 Ingestion flow

```
Joinery: DoorOrder 'Calculated' állapot
  → Orchestrator BFF: POST /bff/joinery/orders/{id}/schedule-cutting
    → Orchestrator: hívja Joinery-t az order items-ért
    → Orchestrator: hívja Cutting-ot: POST /internal/ingest-order
      → Cutting: IngestOrderCommand → CuttingJob-ok létrehozása gépi dimenzióval
```

### 1.3 Alternatív egyszerűbb flow (ajánlott v1-ben)

```
Orchestrator BFF: POST /bff/cutting/planning/{planId}/add-order
  → Orchestrator: GET /bff/joinery/orders/{orderId} → order details + items
  → Orchestrator: POST http://127.0.0.1:5005/internal/ingest-order
      body: { planId, orderId, tenantId, items: [{ widthMm, heightMm, materialCode, quantity, grainDirection }] }
  → Cutting: IngestOrderCommandHandler → CuttingJob-ok létrehozása
```

**Miért az Orchestrator közvetít?**
- A Cutting modul NEM ismeri a Joinery DoorOrder struktúrát (Golden Rule #2: Modular Monolith)
- Az Orchestrator már proxy-zik Joinery → Cutting irányba
- Az Orchestrator JSON-t transzformál (Joinery order items → Cutting ingest DTO)

### 1.4 Ingestion endpoint (Cutting modul)

```csharp
// Api/Endpoints/InternalEndpoints.cs — BŐVÍTÉS

app.MapPost("/internal/ingest-order", IngestOrder)
    .AllowAnonymous();  // X-SpaceOS-Internal header guard

// Request DTO:
public sealed record IngestOrderRequest(
    Guid PlanId,
    Guid OrderId,
    Guid TenantId,
    string Priority,        // "Urgent"|"High"|"Normal"|"Low"
    IReadOnlyList<IngestOrderItem> Items
);

public sealed record IngestOrderItem(
    string Name,            // "Oldallap bal"
    decimal WidthMm,        // 500
    decimal HeightMm,       // 720
    string MaterialCode,    // "MDF-18"
    int Quantity,           // 2
    string GrainDirection   // "vertical"|"horizontal"|"none"
);
```

### 1.5 IngestOrderCommandHandler

```csharp
// 1. Validáció: planId létezik, Draft állapotban van
// 2. OrderId duplikáció check: ha már van CuttingJob ezzel az OrderId-val → skip
// 3. Minden IngestOrderItem-ből CuttingJob.Create():
//    - DaySlotId: IPlanningStrategy.ScheduleJobsAsync() választja ki a DaySlot-ot
//    - WidthMm, HeightMm: az order item méretei
//    - EstimatedTimeHours: item.WidthMm * item.HeightMm * item.Quantity / AREA_PER_HOUR_CONSTANT
// 4. DaySlot.AddJob() minden job-ra
// 5. SaveChangesAsync
```

---

## 2. Geometry bin-packing integráció (Q2)

### 2.1 Jelenlegi nesting infrastruktúra

Az `INestingStrategy` (NuGet) már be van kötve:
- `ComputeAsync(NestingInput)` → `NestingResult`
- `NestingInput` = `Parts[]` + `Panels[]` + `SawBladeGapMm`
- `NestingResult` = `PanelAssignment[]` + `UnplacedParts[]` + `TotalWastePercentage`
- `PlanNestingSnapshot` tárolja a JSON eredményt

### 2.2 Input mapping: CuttingJob → NestingPart

```csharp
// Application/Services/NestingInputMapper.cs

public static class NestingInputMapper
{
    /// <summary>
    /// Maps CuttingJobs (with real dimensions from ingestion) to NestingParts.
    /// Expands Quantity: 1 CuttingJob with Quantity=2 → 2 NestingParts.
    /// </summary>
    public static IReadOnlyList<NestingPart> MapJobsToParts(
        IEnumerable<CuttingJob> jobs,
        IReadOnlyDictionary<Guid, IngestOrderItem> orderItemLookup)
    {
        var parts = new List<NestingPart>();
        foreach (var job in jobs)
        {
            // CuttingJob.WidthMm és HeightMm Phase 3 után már kitöltve
            if (job.WidthMm <= 0 || job.HeightMm <= 0)
                continue;  // skip Phase 1/2 legacy jobs without geometry

            parts.Add(new NestingPart(
                PartId: job.Id.ToString(),
                Name: $"Order-{job.OrderId}",
                WidthMm: job.WidthMm,
                HeightMm: job.HeightMm,
                CanRotate: true  // default; GrainDirection="none" → true, egyébként false
            ));
        }
        return parts;
    }
}
```

**Grain direction kezelés:**
- `GrainDirection = "none"` → `CanRotate = true` (az alkatrész forgatható)
- `GrainDirection = "vertical" | "horizontal"` → `CanRotate = false` (az erezet iránya miatt nem forgatható)

### 2.3 Panel forrás: Inventory → AvailablePanel

```csharp
// Application/Services/PanelSourceService.cs

public sealed class PanelSourceService
{
    private readonly IInventoryProvider _inventory;
    
    /// <summary>
    /// Fetches available panels (full + offcuts) from Inventory for a given material.
    /// Maps to NestingInput.Panels.
    /// </summary>
    public async Task<IReadOnlyList<AvailablePanel>> GetPanelsForMaterialAsync(
        string materialCode, CancellationToken ct)
    {
        var panels = new List<AvailablePanel>();
        
        // 1. Full panels from stock
        var stockResult = await _inventory.GetStockAsync(materialCode, ct);
        if (stockResult.IsSuccess)
        {
            foreach (var item in stockResult.Value)
            {
                panels.Add(new AvailablePanel(
                    PanelId: item.Id.ToString(),
                    MaterialCode: materialCode,
                    WidthMm: item.WidthMm,
                    HeightMm: item.HeightMm,
                    IsOffcut: false
                ));
            }
        }

        // 2. Usable offcuts (≥400mm both dimensions)
        var offcutResult = await _inventory.GetUsableOffcutsAsync(
            materialCode, 400m, 400m, ct);
        if (offcutResult.IsSuccess)
        {
            foreach (var offcut in offcutResult.Value)
            {
                panels.Add(new AvailablePanel(
                    PanelId: offcut.Id.ToString(),
                    MaterialCode: materialCode,
                    WidthMm: offcut.WidthMm,
                    HeightMm: offcut.HeightMm,
                    IsOffcut: true  // offcut prioritás a nesting algorithmban
                ));
            }
        }

        return panels;
    }
}
```

### 2.4 Output mapping: NestingResult → PlanNestingSnapshot

Ez már DEPLOYED. A `PlanNestingSnapshot.Create(planId, tenantId, JsonSerializer.Serialize(result))` tárolja az eredményt. A `RegisterOffcutsOnPlanFrozenHandler` olvassa a waste piece-eket Freeze-kor.

### 2.5 Nesting trigger — Publish transition

A Publish handler meglévő logikája bővül:

```csharp
// PublishCuttingPlanCommandHandler (meglévő, bővítendő)

// 1. plan.Publish(snapshotId)
// 2. ÚJ: Nesting futtatás minden DaySlot-ra
//    a. CuttingJob-ok összegyűjtése materialCode szerint
//    b. Minden materialCode-ra: PanelSourceService.GetPanelsForMaterialAsync()
//    c. NestingInputMapper.MapJobsToParts()
//    d. INestingStrategy.ComputeAsync(new NestingInput(parts, panels))
//    e. PlanNestingSnapshot.Create(planId, tenantId, resultJson)
// 3. IInventoryProvider.ReserveAsync() a használt panelek alapján
```

---

## 3. DaySlot → CuttingPlan lifecycle (Q3)

### Teljes lifecycle a Phase 3 ingestion-nel

```
1. CREATE CuttingPlan (Draft)
   → DaySlot-ok létrehozása (Open)
   → IPlanningStrategy kiválasztása

2. INGEST ORDER (Draft állapotban, többször hívható)
   → POST /internal/ingest-order
   → CuttingJob-ok létrehozása valós dimenzióval
   → IPlanningStrategy.ScheduleJobsAsync() → DaySlot hozzárendelés

3. PUBLISH (Draft → Published)
   → INestingStrategy.ComputeAsync() minden DaySlot-ra
   → PanelSourceService → Inventory stock + offcut lekérdezés
   → NestingResult → PlanNestingSnapshot mentés
   → IInventoryProvider.ReserveAsync() a szükséges panelek alapján
   → ProfileSnapshotId mentés

4. FREEZE (Published → Frozen)
   → DaySlot.Lock() mind
   → PanelReservation.MarkConsumed()
   → RegisterOffcutsOnPlanFrozenHandler → offcut-ok regisztrálása Inventory-ba

5. JOB COMPLETION (Frozen állapotban)
   → CuttingJob.MarkAsCut()
   → CuttingJobCompletedEvent → Inventory (HTTP event bus)

6. CLOSE (Frozen → Closed)
   → DaySlot.CloseSlot() mind
   → Reservation release (ha maradt active)
```

---

## 4. Inventory integráció (Q4)

### 4.1 Meglévő integráció (Phase 2 — DEPLOYED)

| Funkció | Adapter | Állapot |
|---------|---------|--------|
| `ReserveAsync()` | `ContractsInventoryHttpAdapter` | DEPLOYED |
| `ReleaseReservationAsync()` | `ContractsInventoryHttpAdapter` | DEPLOYED |
| `RegisterOffcutsAsync()` | `InventoryCuttingHttpAdapter` | DEPLOYED |
| `CuttingJobCompletedEvent` → Inventory | `CuttingEventPublisher` | DEPLOYED |

### 4.2 Phase 3 bővítés

| Funkció | Adapter | Állapot | Phase 3 munka |
|---------|---------|---------|---------------|
| `GetStockAsync()` | `ContractsInventoryHttpAdapter` | **NotSupportedException** → implementálandó | HTTP GET `/api/inventory/stock?material=...` |
| `GetUsableOffcutsAsync()` | `ContractsInventoryHttpAdapter` | **NotSupportedException** → implementálandó | HTTP GET `/api/inventory/offcuts?material=...&minWidth=...&minHeight=...` |

**A `ContractsInventoryHttpAdapter` két metódusa eddig `NotSupportedException`-t dob** — Phase 3-ban implementálni kell, mert a `PanelSourceService` hívja őket.

### 4.3 Inventory endpoint elérhetőség

Ellenőrizni kell hogy az Inventory modul (port 5004) rendelkezik-e GET stock + GET offcuts endpoint-okkal. A `IInventoryProvider` Contracts interfész definiálja:
- `GetStockAsync(materialCode)` → `IReadOnlyList<StockItemDto>`
- `GetUsableOffcutsAsync(materialCode, minWidth, minHeight)` → `IReadOnlyList<StockItemDto>`

Ha az Inventory API-ban ezek hiányoznak → **BLOKKOLÓ: Inventory bővítés szükséges.**

### 4.4 PanelReservation flow (Phase 3 context)

```
Publish:
  → Nesting kiszámolja melyik panel-eket kell felhasználni
  → ReserveAsync(CorrelationId=PlanId, Items=[panelId1, panelId2, ...], Ttl=24h)
  → PanelReservation.Create(planId, tenantId, inventoryReservationId)

Freeze:
  → PanelReservation.MarkConsumed()
  → Panelek "lefoglalt" státuszba kerülnek az Inventory-ban

Close:
  → Ha maradt active reservation (pl. nem minden panel felhasznált) → Release
```

---

## 5. CuttingJob bővítés

### 5.1 Új mezők

```csharp
public class CuttingJob
{
    // MEGLÉVŐ (Phase 1+2):
    public Guid Id { get; private set; }
    public Guid DaySlotId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public string Priority { get; private set; } = "Normal";
    public decimal EstimatedTimeHours { get; private set; }
    public string Status { get; private set; } = "Pending";
    public decimal WidthMm { get; private set; }          // Phase 2: default 0
    public decimal HeightMm { get; private set; }         // Phase 2: default 0

    // ÚJ (Phase 3):
    public string? MaterialCode { get; private set; }     // "MDF-18"
    public string? PartName { get; private set; }         // "Oldallap bal"
    public int Quantity { get; private set; } = 1;        // darabszám
    public string GrainDirection { get; private set; } = "none";  // "vertical"|"horizontal"|"none"
}
```

### 5.2 Migration

```sql
ALTER TABLE spaceos_cutting."CuttingJobs"
    ADD COLUMN "MaterialCode" varchar(50) NULL,
    ADD COLUMN "PartName" varchar(200) NULL,
    ADD COLUMN "Quantity" integer NOT NULL DEFAULT 1,
    ADD COLUMN "GrainDirection" varchar(20) NOT NULL DEFAULT 'none';
```

---

## 6. Orchestrator bővítés

### 6.1 Új BFF route: order → cutting ingestion

```typescript
// src/routes/cutting.route.ts — ÚJ route

// POST /bff/cutting/planning/{planId}/add-order/{orderId}
// 1. GET Joinery order details: http://127.0.0.1:5002/api/orders/{orderId}
// 2. Transform Joinery response → Cutting ingest DTO
// 3. POST http://127.0.0.1:5005/internal/ingest-order
//    Header: X-SpaceOS-Internal: true
//    Body: { planId, orderId, tenantId, priority, items: [...] }
```

### 6.2 Joinery → Cutting mapping (Orchestrator-ban)

```typescript
// Joinery DoorOrder response → Cutting IngestOrderRequest

interface JoineryOrderItem {
  id: string;
  name: string;          // "Beltéri ajtó 80x200"
  widthMm: number;
  heightMm: number;
  material: string;      // "MDF 18mm"
  quantity: number;
  grainDirection: string;
}

function mapToIngestRequest(
  planId: string,
  orderId: string,
  tenantId: string,
  order: JoineryOrderResponse
): IngestOrderRequest {
  return {
    planId,
    orderId,
    tenantId,
    priority: order.priority ?? 'Normal',
    items: order.items.map(item => ({
      name: item.name,
      widthMm: item.widthMm,
      heightMm: item.heightMm,
      materialCode: item.material,
      quantity: item.quantity,
      grainDirection: item.grainDirection ?? 'none',
    })),
  };
}
```

**Nyitott kérdés:** A Joinery DoorOrder items API pontosan milyen mezőket ad vissza? Ellenőrizni kell a Joinery `GET /api/orders/{id}` response-t — különösen a `widthMm`, `heightMm`, `material`, `grainDirection` mezők jelenlétét.

---

## 7. Fázisolás (Q5)

### Phase 3 MVP — valós ingestion + nesting

| # | Feature | Modul | Effort |
|---|---------|-------|--------|
| 1 | CuttingJob bővítés (MaterialCode, PartName, Quantity, GrainDirection) + migration | CUTTING | 0.5 nap |
| 2 | `POST /internal/ingest-order` endpoint + IngestOrderCommandHandler | CUTTING | 1.5 nap |
| 3 | NestingInputMapper (CuttingJob → NestingPart, grain→canRotate) | CUTTING | 0.5 nap |
| 4 | PanelSourceService (Inventory → AvailablePanel) | CUTTING | 0.5 nap |
| 5 | ContractsInventoryHttpAdapter: GetStockAsync + GetUsableOffcutsAsync implementálás | CUTTING | 0.5 nap |
| 6 | PublishCuttingPlanCommandHandler bővítés (nesting trigger + panel source) | CUTTING | 1.5 nap |
| 7 | Orchestrator: `POST /bff/cutting/planning/{planId}/add-order/{orderId}` route | ORCH | 1.0 nap |
| 8 | Unit tesztek (ingestion, mapping, nesting integration, panel source) | CUTTING | 2.0 nap |
| 9 | Integration teszt (Testcontainers: ingest → publish → nesting → snapshot) | CUTTING | 1.0 nap |
| 10 | E2E: Joinery order → Cutting ingest → Publish → Nesting result | E2E | 1.0 nap |
| **Phase 3 MVP total** | | | **10.0 nap** |

### Phase 3.5 — post-launch enhancements

| # | Feature | Effort | Trigger |
|---|---------|--------|---------|
| 11 | Nesting re-run (ReNest after order change) | 1.5 nap | Production feedback |
| 12 | Multi-material nesting (group by materialCode, separate nesting per group) | 1.0 nap | 2+ material type orders |
| 13 | Portal UI: CuttingPlan nesting vizualizáció (SVG) | 2.0 nap | Doorstar Portal Phase 3 |
| 14 | Nesting comparison (FFDH vs Guillotine side-by-side) | 1.0 nap | Operator kérés |
| 15 | CuttingJob quantity expansion (1 job qty=2 → 2 separate NestingParts) | 0.5 nap | Pontos nesting |
| **Phase 3.5 total** | | **~6.0 nap** | |

---

## 8. Effort összesítő (Q6)

| Modul | Phase 3 MVP | Phase 3.5 |
|-------|-------------|-----------|
| CUTTING | 7.0 nap | 4.0 nap |
| ORCH | 1.0 nap | — |
| E2E | 1.0 nap | — |
| CUTTING tesztek | 1.0 nap | — |
| **Total** | **10.0 nap** | **~6.0 nap** |

---

## 9. Végrehajtási sorrend (Q7)

### Dependency chain

```
[Inventory endpoint ellenőrzés]     ← BLOKKOLÓ ha hiányzik
         │
         ▼
CUTTING (#1: CuttingJob migration + mezők)
         │
         ├── CUTTING (#2: ingest endpoint + handler)
         │           │
         │           ├── CUTTING (#3: NestingInputMapper)
         │           │
         │           └── CUTTING (#4: PanelSourceService)
         │                       │
         │                       └── CUTTING (#5: Inventory adapter impl)
         │
         └── CUTTING (#6: PublishHandler bővítés — #3,#4,#5 DONE után)
                     │
                     └── ORCH (#7: BFF route)
                                 │
                                 └── CUTTING (#8-9: unit + integration tesztek)
                                             │
                                             └── E2E (#10: teljes flow)
```

### Napi ütemterv

| Nap | Feladat | Track |
|-----|---------|-------|
| 1 | Inventory API ellenőrzés: GET stock + GET offcuts endpointok léteznek-e | PRE-CHECK |
| 1-1.5 | CuttingJob bővítés + migration + ContractsInventoryHttpAdapter impl | CUTTING |
| 2-3 | IngestOrderCommand + handler + endpoint + validáció | CUTTING |
| 3.5 | NestingInputMapper + PanelSourceService | CUTTING |
| 4-5 | PublishCuttingPlanCommandHandler bővítés (nesting trigger) | CUTTING |
| 6 | Orchestrator BFF route + Joinery→Cutting mapping | ORCH |
| 7-8 | Unit tesztek (ingestion, mapping, nesting, panel source) | CUTTING |
| 9 | Integration teszt (Testcontainers) | CUTTING |
| 10 | E2E happy path + deploy | E2E |

---

## 10. Blokkoló előfeltételek

| # | Előfeltétel | Állapot | Hatás ha hiányzik |
|---|-------------|---------|-------------------|
| 1 | Inventory modul: `GET /api/inventory/stock?material=...` endpoint | **ELLENŐRIZNI** | PanelSourceService nem tud paneleket lekérni → nesting nem fut |
| 2 | Inventory modul: `GET /api/inventory/offcuts?material=...&minWidth=...&minHeight=...` endpoint | **ELLENŐRIZNI** | Offcut-first stratégia nem működik |
| 3 | Joinery: `GET /api/orders/{id}` response tartalmazza-e az item dimenziókat (widthMm, heightMm, material) | **ELLENŐRIZNI** | Orchestrator mapping nem tud transzformálni |
| 4 | CUTTING-031/033 migration deployed VPS-en | CUTTING_CONTEXT: "deploy szükséges" | Phase 3 migration függ a korábbi migrációktól |

### Előfeltétel ellenőrzés parancsok

```bash
# Inventory stock endpoint
curl -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5004/api/inventory/stock?material=MDF-18

# Inventory offcuts endpoint  
curl -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5004/api/inventory/offcuts?material=MDF-18&minWidth=400&minHeight=400

# Joinery order details
curl -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5002/api/orders/{orderId}
```

---

## 11. Nyitott kérdések Gábornak

| # | Kérdés | Hatás |
|---|--------|-------|
| 1 | A Joinery DoorOrder item response tartalmazza-e a `widthMm`, `heightMm`, `material`, `grainDirection` mezőket? | Orchestrator mapping design |
| 2 | Az Inventory modulnak vannak-e `GET /stock` és `GET /offcuts` endpointjai? | Blokkoló ha nincs → Inventory bővítés kell |
| 3 | A Phase 3 MVP-ben multi-material nesting szükséges? (pl. MDF-18 + Melamin-25 egy CuttingPlan-ben) | Ha igen → materialCode-onkénti nesting group Phase 3 MVP scope |
| 4 | A CuttingJob `Quantity` mező: 1 job qty=2 → 2 külön NestingPart, vagy 1 NestingPart qty=2? | A Nesting NuGet `NestingPart.Quantity`-t NEM támogatja — expanziós logika kell |
| 5 | A nesting trigger mikor fusson? Csak Publish-kor, vagy már Draft-ban is (preview)? | Draft-ban preview → extra UX, de a nesting result nem végleges |
