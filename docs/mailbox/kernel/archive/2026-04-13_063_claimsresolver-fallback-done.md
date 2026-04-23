---
id: MSG-KERNEL-063-DONE
from: kernel
to: root
type: response
status: READ
ref: MSG-KERNEL-063
created: 2026-04-13
---

# MSG-KERNEL-063-DONE — ClaimsTenantResolver graceful fallback

## Root cause

A `8dd0bd7` commitban a `JsonSerializer.Deserialize<List<TenantClaimDto>>()` hívás
`JsonException`-t dobott, ha a `spaceos_tenants` JSON array nem tartalmazta a
`TenantClaimDto` összes `required` propertyjét (`tenant_type`, `enabled_modules`,
`brand_skin`). A catch blokk a `tid` claim-re esett vissza — de ha `tid` sem volt
jelen (pl. test-admin token), a resolver `null`-t adott vissza → üres lista, 500.

## Változtatások

| File | Változás |
|------|---------|
| `ClaimsTenantResolver.cs` | `JsonDocument`-alapú parsing — csak `tenant_id` mezőt olvas, nem igényel teljes TenantClaimDto schemát; `groups` claim fallback; catch-all handler; soha nem throw |
| `TenantSessionInterceptor.cs` | `X-SpaceOS-Active-Tenant` összehasonlítás: GUID-normalizálás (primary) + `string.Equals` fallback (non-standard format) |
| `ClaimsTenantResolverTests.cs` | 15 teszt: spaceos_tenants (plain, minimal JSON, double-wrap, priority, malformed, empty array), tid (valid, malformed, empty GUID), groups (/spaceos-uuid, bare UUID, non-UUID), no claims |

## Tesztek

- **1084 teszt zöld** (890 unit + 101 integration + 93 API), 0 failed, 4 pre-existing skip
- +9 új teszt (volt 5, most 15 teszt a ClaimsTenantResolverTests-ben)

## Security review

- [x] JsonDocument.Parse exception → catch all → graceful null return, nem throw
- [x] groups claim UUID extraction: `Guid.TryParse` guard + `!= Guid.Empty` check
- [x] string comparison: OrdinalIgnoreCase, nincs raw concat
- [x] Sensitive data: tenant UUID log warning szinten — elfogadott

## Git

- Branch: `develop`
- Commit: `316f603` — "fix: ClaimsTenantResolver graceful fallback — JsonDocument parsing + groups claim (MSG-063)"
- Push: OK → `origin/develop`

## Kockázatok / kérdések

A VPS-en deploy után E2E-vel verifikálható. Ha a test-admin tokenben nincs
`spaceos_tenants` és nincs `tid` sem — csak `groups` — a groups fallback fogja
megoldani. Ha a groups claim formátuma eltér a `/spaceos-{uuid}` / bare UUID
formátumoktól, jelezd vissza (MSG-064-BLOCKED).
