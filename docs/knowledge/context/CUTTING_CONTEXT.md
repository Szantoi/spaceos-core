# Cutting Terminal — Hidegindítási Kontextus

> Stack: .NET 8 (három külön repo: Inventory, Cutting, Procurement)
> Repos: `spaceos-modules-inventory`, `spaceos-modules-cutting`, `spaceos-modules-procurement`
> Branch: `develop`

---

## Felelősség

### Inventory modul (port 5004)
- Alapanyag készlet kezelés
- Mozgások (inbound, consumption, offcut)
- Trend elemzés
- Integration endpoint: `POST /api/inventory/integration/cutting-job-completed` (CUTTING-028)

### Cutting modul (port 5005)
- Lapszabász optimalizálás (nesting) — SpaceOS.Nesting.Algorithms NuGet
- CuttingSheet lifecycle
- CuttingPlan aggregate (Draft→Published→Frozen→Closed FSM)
- DailyPlan + CuttingJob child entity-k
- Inventory adatok felhasználása (IInventoryProvider HTTP adapter)
- Event bus: CuttingJobCompletedEvent → Inventory (CUTTING-028)

### Procurement modul (port 5006)
- Anyagbeszerzés
- Purchase order kezelés
- Supplier CRUD (PROCUREMENT-011)

---

## Jelenlegi állapot (2026-04-20)

| Modul | Tesztek | Port | VPS státusz |
|-------|---------|------|-------------|
| Inventory | **154 pass** | 5004 | Deployed |
| Cutting | **195 pass** (10 contracts + 185 domain/app/api/adapter) | 5005 | ACTIVE 🔴 (CUTTING-033 migration fix folyamatban) |
| Procurement | **53 pass** | 5006 | Deployed |
| SpaceOS.Nesting.Algorithms | **29 pass** | N/A (NuGet) | DONE ✅ |

**CUTTING-031 visszadobva** — a manuálisan írt migration elfogadható, de `dotnet ef database update` szükséges VPS-en az élesítéshez. CUTTING-032 (DaySlot) blokkolt CUTTING-031 deploy-ig.

---

## CuttingPlan aggregate — Session B állapot

### FSM (CUTTING-031 után)

```
Draft(0) → Published(1) → Frozen(2) → Closed(3)
```

**Enum:** `CuttingPlanStatus` (int, DB migration CASE konverzióval CUTTING-031)

**Backward-compat aliasok:**
- `"Approved"` string → `Published(1)`
- `"inprogress"` string → `Frozen(2)`

**Migration:** `20260420000001_CuttingPlanStatusToEnum.cs` — manuálisan megírva, `dotnet ef` nélkül.

### Planning stratégiák (CUTTING-027)

| Stratégia | Logika | Cél yield |
|-----------|--------|-----------|
| `MaxCutStrategy` | Guillotine optimized, hours desc + priority rank asc | 91%+ |
| `FIFOStrategy` | scheduledDate asc | ~70% |
| `PriorityStrategy` | priority rank asc + date asc | ~75% |
| `CustomStrategy` | MaxCutStrategy fallback | 91% |

**DI:** `IPlanningStrategy, FfdhNestingStrategy` + `IPlanningStrategyFactory` singleton

### Planning API endpoints

```
POST   /api/cutting/planning/                      — plan létrehozás
GET    /api/cutting/planning/                      — plan lista
GET    /api/cutting/planning/{planId}              — plan részletek
PUT    /api/cutting/planning/{planId}              — státusz frissítés
GET    /api/cutting/planning/{planId}/daily/{date} — napi bontás
PUT    /api/cutting/planning/jobs/{jobId}/complete — job befejezés (CUTTING-028)
```

Minden endpoint: `RequireAuthorization("ManufacturerOnly")`

---

## SpaceOS.Nesting.Algorithms NuGet (CUTTING-029/030)

**Repo:** `/opt/spaceos/spaceos-nesting-algorithms/` · commit `3e87954`

**Implementált stratégiák:**
- `FfdhNestingStrategy` — First Fit Decreasing Height (CUTTING-030-ban aktív)
- `GuillotineNestingStrategy` — Shorter Axis Rule (benchmark: ≥88% yield, <1s/200 part)
- `MaxRectsNestingStrategy` — placeholder (NotImplementedException, v2+)

**Kulcs modellek:**
```csharp
NestingInput       // Parts, Panels, SawBladeGapMm=4
NestingPart        // PartId, Name, WidthMm, HeightMm, CanRotate, Quantity
AvailablePanel     // PanelId, MaterialCode, WidthMm, HeightMm, IsOffcut
NestingResult      // Assignments, UnplacedParts, TotalWastePercentage
```

**KRITIKUS:** A NuGet **független** a `SpaceOS.Modules.Contracts`-tól — szándékosan standalone a FreeTier.Api direktör-kommunikáció céljából.

**Cutting modul integráció (CUTTING-030):**
- `GetNestingResultQueryHandler` átírva: `NestingService` → `INestingStrategy`
- `NestingService` `[Obsolete]` attribútummal — törlés v1.4.0-ban
- Mapper: `CuttingLine` → `NestingPart`, `PanelStockDto/OffcutDto` → `AvailablePanel`

---

## ICuttingEventPublisher — HTTP event bus (CUTTING-028)

```csharp
// Application réteg (interface)
public interface ICuttingEventPublisher
{
    Task PublishJobCompletedAsync(Guid jobId, ...);
}

// Infrastructure réteg (implementáció)
// CuttingEventPublisher → POST http://127.0.0.1:5004/api/inventory/integration/cutting-job-completed
// Header: X-Internal-Service: cutting
```

**Inventory oldal:** `POST /api/inventory/integration/cutting-job-completed`
- `AllowAnonymous` + `X-Internal-Service` header kötelező (403 ha hiányzik)
- Dimension guard: `if (WidthMm <= 0 || HeightMm <= 0 || ThicknessMm <= 0) return;`
- v1 stub: CUTTING még nem küldi a pontos méretet → offcut létrehozás halasztva v1.5-re

**Commit:** CUTTING `b0a11ba`, INVENTORY `2fe889e`

---

## Kritikus implementációs részletek

### TenantSessionInterceptor — mindhárom modulban

```csharp
// SaveChangesInterceptor + Set app.current_tenant_id GUC
// GUC kulcs: app.current_tenant_id (NEM app.tenant_id!)
// tid claim → GUID parse → set_config parametric call
```

**Commitok:** `a363ad6` (inventory), `0dbb02e` (procurement), `1ae66a0` (cutting) [MSG-CUTTING-008-DONE]

### MapInboundClaims = false (CUTTING-007)

Minden modulban `Program.cs`-ben kötelező:
```csharp
options.MapInboundClaims = false;
```

### InventoryProviderHttpAdapter (cutting)

`IInventoryProvider` implementáció HTTP adapter-rel:
- Regisztrálva: `AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>`
- Config: `InventoryService__BaseUrl=http://127.0.0.1:5004`

[MSG-CUTTING-010-DONE]

### DateTime.SpecifyKind — ISMERT GOTCHA (BUG-004)

```csharp
// HELYTELEN: DateTime.TryParse → Kind=Unspecified → Npgsql exception!
// HELYES:
var planDate = DateTime.SpecifyKind(rawDate.Date, DateTimeKind.Utc);
```

**Miért nem bukott meg unit teszteken:** InMemory EF nem ellenőrzi Kind-et. VPS PostgreSQL Npgsql stricten ellenőrzi `timestamp with time zone`-nál. [MSG-CUTTING-018-DONE]

### OpenConnectionAsync affinity fix (internal endpoints)

```csharp
if (dbContext.Database.IsRelational()) await dbContext.Database.OpenConnectionAsync(ct);
await dbContext.Database.ExecuteSqlAsync($"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct);
try { ... }
finally { if (dbContext.Database.IsRelational()) await dbContext.Database.CloseConnectionAsync(); }
```

[MSG-CUTTING-015-DONE]

---

## Env config

```ini
# /etc/spaceos/cutting.env
ASPNETCORE_URLS=http://127.0.0.1:5005
ConnectionStrings__CuttingDb=Host=127.0.0.1;Port=5433;Database=spaceos_cutting;...
Jwt__Authority=https://joinerytech.hu/auth/realms/spaceos
InventoryService__BaseUrl=http://127.0.0.1:5004
```

---

## GUC regisztráció (egyszeri DB init)

```sql
ALTER DATABASE spaceos_cutting SET "app.current_tenant_id" TO '';
ALTER DATABASE spaceos_inventory SET "app.current_tenant_id" TO '';
ALTER DATABASE spaceos_procurement SET "app.current_tenant_id" TO '';
```

**KRITIKUS:** Ha ez hiányzik → `42704 unrecognized configuration parameter` [MSG-E2E-044]

---

## Debug lánc (nesting aktiválás)

Ha a nesting E2E teszt hibázik, check sorrendben:
1. `401` → `MapInboundClaims = false` megvan-e?
2. `42501 permission denied` → séma GRANT megvan-e?
3. `42704 unrecognized GUC` → `ALTER DATABASE SET` lefutott-e?
4. `22P02 invalid uuid ""` → TenantSessionInterceptor fut-e?
5. `500 DI hiba` → `IInventoryProvider` regisztrálva?
6. `500 connection affinity` → OpenConnectionAsync explicit pinning?
7. `500 Npgsql timestamptz` → `DateTime.SpecifyKind(Utc)` megvan-e?

---

## Session B nyitott feladatok (CUTTING-032..036)

```
CUTTING-031 ✅ DONE (enum + migration, deploy szükséges)
CUTTING-032 DaySlot value object (blokkolt CUTTING-031 deploy-ig)
CUTTING-033 🔄 Migration fix folyamatban
CUTTING-034 PanelReservation aggregate
CUTTING-035 Publish/Freeze/Close command handlers
CUTTING-036 Extension points (PartnerId, SourceChannel)
```

Spec: `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`

---

## Ismert tech debt

1. Service-to-service auth (HTTP adapter) nincs autentikálva — Q3 scope
2. `kernel.env` jogosultság 644 → 640 (procurement/inventory is érintett)
3. `NestingService` `[Obsolete]` — törlés v1.4.0-ban
4. CUTTING-031 manuális migration — `dotnet ef database update` kötelező VPS-en
