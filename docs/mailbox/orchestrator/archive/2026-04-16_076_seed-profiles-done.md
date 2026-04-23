---
id: MSG-ORCH-076-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-076
created: 2026-04-16
---

## Összefoglaló

BE-TEST-07 implementálva: seed profiles `empty-v1` + `doorstar-smoke-v1` + stub profilok.

| Fájl | Változás |
|---|---|
| `src/routes/test.route.ts` | Teljes refactor: statikus SEED_PROFILES → async SeedFn map · `getSeedToken()` helper · unknown → 400 (vol 404) |
| `src/routes/test.route.test.ts` | Test 5 frissítve (404→400) + 3 új teszt (7, 8, 9) |
| `src/config/env.ts` | `KC_TOKEN_URL`, `TEST_RUNNER_CLIENT_ID`, `TEST_RUNNER_CLIENT_SECRET` optional hozzáadva |
| `.env.example` | Test infra env var dokumentálva |

**Commit:** `210fd81` + `e500a4f` — develop branch, pushed ✅

### Seed profilok

| Profil | Viselkedés |
|---|---|
| `empty-v1` | Nincs seed hívás → `{ orders:0, panelStocks:0, suppliers:0 }` |
| `doorstar-smoke-v1` | KC token → Kernel FlowEpic → Joinery DoorOrder → `{ orders:1, ... }` |
| `doorstar-cutting-ready-v1` | Stub (BE-TEST-08 bővíti) → `{ orders:1, panelStocks:2, suppliers:1 }` |
| `doorstar-order-lifecycle-v1` | Stub (BE-TEST-08 bővíti) → `{ orders:3, ... }` |
| Ismeretlen | 400 + `available: [...]` |

### Seed step failure handling

Ha a seed axios hívás kivételt dob → `502 Seed failed` (nem 200 graceful).
Ez szándékos: a törlés graceful, a seed kritikus (ha nem sikerül, nincs tiszta állapot).

## Tesztek

**217/217 passing** (volt 214, +3 új)

Új tesztek:
- `empty-v1 → seededEntities all zeros, no seed HTTP calls` ✅
- `doorstar-smoke-v1 → seededEntities.orders === 1, 3 axios.post calls` ✅
- `seed step failure → 502` ✅

Frissített:
- Test 5: `unknown seedProfile → 400 with available list` (volt 404)

## Security review

- `KC_TOKEN_URL` client credentials flow — nem Resource Owner Password (user/pass nincs kódban)
- `TEST_RUNNER_CLIENT_SECRET` optional, env var-ból jön, nincs kódban hardcode
- `getSeedToken()` csak belső seed híváshoz használatos, response nem kerül klienshez
- Seed error response: nincs token/secret szivárogtatás a 502 body-ban
- Seed profilok enumerálhatók (`available` key) — szándékos, nem security risk (csak test env-en él)

## Kockázatok / kérdések

**1 nyitott kérdés a INFRA terminálnak:**

A VPS `.env` fájlban szükséges:
```
KC_TOKEN_URL=https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/token
TEST_RUNNER_CLIENT_ID=test-runner
TEST_RUNNER_CLIENT_SECRET=<Keycloak test-runner client secret>
```

A `test-runner` client-et az INFRA-124 hozta létre Keycloak-ban — a client secret értékét az INFRA terminál tudja.
Ha a secret nincs beállítva, a `doorstar-smoke-v1` seed hívás 401-et kap Keycloak-tól.
Az `empty-v1` profil env var nélkül is működik.
