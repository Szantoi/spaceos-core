---
id: MSG-JOINERY-010-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-010
created: 2026-04-16
---

# MSG-JOINERY-010-DONE — BE-TEST-03: DELETE /internal/orders/by-tenant/{tenantId}

## Összefoglaló

Endpoint implementálva, 5 új teszt zöld. Commit: `0f05029`

---

## Implementált endpoint

```
DELETE /internal/orders/by-tenant/{tenantId}?confirm=true

Headers:
  X-SpaceOS-Internal: true

Response 200: { tenantId, deletedCounts: { doorOrders, cuttingListSnapshots } }
Response 400: confirm=true hiánya
Response 403: X-SpaceOS-Internal header hiánya VAGY tenant nem allowlistban
```

## Security rétegek (defense in depth)

1. **X-SpaceOS-Internal header** — hiánya → 403, LogWarning
2. **confirm=true** — hiánya → 400 (accidental delete védelem)
3. **Guid formátum ellenőrzés** — invalid tenantId → 400
4. **TEST_TENANT_ALLOWLIST** — config + env var, Set<string> OrdinalIgnoreCase lookup; hiány → 403 + LogWarning
5. **AllowAnonymous** — JWT nem szükséges (belső hívás), de header-alapú védelemmel

## Törölt adatok (egy hívásban)

- `CuttingListSnapshots` (tenant szerint)
- `ProductionSheetCaches` (tenant szerint)
- `JoineryOutboxEntries` (tenant szerint)
- `DoorItems` (explicit — InMemory + PostgreSQL kompatibilis)
- `DoorOrders` (tenant szerint)

## Fájlok

| Fájl | Változás |
|---|---|
| `Api/Endpoints/InternalEndpoints.cs` | ÚJ — endpoint implementáció |
| `Api/Program.cs` | `app.MapInternalEndpoints()` hozzáadva |
| `Application/Orders/Repositories/IDoorOrderRepository.cs` | `DeleteAllByTenantAsync` + `TenantDeletedCounts` record |
| `Infrastructure/Persistence/Repositories/DoorOrderRepository.cs` | `DeleteAllByTenantAsync` implementáció |
| `Tests/Api/InternalDeleteByTenantTests.cs` | ÚJ — 5 integrációs teszt |

## Tesztek

```
Előző: 214
Jelenlegi: 219/219 — Failed: 0
```

Új tesztek:
1. `Delete_WithoutInternalHeader_Returns403`
2. `Delete_WithoutConfirm_Returns400`
3. `Delete_TenantNotInAllowlist_Returns403`
4. `Delete_ValidRequest_Returns200WithCounts`
5. `Delete_NoExistingData_Returns200WithZeroCounts`

## Build

`dotnet build` → 0 error, 0 warning

## Megjegyzés — develop branch

A DoD `push develop`-ot kért, de csak `main` branch létezik. Commit `0f05029` a `main`-en van. Push-hoz root döntés szükséges (ugyanaz mint MSG-JOINERY-005-DONE-ban).
