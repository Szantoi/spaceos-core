---
id: MSG-INVENTORY-001-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-001
created: 2026-04-16
---

## Összefoglaló

Implementálva: `DELETE /internal/panel-stocks/by-tenant/{tenantId}?confirm=true`

Érintett fájlok:
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/InternalEndpoints.cs` — új fájl, endpoint implementáció
- `src/SpaceOS.Modules.Inventory.Api/Program.cs` — `app.MapInternalInventoryEndpoints()` hozzáadva
- `tests/SpaceOS.Modules.Inventory.Tests/Api/InventoryWebFactory.cs` — új, in-memory WebApplicationFactory
- `tests/SpaceOS.Modules.Inventory.Tests/Api/InternalDeleteByTenantTests.cs` — 6 új integrációs teszt

Commit: `0ab148b`

## Tesztek

53 teszt futott, mind zöld. 6 új integrációs teszt:
1. `Delete_WithoutInternalHeader_Returns403`
2. `Delete_WithoutConfirm_Returns400`
3. `Delete_TenantNotInAllowlist_Returns403`
4. `Delete_ValidRequest_Returns200WithCounts`
5. `Delete_NoExistingData_Returns200WithZeroCounts`
6. `Delete_InvalidTenantIdFormat_Returns400`

```
Test Run Successful.
Total tests: 53
     Passed: 53
 Total time: 2.2760 Seconds
```

## Security review

- `X-SpaceOS-Internal` header hiánya → 403 + LogWarning (IP loggolva)
- `confirm=true` hiánya → 400
- Érvénytelen GUID formátum → 400
- `TEST_TENANT_ALLOWLIST` allowlist ellenőrzés → nincs benne: 403 + LogWarning (tenant ID loggolva)
- `.AllowAnonymous()` az endpoint szinten — auth header alapú, nem JWT
- Törlési sorrend: StockMovements → Offcuts → PanelStocks (FK-safe)
- Nincs secret a logban

## Kockázatok / kérdések

Nincsenek.
