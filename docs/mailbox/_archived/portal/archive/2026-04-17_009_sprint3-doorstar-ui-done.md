---
id: MSG-PORTAL-009-DONE
from: portal
to: root
type: done
priority: high
status: READ
ref: MSG-PORTAL-009
created: 2026-04-17
---

# Sprint 3 — Doorstar Portal: Teljes üzleti logika UI — KÉSZ

## Összefoglaló

Mind a 6 FE task implementálva. Commit: `a57511b` (branch: `develop`)

### Implementált fájlok (38 fájl, +2379 sor)

| Task | Fájlok |
|---|---|
| **FE-012** — Nav Shell | `Sidebar.tsx` (Doorstar szekció: Dashboard/Rendelések/Készlet/Szállítók/Vágótervek), `router/index.tsx` (4 új lazy route) |
| **FE-013** — Suppliers | `suppliersApi.ts`, `useSuppliers.ts`, `useCreateSupplier.ts`, `SuppliersPage.tsx`, `CreateSupplierModal.tsx` + 2 teszt fájl |
| **FE-014** — Inventory | `inventoryApi.ts`, `useInventoryStock.ts`, `useOffcuts.ts`, `useRecordInbound.ts`, `useConsumptionTrend.ts`, `InventoryPage.tsx` (3 tab), `RecordInboundModal.tsx` + 2 teszt fájl |
| **FE-015** — Cutting | `cuttingApi.ts`, `useCuttingPlans.ts`, `useCreateCuttingPlan.ts`, `useNestingResult.ts`, `CuttingPlansPage.tsx`, `CreateCuttingPlanModal.tsx`, `NestingResultPanel.tsx` + 2 teszt fájl |
| **FE-016** — Dashboard | `DashboardPage.tsx` overhaul: 4 stat kártya (nyitott rendelések, MDF 18mm készlet, szállítók, utolsó vágóterv) + quick actions + loading skeleton + `DashboardPage.test.tsx` |
| **FE-017** — Procurement | `procurementApi.ts`, `useProcurementOrders.ts`, `useCreatePurchaseOrder.ts`, `useRecordDelivery.ts`, `ProcurementPage.tsx` (2 tab), `CreatePurchaseOrderModal.tsx`, `RecordDeliveryModal.tsx` + teszt fájl |

### Hozzáadott típusok (`types/index.ts`)
`SupplierDto`, `CreateSupplierRequest`, `MaterialType`, `InventoryStockDto`, `RecordInboundRequest`, `OffcutDto`, `ConsumptionTrendDto`, `CuttingPlanDto`, `CreateCuttingPlanRequest`, `NestingResultDto`, `NestingPieceDto`, `PurchaseOrderDto`, `CreatePurchaseOrderRequest`, `DeliveryDto`, `RecordDeliveryRequest`

## Tesztek

```
Test Files  52 passed (52)
     Tests  306 passed (306)   [baseline: 281 → +25 új teszt]
  Start at  15:11:11
  Duration  33.52s
```

**0 TypeScript hiba** (`tsc --noEmit` clean)

## Security review

- XSS: `dangerouslySetInnerHTML` nem használt sehol
- Auth: minden új oldal `ProtectedRoute` mögött van (AppShell-en belül)
- Token handling: `apiClient` interceptor kezeli, token csak memóriában
- API errors: form hibák user-friendly üzenetként, stack trace nélkül
- Input sanitization: react-hook-form + zod validáció minden formon
- OWASP: nincs nyilvánvaló sebezhetőség

## Kockázatok / kérdések

Nincsenek. Minden BFF endpoint dokumentált volt, az API konvenciók követik a meglévő mintát.

**Megjegyzés:** A NestingResultPanel `retry: false`-szal kezeli a 404-et (ha nincs nesting adat a kiválasztott tervhez) — graceful empty state jelenik meg.
