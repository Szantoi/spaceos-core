---
id: MSG-CUTTING-003
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-002-DONE
created: 2026-04-15
---

# MSG-CUTTING-003 — Procurement Core implementáció

## Kontextus

A Cutting Core (CUTTING-003) kész — 91/91 teszt, `CuttingProviderAdapter` implementálva.

Most jön az utolsó implementációs fázis: **Procurement Core**.

## Feladat — CUTTING-004

Implementáld a következő projekteket a solution-be:

### 1. SpaceOS.Modules.Procurement.Domain

Aggregatok:
- **`Supplier`** — Szállító cég. Tulajdonságok: `TenantId`, `Name`, `ContactEmail`, `LeadTimeDays`, `Rating` (decimal 0-5), `IsActive`. `Deactivate()` domain metódus.
- **`PurchaseOrder`** — Beszerzési rendelés. FSM: `Draft → Submitted → Confirmed → Shipped → Delivered / Cancelled`. Tulajdonságok: `TenantId`, `SupplierId`, `MaterialType`, `Quantity`, `UnitPrice`, `Currency`, `Status`, `ExpectedDeliveryDate`, `CreatedAt`. Domain metódusok: `Submit()`, `Confirm()`, `MarkShipped()`, `RecordDelivery()`, `Cancel()`. Minden FSM átmenet domain eventet dob.
- **`Delivery`** — Szállítás fogadása. Tulajdonságok: `TenantId`, `PurchaseOrderId`, `ReceivedQuantity`, `ReceivedAt`, `Notes`, `RecordedBy`. **Append-only** — nincs UPDATE.

Domain events: `PurchaseOrderSubmittedEvent`, `PurchaseOrderDeliveredEvent`, `ReorderAlertTriggeredEvent`

### 2. SpaceOS.Modules.Procurement.Application

CQRS handlerek (MediatR):
- `CreatePurchaseOrderCommand` → `Result<Guid>`
- `GetOrderStatusQuery` → `OrderStatusResponse`
- `GetSupplierPricesQuery` → `List<SupplierPriceResponse>` — szállítónkénti árlista adott anyagtípushoz
- `RecordDeliveryCommand` → `Result` — szállítás fogadás + Inventory `RecordInbound` hívása

FluentValidation validátorok minden Command-hoz.

**Fontos:** A `RecordDeliveryCommand` handler az `IInventoryProvider`-en keresztül hívja a `RecordInboundAsync`-ot — ez a Cutting → Inventory integráció első pontja.

### 3. SpaceOS.Modules.Procurement.Infrastructure

- EF Core 8 DbContext: `ProcurementDbContext`
- PostgreSQL séma: `spaceos_procurement`
- Migrations: initial migration
- RLS: `Suppliers`, `PurchaseOrders`, `Deliveries` — `FORCE ROW LEVEL SECURITY`
- Repository: `IProcurementRepository` → `ProcurementRepository`
- **`IProcurementProvider` implementáció**: `ProcurementProviderAdapter`

### 4. SpaceOS.Modules.Procurement.Api

Minimal API (port 5004, prefix `/api/procurement`):

```
POST   /api/procurement/orders          CreatePurchaseOrder
GET    /api/procurement/orders/{id}     GetOrderStatus
GET    /api/procurement/prices          GetSupplierPrices (query: materialType)
POST   /api/procurement/deliveries      RecordDelivery
```

Minden endpoint: `RequireAuthorization("ManufacturerOnly")`

### 5. Bővítsd a tesztet

Minimum 30 új teszt:
- Domain: 10 — PurchaseOrder FSM, Delivery append-only, Supplier lifecycle
- Infrastructure/EF: 10 — CRUD, RLS szimulálás, FSM persistence
- API: 10 — endpoint smoke tests + `RecordDelivery` → `IInventoryProvider` mock hívás verifikálása

## DoD

- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → ≥30 új teszt + meglévő 91 mind zöld (összesen ≥121)
- [ ] RLS migration-ben implementálva
- [ ] `IProcurementProvider` implementálva (`ProcurementProviderAdapter`)
- [ ] `RecordDelivery` → `IInventoryProvider.RecordInboundAsync` integráció
- [ ] Outbox üzenet: `MSG-CUTTING-003-DONE`
