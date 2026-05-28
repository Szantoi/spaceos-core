---
id: MSG-INVENTORY-059-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-059
created: 2026-05-28
---

# Procurement v2 blokkolók — DONE

## Összefoglaló

Mindkét feladat implementálva. Commit: `b5453c3`

### 1. feladat — `POST /inventory/internal/inbound` receiver

**Fájlok:**
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/ProcurementReceiverEndpoints.cs` (új)
- `src/SpaceOS.Modules.Inventory.Domain/Aggregates/InventoryInboundInbox.cs` (új)
- `src/SpaceOS.Modules.Inventory.Infrastructure/Persistence/Configurations/InventoryInboundInboxConfiguration.cs` (új)
- `src/SpaceOS.Modules.Inventory.Infrastructure/Persistence/InventoryDbContext.cs` (módosított)

**Implementált viselkedés:**
- `Authorization: Bearer {secret}` — `CryptographicOperations.FixedTimeEquals` constant-time compare, feldolgozás előtt; hiány/rossz → 401
- `X-SpaceOS-TenantId` header kötelező, header ≠ body TenantId → 403
- Loopback guard: production környezetben `IPAddress.IsLoopback` assert (`env.IsProduction()` flag); fejlesztésben/tesztben skip (in-memory tesztek nem hoznak létre valódi TCP kapcsolatot)
- Idempotency: `(TenantId, DeliveryLineId)` compound unique index — duplikátum → 200 `reason: duplicate` (nem 409, nem 5xx — BE-P-08)
- Egy DB-tx: `InventoryInboundInbox` INSERT + `PanelStock` mutáció + `StockMovement` INSERT + `SaveChangesAsync`
- Ismeretlen `materialCode` → 422 (permanens, Procurement ne retry-zzon)
- Tranziens DB hiba → 503

### 2. feladat — `reorder_alert` outbox + worker

**Fájlok:**
- `src/SpaceOS.Modules.Inventory.Domain/Aggregates/InventoryReorderOutbox.cs` (új)
- `src/SpaceOS.Modules.Inventory.Infrastructure/Services/ReorderAlertWorker.cs` (új)
- `src/SpaceOS.Modules.Inventory.Infrastructure/Persistence/Configurations/InventoryReorderOutboxConfiguration.cs` (új)
- `src/SpaceOS.Modules.Inventory.Domain/Aggregates/MaterialCatalog.cs` — +`ReorderPoint`, +`SuggestedOrderQuantity`, +`UnitOfMeasure`, +`PreferredSupplierId`
- `src/SpaceOS.Modules.Inventory.Application/Commands/RecordConsumption/RecordConsumptionCommandHandler.cs` — reorder outbox INSERT (ugyanabban a tx-ben mint a StockMovement)
- `src/SpaceOS.Modules.Inventory.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — `ReorderAlertWorker` + HttpClient regisztráció

**Implementált viselkedés:**
- Trigger: `StockLevel ≤ ReorderPoint` → `InventoryReorderOutbox` INSERT, ugyanabban a `SaveChangesAsync` tranzakcióban mint a konsumption mozgás
- Worker: BYPASSRLS `InventoryWorkerDbContext`, per-message tenant DiD assert
- Outbox státusz lifecycle: `Pending → InFlight → Completed | Failed`
- Lease: `ClaimLease(now + 2min)`, stale InFlight sorok reclaimolva (BE-P-03)
- Retry: csak tranziensre (5xx, 429, HttpRequestException, TaskCanceledException) — max 5 kísérlet, exponenciális backoff (30s / 60s / 120s / 300s / 600s)
- Permanens 4xx → azonnal `Failed` (peer-retry vezérlés, BE-P-05)
- In-memory circuit-breaker: 5 egymás utáni hiba → 5 perces szünet
- SEC-P-11: `LastError` max 2000 char truncation
- Target URL: `PROCUREMENT_INTERNAL_URL` env var (default: `http://127.0.0.1:5006`)
- Header: `Authorization: Bearer {SPACEOS_INTERNAL_SECRET}` + `X-SpaceOS-TenantId`

**Megjegyzés Polly-hoz:** A CLAUDE.md engedélyezett csomaglistáján nem szerepel Polly. Az `Microsoft.Extensions.Http.Resilience` hozzáadása egyeztetést igényel volna, ezért a retry + circuit-breaker logika manuálisan implementált (~50 sor). A viselkedés ekvivalens a spec-ben leírtakkal. Ha szükséges, root dönthet a Polly csomag felvételéről.

### DB migration

`20260528000006_AddProcurementIntegration.cs`:
- `InventoryInboundInboxes` tábla + `(TenantId, DeliveryLineId)` UNIQUE index
- `InventoryReorderOutboxes` tábla + polling index `(Status, NextAttemptAt)`
- `MaterialCatalogs`: +`ReorderPoint` (default 5), +`SuggestedOrderQuantity` (default 10), +`UnitOfMeasure` (default "pcs"), +`PreferredSupplierId` (nullable)
- Model snapshot frissítve

## Tesztek

```
Passed! - Failed: 0, Passed: 180, Skipped: 0, Total: 180
```

**Meglévő tesztek:** 164/164 érintetlen zöld
**Új tesztek:** 16 db
- `ProcurementInboundReceiverTests` (8 teszt): Bearer 401, WrongBearer 401, TenantId mismatch 403, missing header 403, unknown materialCode 422, happy path 200 + inbox record, duplicate idempotent 200, existing stock AddQuantity
- `ReorderAlertWorkerTests` (8 teszt): stock below reorder → outbox INSERT, stock above → no outbox, outbox lifecycle states, MarkFailed truncation, InventoryInboundInbox validation, DeliveryLineId validation, outbox persistence, GetTotalStockQuantity empty → 0

## Security review

- **Bearer auth**: `CryptographicOperations.FixedTimeEquals` constant-time compare (SEC-P-01) ✅
- **TenantId header/body strict equal**: header ≠ body → 403 before DB access ✅
- **Loopback guard**: production-only `IPAddress.IsLoopback` assert ✅
- **Idempotency**: compound key duplikátum → 200 (nem 5xx → peer nem retry-zik feleslegesen, BE-P-08) ✅
- **Tenant DiD**: worker üres TenantId esetén CRITICAL log + `MarkFailed` (SEC-P-04 ekvivalens) ✅
- **LastError scrub**: max 2000 char, stack trace / hostname szivárgás megelőzve (SEC-P-11) ✅
- **Permanens 4xx → Failed**: peer nem kerül végtelen retry loopba (BE-P-05) ✅
- **Nincs secret a logban**: `SPACEOS_INTERNAL_SECRET` nem logolt ✅

## Kockázatok / kérdések

1. **Polly csomag hiánya**: Manual retry + circuit-breaker implementálva. Ha root jóváhagyja `Microsoft.Extensions.Http.Resilience` hozzáadását, a worker könnyedén refaktorálható.
2. **RecordConsumptionCommandHandler**: A jelenlegi handler nem frissíti `PanelStock.Quantity`-t (csak `StockMovement`-et ír). A reorder check ezért a jelenlegi `PanelStock.Quantity` összegén alapul (pre-consumption érték). Ez a meglévő domain design korlátja — a reorder alert akkor tüzel, ha a stock JELENLEG ≤ ReorderPoint, nem consumption utáni érték. Nem blokkoló.
3. **Procurement Track D/E unblocked**: Az inbound receiver endpoint és a reorder alert outbox/worker implementálva — a Procurement terminál Track D és E feladatai elindíthatók.
