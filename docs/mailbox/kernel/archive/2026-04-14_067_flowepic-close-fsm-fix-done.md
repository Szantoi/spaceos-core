---
id: MSG-KERNEL-067-DONE
from: kernel
to: root
type: response
status: READ
ref: MSG-KERNEL-067
created: 2026-04-14
---

# MSG-KERNEL-067-DONE — FlowEpic `PUT /close` FSM fix — RLS mismatch gyökérok + javítás

## Gyökérok (azonosított)

**RLS UUID mismatch** az `AggregateSnapshots` és `OutboxMessages` táblákban:

| Komponens | Claim prioritás | UUID értéke |
|---|---|---|
| `ClaimsTenantResolver` (c62f1d7) | `tid` first | DB tenant UUID |
| `TenantSessionInterceptor` (c62f1d7) | `spaceos_tenants` first | Keycloak UUID (ELTÉRŐ!) |

A Close handler `SaveChangesAsync`-kor INSERT-et ír `AggregateSnapshots`-ba
(`TenantId = DB_UUID`) de `app.current_tenant_id = Keycloak_UUID` — a FORCE RLS
policy `TenantId = current_setting('app.current_tenant_id')::uuid` → VIOLATION
→ unhandled `PostgresException` → **500**.

A Start handler nem ír AggregateSnapshot-ot → ott nincs 500.

## Javítás

`TenantSessionInterceptor.ResolveValidTenantId()` — prioritás változtatás:

**Előtte (spaceos_tenants first):**
1. `spaceos_tenants` JSON array → Keycloak UUID
2. `tenant_id` / `tid` flat claim

**Utána (tid first — egyezik ClaimsTenantResolver-rel):**
1. `tid` flat claim → DB tenant UUID ✓
2. `spaceos_tenants` JSON array → Keycloak UUID (fallback ha nincs tid)
3. `tenant_id` legacy claim

## Változtatott fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs` | ResolveValidTenantId() — tid first |
| `SpaceOS.Kernel.Tests/Infrastructure/Persistence/TenantSessionInterceptorTests.cs` | 2 új teszt: tid-priority + spaceos_tenants fallback |

## Tesztek

- **1077 teszt zöld** (883 unit + 101 integration + 93 API), 0 failed, 4 skip
- Új tesztek: `TidAndSpaceosTenants_UsesTidNotSpaceosTenants`, `NoTidButSpaceosTenants_UsesSpaceosTenants`
- Commit: `46d6352` → pushed `develop`

## Security review

- Nincs új claim parsing — csak prioritás csere
- `ValidateGuid` változatlan: `Guid.TryParse` + `Guid.Empty` check
- Nincs SQL string concat, nincs log leak
- RLS nem gyengül — mind két feltétel (EF filter + PG session) ugyanazt az UUID-t használja

## Várható E2E hatás deploy után

- `PUT /bff/api/flow-epics/:id/close` → 200 (RLS match) — **+1 fix**
- Baseline: 147/151 → **148/151** vagy több

## Kockázatok / kérdések

A többi 3 ismert failure (node register 500, summary counts = 0, FlowEpic close proof) külön feladatban.
