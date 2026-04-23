---
id: MSG-ORCH-074-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-074
created: 2026-04-16
---

## Összefoglaló

BE-TEST-01 implementálva: `/bff/test/*` route + `testGuard` middleware + reset handler.

Módosított/létrehozott fájlok:

| Fájl | Változás |
|---|---|
| `src/config/env.ts` | 3 új opcionális env var: `SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER`, `TEST_SEEDER_SECRET`, `TEST_TENANT_ALLOWLIST` |
| `src/middleware/testGuard.ts` | Új — 3 rétegű guard: feature flag + seeder secret + tenant allowlist |
| `src/routes/test.route.ts` | Új — `POST /tenants/:tenantId/reset` graceful degradation + seed profile validáció |
| `src/routes/test.route.test.ts` | Új — 7 teszt |
| `src/index.ts` | `app.use('/bff/test', testRouter)` regisztráció + startup WARNING import |

**testGuard 3 réteg:**
1. Feature flag: `SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER !== 'true'` → 404
2. Seeder secret: `X-Test-Seeder-Secret` header egyezés → 403
3. Tenant allowlist: `TEST_TENANT_ALLOWLIST` CSV listában kell lennie → 403

**Graceful degradation:** ha egy modul delete endpoint 404-et ad (pl. még nincs implementálva), log + folytat, nem dob 5xx-et.

## Tesztek

**214/214 passing** (korábban 183, most 214 — +31 teszt az előző sprint óta, ebből +7 az ORCH-074)

Új tesztek (7):
- feature flag disabled → 404
- invalid x-test-seeder-secret → 403
- missing x-test-seeder-secret header → 403
- tenant not in allowlist → 403
- unknown seedProfile → 404 with message
- valid request → 200 with correct shape
- module delete returns 404 → graceful degradation, still 200

## Security review

- **Feature flag**: `process.env` közvetlen olvasás (nem `env.ts`) — szándékos, a testGuard startup-time eval előtt fut
- **Secret comparison**: string egyenlőség, nem timing-attack veszélyes kontextusban (test-only endpoint, disabled prod-ban)
- **Tenant allowlist**: nem JWT-ből jön, külön env var — megrendelő nem tud hamis tenant-t becsúsztatni JWT manipulációval
- **Startup WARNING**: `console.warn` ha `DANGER=true` — SEC-TS-02 teljesítve
- **SECURITY log**: tenant allowlist bypass kísérlet → `console.error` auditba kerül
- **Rate limiting**: `/bff/test` nem kapott dedikált rate limiter-t — a global express-rate-limit lefedi, de a testGuard 404/403 miatt prod-ban úgysem érhető el
- **No JWT required**: szándékos — a reset handler Playwright-ból fut, JWT nincs; a seeder secret + tenant allowlist véd
- **Input**: seedProfile Zod nélkül, de ismert kulcsok dict lookup-pal validálva → 404 on unknown

## Kockázatok / kérdések

Nincsenek. Az endpoint prod-ban 404-et ad (`DANGER` flag nincs beállítva), a VPS `.env`-ben `SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER=true` él (INFRA-126 DONE).

ORCH-075 (BE-TEST-07 seed profiles) az összes modul internal delete DONE után kiadható.
