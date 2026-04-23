# SpaceOS — Module Boundaries

> Interfészek, contracts, adatáramlás. Forrás: mailbox archive + Codebase_Status.md.

---

## IInventoryProvider

**Definiálva:** `spaceos-modules-abstractions` Contracts NuGet csomag (`SpaceOS.Modules.Inventory.Contracts`)

**Metódusok:**
```csharp
public interface IInventoryProvider
{
    Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default);
    Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default);
    Task RecordConsumptionAsync(IEnumerable<ConsumptionItem> items, CancellationToken ct = default);
    Task RecordInboundAsync(InboundItem item, CancellationToken ct = default);
    Task RecordOffcutAsync(OffcutItem item, CancellationToken ct = default);
    Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
```

**Implementációk:**
- `InventoryProviderStub` — hardcoded empty returns (teszt)
- `InventoryProviderHttpAdapter` — HTTP hívás az Inventory service `127.0.0.1:5004`-re [MSG-CUTTING-010-DONE]

**Fogyasztói:** Cutting modul (`GetNestingResultQueryHandler`)

---

## ICuttingProvider

**Definiálva:** `spaceos-modules-abstractions` Contracts NuGet csomag (`SpaceOS.Modules.Cutting.Contracts`)

**Fogyasztói:** Joinery modul (BOM generáláshoz), Orchestrator (proxy)

---

## IProcurementProvider

**Definiálva:** `spaceos-modules-abstractions` Contracts NuGet csomag (`SpaceOS.Modules.Procurement.Contracts`)

**Fogyasztói:** Joinery modul (hardverlista generáláshoz)

---

## Contracts NuGet csomagok

| Csomag | Aktuális verzió | Tartalom | Ki fogyasztja |
|--------|-----------------|----------|---------------|
| `SpaceOS.Modules.Inventory.Contracts` | **1.2.0** | `IInventoryProvider` + DTO-k + Reservation API | Cutting modul |
| `SpaceOS.Modules.Cutting.Contracts` | **1.3.0** | `ICuttingProvider` + DTO-k + `SourceChannel` + `AnonymousSheetRequest` + `SubmitAnonymousSheetAsync` DIM | Joinery modul |
| `SpaceOS.Modules.Procurement.Contracts` | 1.0.0 | `IProcurementProvider` + DTO-k | Joinery modul |

### Contracts 1.3.0 újdonságai (ABSTRACTIONS-010 DONE)

**`SourceChannel` enum** (Shared namespace):
```csharp
namespace SpaceOS.Modules.Contracts.Shared;
public enum SourceChannel { Direct = 0, FreeTier = 1, PartnerTier = 2 }
```
Szerepe: audit trail, RBAC differenciálás, rate-limit policy-k. Nem Cutting-specifikus.

**`ProviderCapability.CuttingAnonymous`** = `1 << 12` — Új flag, meglévő flag-ek változatlanok.

**`ICuttingProvider.SubmitAnonymousSheetAsync`** — Default Interface Method (DIM):
```csharp
// DIM default: NotSupportedException
// Consumer MUST check CuttingAnonymous capability before calling
```
Meglévő implementációk NEM törnek el (DIM). [ADR-032]

**`AnonymousSheetRequest`** — Wrapper DTO `SubmitCuttingSheetRequest` fölött:
```csharp
// Extra mezők: Source (SourceChannel), PartnerId, BrandingContextId
```

**NuGet feed:** Belső NuGet feed vagy local path referencia.

[MSG-ABSTRACTIONS-001-DONE, Contracts v4.2 spec]

---

## ProviderAdapter (in-process) vs ProviderHttpAdapter (HTTP)

### In-process adapter (korábbi fázis)

```csharp
// Ha az összes modul egy process-ben fut (Modular Monolith):
services.AddScoped<IInventoryProvider>(sp =>
    new InventoryProviderAdapter(sp.GetRequiredService<IInventoryService>()));
```

**Mikor:** Monolith deploy — teljesítmény kritikus, nincs network overhead.

### HTTP adapter (jelenlegi production)

```csharp
// Ha a modulok különböző process-ekben futnak (mikro-service):
services.AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>(client =>
    client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"]!));
```

**Mikor:** Külön systemd service-ek, loopback HTTP kommunikáció.

**Config:**
```ini
InventoryService__BaseUrl=http://127.0.0.1:5004
```

[MSG-CUTTING-010-DONE]

---

## ProviderStub

```csharp
// Fejlesztési/tesztelési célra:
services.AddScoped<IInventoryProvider, InventoryProviderStub>();
```

**KRITIKUS:** Stub NEM kerülhet production DI container-be. Csak tesztkonfigurációban.

**Tünet ha mégis production-ban van:** `IInventoryProvider` null/empty results → `GetNestingResultQueryHandler` nem kap panel adatot.

---

## SpaceOS.Nesting.Algorithms NuGet

**Repo:** `/opt/spaceos/spaceos-nesting-algorithms/` · v1.0.0 · commit `3e87954`

**Stratégiák:**
```
INestingStrategy (interface)
  FfdhNestingStrategy      — First Fit Decreasing Height (aktív production)
  GuillotineNestingStrategy — Shorter Axis Rule (benchmark: ≥88% yield)
  MaxRectsNestingStrategy  — NotImplementedException placeholder (v2+)
NestingStrategyFactory     — factory dispatch
```

**KRITIKUS:** Független a `SpaceOS.Modules.Contracts`-tól — szándékosan standalone.

**Fogyasztói:** Cutting modul (`GetNestingResultQueryHandler` → `INestingStrategy.ComputeAsync`)

[MSG-CUTTING-029-DONE, MSG-CUTTING-030-DONE, ADR-035]

---

## ICuttingEventPublisher (Cutting → Inventory HTTP event bus)

```csharp
// Application réteg (interface):
public interface ICuttingEventPublisher { Task PublishJobCompletedAsync(Guid jobId, ...); }

// Infrastructure réteg (implementáció):
// CuttingEventPublisher → POST http://127.0.0.1:5004/api/inventory/integration/cutting-job-completed
// Header: X-Internal-Service: cutting (kötelező — Inventory 403 ha hiányzik)
```

**Inventory endpoint:** `POST /api/inventory/integration/cutting-job-completed`
- `AllowAnonymous` + `X-Internal-Service` header guard
- Dimension guard: méretek nélkül kihagyja az offcut létrehozást (v1 stub)

[MSG-CUTTING-028-DONE, ADR-036-hez kapcsolódik]

---

## IParametricProduct (Kernel interfész)

```csharp
// SpaceOS.Kernel.Domain/Interfaces/IParametricProduct.cs
public interface IParametricProduct
{
    Guid Id { get; }
    Guid TenantId { get; }
    string ProductCode { get; }
    IReadOnlyList<IParametricComponent> Components { get; }
}
```

**Szerepe:** A Kernel `IParametricProduct` interfészen dolgozik — nem tudja, hogy ajtó vagy szekrény van mögötte (5 Golden Rule #2 — Modular Monolith).

---

## Orchestrator BFF routing logika

```typescript
// src/index.ts — route regisztrációs sorrend
app.use('/bff/health', healthRouter);
app.use('/bff/auth', authRouter);
app.use('/bff/chat', requireAuth, chatLimiter, chatRouter);       // ← requireAuth ELŐBB!
app.use('/bff/internal', proxyLimiter, requireInternalHeader, internalRouter);
app.use('/bff/joinery', requireAuth, proxyLimiter, joineryRouter);
app.use('/bff/abstractions', requireAuth, proxyLimiter, abstractionsRouter);  // pass-through
app.use('/bff/cutting', requireAuth, proxyLimiter, cuttingRouter);
app.use('/bff/stages', requireAuth, stageDispatchRouter);
app.use('/bff/handshakes', requireAuth, handshakesRouter);
app.use('/bff/api', requireAuth, proxyLimiter, kernelProxyRouter);
app.use('/bff/nodes', requireAuth, proxyLimiter, nodesRouter);
app.use('/bff/sync', requireAuth, proxyLimiter, syncRouter);
```

**Kritikus sorrend:** `requireAuth` MINDIG a `chatLimiter` / `proxyLimiter` előtt! Ha fordítva van, unauthentikált kérések a rate limiter ablakból 429-et kapnak 401 helyett. [MSG-ORCH-058-DONE]

**Abstractions pass-through:**
```typescript
// abstractionsRouter: pathRewrite '^/': '/api/'
// /bff/abstractions/modules/templates/* → 127.0.0.1:5003/api/modules/templates/*
```

[MSG-ORCH-059-DONE]

---

## Kernel RBAC policy-k

| Policy | Role | Endpoints |
|--------|------|-----------|
| `SystemAdminPolicy` | SystemAdmin | Stage definíció regisztráció/törlés |
| `TenantAdminPolicy` | TenantAdmin | Stage chain template kezelés, brand skin |
| `StageOperatorPolicy` | StageOperator | Stage dispatch |
| `ReadPolicy` | Bármely auth | Stage lista, chain template lista, handoff lekérés |
| `ManufacturerOnly` | Manufacturer (Abstractions) | Minden Abstractions endpoint |

**Keycloak realm_access.roles → ASP.NET Core role claim mapping:**
```csharp
// OnTokenValidated hook:
context.Principal = new ClaimsPrincipal(
    new ClaimsIdentity(
        context.Principal?.Claims
            .Concat(realmRoles.Select(r => new Claim(ClaimTypes.Role, r))),
        context.Scheme.Name));
```

[MSG-KC01-RESP T1]

---

## Adatbázis szeparáció

| DB neve | Schema | Tartalmaz | Service |
|---------|--------|-----------|---------|
| `spaceos` | public | Kernel összes tábla (Tenants, Facilities, FlowEpics, Spaces, AuditEvents, RefreshTokens, Stage*, ...) | Kernel |
| `spaceos_audit_sink` | public | `hash_chain_records` (HashSinkDbContext) | Kernel (WORM) |
| `spaceos_joinery` | spaceos_joinery | DoorOrders, DoorItems, CuttingListSnapshots, OutboxEntries, ... | Joinery |
| `spaceos_abstractions` | spaceos_modules | ProductTemplates, ComponentSlots, SlotConnections, TemplateParameters, GeometryAttachments | Abstractions |
| `spaceos_cutting` | spaceos_cutting | CuttingSheets, NestingResults | Cutting |
| `spaceos_inventory` | spaceos_inventory | Stock, Movements, OffcutRecords | Inventory |
| `spaceos_procurement` | spaceos_procurement | ProcurementItems, Orders | Procurement |
| `spaceos_keycloak` | public | Keycloak belső adatok | Keycloak |

**PostgreSQL port:** `5433` (NEM 5432!)
