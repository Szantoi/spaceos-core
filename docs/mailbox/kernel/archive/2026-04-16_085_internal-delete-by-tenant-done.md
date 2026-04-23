---
id: MSG-KERNEL-085-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-085
created: 2026-04-16
---

## Összefoglaló

BE-TEST-02 implementálva: `DELETE /internal/flow-epics/by-tenant/{tenantId}`

Commit: `130959a` (develop) — pushed to origin.

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/Repositories/IFlowEpicRepository.cs` | `DeleteAllByTenantAsync` + `TenantDeletedCounts` record — már deklarálva volt |
| `SpaceOS.Infrastructure/Data/Repositories/FlowEpicRepository.cs` | `DeleteAllByTenantAsync` implementálva: SnapshotCount + EpicCount + AuditCount |
| `SpaceOS.Kernel.Api/Endpoints/InternalEndpoints.cs` | Új fájl — 4 gate: X-SpaceOS-Internal + confirm=true + GUID parse + TEST_TENANT_ALLOWLIST |
| `SpaceOS.Kernel.Api/Program.cs` | `app.MapInternalEndpoints()` regisztrálva |
| `SpaceOS.Kernel.Api.Tests/Endpoints/InternalDeleteByTenantEndpointTests.cs` | 5 integációs teszt (InternalTestFactory SQLite in-memory) |

**Endpoint:** `DELETE /internal/flow-epics/by-tenant/{tenantId}?confirm=true`
**Headers:** `X-SpaceOS-Internal: true` kötelező
**Config:** `TEST_TENANT_ALLOWLIST` env var (CSV)

**Response 200:**
```json
{
  "tenantId": "uuid",
  "deletedCounts": { "flowEpics": N, "snapshots": N, "auditEvents": N }
}
```

## Tesztek

**1121/1121 passing** (910 unit + 107 integration + 104 api-tests, 4 skip változatlan)

Új tesztek (5):
- `DeleteByTenant_ValidRequest_Returns200WithCounts` — seeded 2 FlowEpic + 1 Snapshot, counts visszaellenőrizve
- `DeleteByTenant_MissingInternalHeader_Returns403`
- `DeleteByTenant_MissingConfirmParam_Returns400`
- `DeleteByTenant_TenantNotInAllowlist_Returns403`
- `DeleteByTenant_InvalidGuid_Returns400`
- `DeleteByTenant_NoData_Returns200WithZeroCounts`

## Security review

- **X-SpaceOS-Internal header**: Gate 1 — hiány → 403
- **confirm=true**: Gate 2 — véletlenszerű hívás ellen
- **GUID format**: Gate 3 — invalid tenantId → 400 (nem 500)
- **TEST_TENANT_ALLOWLIST**: Gate 4 — defense in depth, ha az Orchestrator kompromittálódna
- **IgnoreQueryFilters**: szándékos — EF Core tenant filter bypass csak ennél az endpointnál, a tenantGuid paraméter explicit WHERE feltételként
- **AuditDbContext**: opcionális injektálás — ha nincs (test context), auditCount=0, nem crash
- **AllowAnonymous**: szándékos — a JWT auth helyett a belső header + allowlist véd
- **No sensitive data in logs**: nincs token/secret logolás

## Kockázatok / kérdések

Nincsenek. Az endpoint a prod környezetben is megvan, de `TEST_TENANT_ALLOWLIST` env var nélkül üres allowlist → minden hívás 403.
