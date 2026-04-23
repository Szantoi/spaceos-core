---
id: MSG-KERNEL-062-DONE
from: kernel
to: root
type: response
status: UNREAD
ref: MSG-KERNEL-062
created: 2026-04-13
---

# MSG-KERNEL-062-DONE — E2E maradék Kernel hibák

## Összefoglaló

**Root cause (issues 1, 3–4):** `ClaimsTenantResolver.TryResolve()` csak a `tid` JWT claim-et olvasta,
míg `TenantSessionInterceptor` a `spaceos_tenants` claim-et. Keycloak eltérő UUID-kat ad vissza a két
claim-ben, ezért:
- `AppDbContext` globális query filter (`HasQueryFilter`) a `tid`-UUID-t használta
- PostgreSQL `app.current_tenant_id` session variable a `spaceos_tenants`-UUID-t kapta
→ UUID mismatch → AggregateSnapshot RLS-INSERT elutasítva (FlowEpic close → 500)
→ Summary query: filter UUID ≠ session UUID → count = 0

**Root cause (issue 2 — node register):** `TenantSessionInterceptor` az `X-SpaceOS-Active-Tenant`
header értékét raw string `OrdinalIgnoreCase` összehasonlítással ellenőrizte. Ha a header GUID
formátuma eltér a claim-ben tárolt formátumtól (zárójelek, nagybetűk stb.) → nem talált egyezést
→ `UnauthorizedAccessException` → 500.

## Változtatások

| File | Változás |
|------|---------|
| `SpaceOS.Infrastructure/Auth/ClaimsTenantResolver.cs` | `TryResolve()` most elsőként a `spaceos_tenants` JSON array claim-et olvassa (double-deserialization support), fallback: `tid` claim |
| `SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs` | `X-SpaceOS-Active-Tenant` header összehasonlítás GUID-normalizálva: `Guid.TryParse()` mindkét oldalon |
| `SpaceOS.Kernel.Tests/Infrastructure/Auth/ClaimsTenantResolverTests.cs` | 4 új teszt: `spaceos_tenants` primary path, double-wrapped JSON, claim prioritás, malformed JSON fallback |

## Tesztek

- **1075 teszt zöld** (881 unit + 101 integration + 93 API integration), 0 failed, 4 pre-existing skip
- 4 új teszt hozzáadva: `ClaimsTenantResolverTests`

## Security review

- [x] Input validation: `Guid.TryParse()` mind a header, mind a claim oldalon — nincs raw cast
- [x] JSON deserialization: try/catch `JsonException` → graceful fallback
- [x] Authorization: `UnauthorizedAccessException` path megmarad — érvénytelen header → 500 (szándékos)
- [x] RLS: a fix után `app.current_tenant_id` és a globális filter ugyanazt az UUID-t használja
- [x] SQL injection: nincs változás — paraméteres `set_config` megmarad
- [x] Sensitive data: tenant UUID logba kerülhet (warning szinten), ez elfogadott

## Git

- Branch: `develop`
- Commit: `8dd0bd7` — "fix: ClaimsTenantResolver reads spaceos_tenants first — RLS/filter UUID mismatch (MSG-062)"
- Push: OK → `origin/develop`

## Definition of Done — állapot

| Feladat | Státusz |
|---------|---------|
| `05-flowepic-lifecycle` PUT /close → 200 | Fix deploy után verifikálható (RLS mismatch javítva) |
| `15-nodes-sync` POST /nodes/register → 201/409 | Fix deploy után verifikálható (GUID comparison javítva) |
| `24-tenant-summary` flowEpicCount/activeWorkstationCount | Fix deploy után verifikálható (filter = session UUID) |
| 1075 teszt zöld | ✅ |
| Commit + push | ✅ |

## Kockázatok / kérdések

A VPS-en a javítás csak deploy után verifikálható E2E-vel. Ha a `tid` claim egyáltalán nincs jelen
Keycloak tokenben (csak `spaceos_tenants`), akkor az eddigi `tid`-alapú kód mindig `null`-t adott
vissza — a globális filter ilyenkor átengedte az összes sort (`CurrentTenantGuid == null` branch).
A javítás után ez konsisztensen a `spaceos_tenants`-UUID-t adja vissza, ami a helyes viselkedés.
