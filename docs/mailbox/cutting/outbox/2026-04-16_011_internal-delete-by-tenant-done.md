---
id: MSG-CUTTING-011-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-011
created: 2026-04-16
---

## Összefoglaló

`DELETE /internal/cutting-sheets/by-tenant/{tenantId}` endpoint implementálva 4 rétegű security-vel.

### Új fájlok

**`src/SpaceOS.Modules.Cutting.Api/Endpoints/InternalEndpoints.cs`**

Endpoint: `DELETE /internal/cutting-sheets/by-tenant/{tenantId}?confirm=true`
Biztonsági rétegek sorrendben:
1. `X-SpaceOS-Internal: true` header → hiány: 403
2. `?confirm=true` query param → hiány: 400
3. GUID formátum → érvénytelen: 400
4. `TEST_TENANT_ALLOWLIST` env var allowlist lookup → nincs benne: 403 + LogWarning

Sikeres törlés response (200):
```json
{ "tenantId": "...", "deletedCounts": { "cuttingSheets": 3, "dailyCuttingPlans": 1 } }
```

**`tests/SpaceOS.Modules.Cutting.Tests/Api/InternalEndpointsTests.cs`** — 6 új teszt:
- `Delete_MissingInternalHeader_Returns403` ✅
- `Delete_WrongHeaderValue_Returns403` ✅
- `Delete_MissingConfirm_Returns400` ✅
- `Delete_InvalidGuid_Returns400` ✅
- `Delete_TenantNotInAllowlist_Returns403` ✅
- `Delete_ValidRequest_Returns200WithCounts` ✅

### Módosított fájlok

**`ICuttingRepository.cs`**: `DeleteByTenantAsync(Guid tenantId, ct)` → `(int CuttingSheets, int DailyCuttingPlans)` hozzáadva

**`CuttingRepository.cs`**: implementáció — `DailyCuttingPlans` + `CuttingSheets` törlése tenant szerint, `RemoveRange` + `SaveChangesAsync`

**`Program.cs`**: `app.MapInternalEndpoints()` hozzáadva

### Commit

`745d387` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Security review

- `Results.StatusCode(403)` használva `Results.Forbid()` helyett (utóbbi auth middleware nélkül dob)
- `AllowAnonymous()` az endpoint-on — az auth a header/allowlist ellenőrzésen alapul, nem JWT-n
- Allowlist env var nulla vagy üres → minden tenant visszautasítva (fail-closed)
- Törölt entitások: `CuttingSheets` + `DailyCuttingPlans` (+ cascade: `CuttingLines`, `CuttingBatches` EF cascade delete-tel)
- Nincsenek nyílt SQL stringek — EF LINQ query-k

## Kockázatok / megjegyzések

Nincs. A JOINERY-010 mintát követi, azonos security model.
