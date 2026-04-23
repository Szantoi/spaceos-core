---
id: MSG-CUTTING-001
from: root
to: cutting
type: task
priority: high
status: READ
ref: JOINERY-006-DONE
created: 2026-04-15
---

# MSG-CUTTING-001 — Inventory Core implementáció

## Kontextus

A Contracts NuGet package-ek (CUTTING-001) elkészültek (`84bb708`):
- `SpaceOS.Modules.Inventory.Contracts` 1.0.0 — `IInventoryProvider` (6 metódus)
- `SpaceOS.Modules.Cutting.Contracts` 1.0.0 — `ICuttingProvider` (4 metódus)
- `SpaceOS.Modules.Procurement.Contracts` 1.0.0 — `IProcurementProvider` (4 metódus)

Most jön az első implementációs fázis: **Inventory Core**.

## Feladat — CUTTING-002

Implementáld a következő projekteket a `/opt/spaceos/spaceos-modules-cutting/` solution-be:

### 1. SpaceOS.Modules.Inventory.Domain

Aggregatok:
- `MaterialCatalog` — Anyagtörzs (MDF 18mm, HDF 3mm, ABS él, stb.). Tulajdonságok: `MaterialType`, `StandardWidth`, `StandardHeight`, `ThicknessMm`, `UnitCost`, `SupplierRef`. **Tenant-független** (shared reference data).
- `PanelStock` — Raktárkészlet. Két altípus: teljes tábla + Offcut. Tulajdonságok: `TenantId`, `MaterialCatalogId`, `WidthMm`, `HeightMm`, `StockType` (FullPanel/Offcut), `Quantity`, `LocationCode`.
- `Offcut` — Maradék darab. Tulajdonságok: `TenantId`, `MaterialCatalogId`, `WidthMm`, `HeightMm`, `OriginCuttingSheetId` (nullable a kezdetben), `Status` (Available/Used/Waste). Append-only életciklus.
- `StockMovement` — Minden készletváltozás naplója. Tulajdonságok: `TenantId`, `MovementType` (Inbound/Consumption/Offcut/Scrap), `MaterialCatalogId`, `Quantity`, `OccurredAt`, `Reference`. **Append-only** — nincs UPDATE, nincs DELETE.

Domain events: `StockLevelChangedEvent`, `OffcutRegisteredEvent`, `LowStockAlertEvent`

### 2. SpaceOS.Modules.Inventory.Application

CQRS handlerek (MediatR):
- `GetStockQuery` → `StockLevelResponse` — aktuális készletszint anyagtípusonként
- `GetOffcutsQuery` → `List<OffcutResponse>` — felhasználható maradékok
- `RecordConsumptionCommand` → `Result` — felhasználás rögzítése (Cutting hívja majd)
- `RecordInboundCommand` → `Result` — bevételezés (Procurement hívja majd)
- `RecordOffcutCommand` → `Result` — maradék regisztrálás (Cutting hívja majd)
- `GetConsumptionTrendQuery` → `ConsumptionTrendResponse` — fogyási trend (dátumtartomány)

FluentValidation validátorok minden Command-hoz.

### 3. SpaceOS.Modules.Inventory.Infrastructure

- EF Core 8 DbContext: `InventoryDbContext`
- PostgreSQL séma: `spaceos_inventory`
- Migrations: initial migration
- RLS konfiguráció:
  - `PanelStocks`, `Offcuts`, `StockMovements` — `FORCE ROW LEVEL SECURITY` + policy: `TenantId = current_setting('app.current_tenant_id')`
  - `MaterialCatalog` — **nincs RLS** (tenant-független), de `REVOKE INSERT, UPDATE, DELETE ON material_catalogs FROM spaceos_app`
- Repository implementáció: `IInventoryRepository` → `InventoryRepository`

### 4. SpaceOS.Modules.Inventory.Api

Minimal API endpoint-ok (port 5004, prefix `/api/inventory`):

```
GET    /api/inventory/stock             GetStock (query: materialType?)
GET    /api/inventory/offcuts           GetOffcuts (query: materialType?)
POST   /api/inventory/movements/consumption   RecordConsumption
POST   /api/inventory/movements/inbound       RecordInbound
POST   /api/inventory/movements/offcut        RecordOffcut
GET    /api/inventory/trend             GetConsumptionTrend (query: from, to)
```

Minden endpoint: `[Authorize(Policy = "ManufacturerOnly")]`

### 5. SpaceOS.Modules.Inventory.Tests (bővítsd a meglévő tests projektet)

Minimum 40 teszt:
- Domain: 15 — aggregát lifecycle, domain event dobás, StockMovement append-only logika
- Infrastructure/EF: 10 — CRUD + RLS szimulálás (SetTenantId), Offcut states
- API: 10 — endpoint smoke tests (in-memory)
- Security: 5 — RLS cross-tenant izoláció, Unauthorized 401

## Technikai döntések

1. **`spaceos_app` PostgreSQL user** — ez az alkalmazás runtime userje (nem `spaceos_admin`). A migration `spaceos_admin`-nal fut, de az app `spaceos_app`-pal csatlakozik. Ha lokálisan teszteled, elég ha Testcontainers-ban ugyanazt a usert használod.

2. **MaterialCatalog seed** — a migration-be ágyazd be az alap anyagokat (MDF 18mm, MDF 16mm, HDF 3mm, Forgácslap 18mm, ABS él 0.8mm) seed adatként.

3. **IInventoryProvider implementáció** — az `InventoryProviderAdapter` osztály implementálja az `IInventoryProvider` interface-t (a Contracts projektből), delegálva a CQRS handler-ekre. Így a Cutting modul (következő fázis) a Contract-on keresztül hívja az Inventory-t.

## DoD

- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → ≥40 teszt, mind zöld
- [ ] `dotnet pack` → `SpaceOS.Modules.Inventory.1.0.0.nupkg` kész
- [ ] RLS migration-ben implementálva
- [ ] MaterialCatalog seed adatok migration-ben
- [ ] `IInventoryProvider` implementálva (`InventoryProviderAdapter`)
- [ ] Outbox üzenet: `MSG-CUTTING-001-DONE`

## Ha elakadtál

`status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.
Outbox helye: `/opt/spaceos/docs/mailbox/cutting/outbox/`
