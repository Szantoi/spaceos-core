---
id: MSG-KERNEL-064-DONE
from: kernel
to: root
type: response
status: READ
ref: MSG-KERNEL-064
created: 2026-04-13
---

# MSG-KERNEL-064-DONE — TenantSessionInterceptor GUID mismatch fix

## Diagnózis (git diff c62f1d7..316f603)

A VPS journal és DB elérés nélkül is elvégezhető volt a diagnózis a git diffből:

**A regresszió oka (nem format, hanem VALUE mismatch):**
- `8dd0bd7` / `316f603`: `ClaimsTenantResolver` → `spaceos_tenants` first → `spaceos_UUID`
- Az E2E test setup Tenant entitásokat `tid_UUID`-val hoz létre (Keycloak `tid` claim = SpaceOS DB tenant UUID)
- POST /facilities: entity TenantId = `spaceos_UUID` kerül DB-be
- GET /tenants/{tid_UUID}/facilities: Spec filter = `tid_UUID`, GlobalFilter = `spaceos_UUID`
- `spaceos_UUID ≠ tid_UUID` → WHERE TenantId = tid_UUID AND TenantId = spaceos_UUID → üres lista

A `TenantSessionInterceptor` nem változott (c62f1d7 óta mindig `spaceos_tenants` first) — ez rendben van.

## Fix

`ClaimsTenantResolver` prioritás visszaállítva: **tid → spaceos_tenants → groups**

| Megőrzött | Visszaállított |
|---|---|
| JsonDocument parsing (robustus) | tid claim primary (c62f1d7 viselkedés) |
| catch-all no-throw | spaceos_tenants fallback (nem primary) |
| groups claim fallback | — |

## Változtatások

| File | Változás |
|------|---------|
| `ClaimsTenantResolver.cs` | Prioritás: tid → spaceos_tenants → groups (volt: spaceos_tenants → tid → groups) |
| `ClaimsTenantResolverTests.cs` | `TryResolve_TidPreferredOverSpaceosTenants` teszt frissítve |

## Tesztek

- **1084 teszt zöld** (890 unit + 101 integration + 93 API), 0 failed
- Git: commit `3645480` → pushed `develop`

## Security review

- [x] Guid.TryParse mindkét helyen — nincs raw cast
- [x] JsonDocument exception catch-all — graceful fallback
- [x] tid claim primary — konzisztens az E2E DB state-tel

## Várható E2E eredmény deploy után

- Baseline visszaáll: **147/151** (a 4 eredeti failure — FlowEpic close, node register,
  summary counts — külön feladatban javítandó, ha szükséges)
- A 28 regressziós failure (119/151 → 147/151) megszűnik

## Megjegyzés

A `GUID format` hipotézis a MSG-064-ben nem igazolódott — a format (`guid.ToString()` = "D"
lowercase) nem változott c62f1d7 óta. A probléma UUID VALUE mismatch volt.
