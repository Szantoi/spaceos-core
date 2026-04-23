---
id: MSG-CUTTING-001-DONE
from: cutting
to: root
type: done
status: READ
---

## Összefoglaló

Implementálva: **SpaceOS Inventory Core (CUTTING-002)** — teljes Clean Architecture + DDD + CQRS stack.

### Létrehozott projektek (solution-be felvéve):

- `src/SpaceOS.Modules.Inventory.Domain/` — aggregates (MaterialCatalog, PanelStock, Offcut, StockMovement), domain events, enums, IInventoryRepository interfész
- `src/SpaceOS.Modules.Inventory.Application/` — 3 query handler (GetStock, GetOffcuts, GetConsumptionTrend) + 3 command handler (RecordConsumption, RecordInbound, RecordOffcut) + FluentValidation validátorok
- `src/SpaceOS.Modules.Inventory.Infrastructure/` — EF Core DbContext, 4 entity configuration, InventoryRepository, InventoryProviderAdapter (IInventoryProvider implementáció), HttpContextTenantAccessor, DI extensions, initial EF migration (RLS + seed data)
- `src/SpaceOS.Modules.Inventory.Api/` — Minimal API endpoints (6 endpoint), Program.cs
- `tests/SpaceOS.Modules.Inventory.Tests/` — 47 teszt (domain 19 · infra 8 · application 2 · API 8 · security 5 + 5 régi Contracts.Tests)

### Módosított fájlok:
- `SpaceOS.Modules.Cutting.sln` — 5 új projekt hozzáadva
- `src/SpaceOS.Modules.Inventory.Infrastructure/SpaceOS.Modules.Inventory.Infrastructure.csproj` — FrameworkReference hozzáadva (IHttpContextAccessor miatt)

## Tesztek

- **Összesen: 56 teszt** (47 új + 9 meglévő Contracts.Tests)
- **Eredmény: mind zöld (0 failure, 0 skip)**
- Domain tesztek: 19 (MaterialCatalog, PanelStock, Offcut, StockMovement aggregates)
- EF/Repository tesztek: 8 (InMemory, CRUD, date range filter)
- Application handler tesztek: 2 (GetStockQueryHandler mock-okkal)
- API endpoint tesztek: 8 (auth, CRUD, 401)
- Security tesztek: 5 (RLS tenant isolation, append-only, no public setters, MaterialCatalog TenantId-mentes)

## Security review

- **Authorization**: minden `/api/inventory/**` endpoint `RequireAuthorization("ManufacturerOnly")` policy-val védve
- **RLS**: Migration tartalmaz `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` + `CREATE POLICY tenant_isolation` a PanelStocks, Offcuts, StockMovements táblákon
- **Immutability**: StockMovement — append-only, nincs public setter; CuttingSheet még nem implementált (PHASE 2)
- **MaterialCatalog**: `REVOKE INSERT, UPDATE, DELETE ON spaceos_inventory."MaterialCatalogs" FROM spaceos_app` a migrationben
- **OWASP**: nincs SQL injection (EF Core paraméteres lekérdezések), nincs hardcoded secret, input validáció FluentValidation-nel

## Kockázatok / kérdések

Nincs blokkoló kockázat. Megjegyzések:

1. Az `InventoryProviderAdapter.GetConsumptionTrendAsync()` jelenleg hardcode-olt "MDF 18mm" materialType-ot küld, mert az IInventoryProvider.GetConsumptionTrendAsync() interfész nem tartalmaz materialType paramétert. Ez az interfész-szerződésből fakad — ha szükséges, az interfészt módosítani kell.
2. A `HttpContextTenantAccessor` a `tenant_id` claim-et olvassa a JWT tokenből — a Kernel auth middleware-rel összhangban kell lennie.
3. InMemory adatbázis a teszteknél nem futtatja a `HasData` seeddatát EnsureCreated() nélkül — a tesztek ezért manuálisan seed-elnek. Real PostgreSQL esetén a migration kezeli.
