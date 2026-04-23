---
id: MSG-PROCUREMENT-001-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-001
created: 2026-04-16
---

# DONE — BE-TEST-06: `DELETE /internal/purchase-orders/by-tenant/{tenantId}`

## Összefoglaló

Implementálva a JOINERY-010 mintát követve. Módosított/létrehozott fájlok:

| Fájl | Változás |
|---|---|
| `src/.../Api/Endpoints/InternalEndpoints.cs` | **ÚJ** — endpoint implementáció |
| `src/.../Api/Program.cs` | `app.MapInternalEndpoints()` regisztráció |
| `src/.../Domain/Interfaces/IProcurementRepository.cs` | `TenantDeletedCounts` record + `DeleteAllByTenantAsync` metódus |
| `src/.../Infrastructure/Repositories/ProcurementRepository.cs` | `DeleteAllByTenantAsync` implementáció |
| `tests/.../Api/InternalEndpointsTests.cs` | **ÚJ** — 6 teszt |

Commit: `39bc9db` — push kihagyva (nincs remote konfigurálva).

## Tesztek

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

Új tesztek (6 db):
1. `DeleteByTenant_MissingInternalHeader_Returns403`
2. `DeleteByTenant_MissingConfirm_Returns400`
3. `DeleteByTenant_InvalidGuid_Returns400`
4. `DeleteByTenant_TenantNotInAllowlist_Returns403`
5. `DeleteByTenant_ValidRequest_Returns200WithCounts`
6. `DeleteByTenant_EmptyAllowlist_Returns403`

## Security review

| Réteg | Státusz |
|---|---|
| X-SpaceOS-Internal header kötelező | ✅ hiány → 403 + LogWarning |
| confirm=true kötelező | ✅ hiány → 400 |
| GUID formátum validáció | ✅ invalid → 400 |
| TEST_TENANT_ALLOWLIST allowlist lookup | ✅ nincs benne → 403 + LogWarning |
| Törlési sorrend (FK-safe) | ✅ Deliveries → PurchaseOrders |
| Secret nem kerül logba | ✅ csak tenantId és IP kerül naplóba |
| AllowAnonymous (nem JWT) | ✅ auth header-alapú, nem JWT |

## Kockázatok / kérdések

Nincsenek. A push nem volt lehetséges (nincs remote), ezt operátornak kell elvégeznie.
