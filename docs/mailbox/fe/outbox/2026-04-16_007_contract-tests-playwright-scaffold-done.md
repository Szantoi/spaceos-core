---
id: MSG-FE-007-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-007
created: 2026-04-16
---

# FE-007 DONE — Contract Tests (MSW + Zod) + Playwright E2E scaffold

## Összefoglalás

Contract tesztréteg (MSW + Zod) és Playwright E2E infrastruktúra scaffold kész.
90 teszt zöld (87 unit + 3 új contract), build/lint/typecheck 0 hiba.

## DoD ellenőrzőlista

- [x] `pnpm add -D msw zod` — függőségek telepítve (msw 2.13.4, zod 4.3.6)
- [x] `tests/contract/setup.ts` + `auth.handlers.ts` + `authMe.schema.ts` létrejön
- [x] `auth-contract.spec.ts` — 3 contract teszt futó és zöld
- [x] `pnpm test` → unit + contract együtt zöld (vitest include mindkettőt)
- [x] `@playwright/test` telepítve (1.59.1), `playwright.config.ts` kész
- [x] `tests/e2e/global-setup.ts` + `auth.fixture.ts` + `01-login.spec.ts` létrejön
- [x] `pnpm test:e2e` parancs konfigurált
- [x] `.github/workflows/ci.yml` létrehozva (unit + contract fut CI-ban, E2E nem — SEC-TS-05)
- [x] `pnpm build` → 0 error
- [x] `pnpm test` (unit + contract) → 90 teszt zöld (3 új contract)
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit + push

## Build output

```
dist/assets/index-CRp5PkT8.css   13.35 kB │ gzip:   3.58 kB
dist/assets/index-BbOmw1_l.js   356.55 kB │ gzip: 109.79 kB
✓ built in 1.70s
```

## Test eredmény

```
Test Files  19 passed (19)
     Tests  90 passed (90)   (+3 új contract teszt)
  Duration  24.85s
```

## Commit

```
3a0b931 feat: Contract tests (MSW + Zod) + Playwright E2E scaffold
```

## Technikai megjegyzések

**Zod v4 UUID validáció:** A Zod v4 szigorúbb UUID regex-et használ (`[1-8]` version bit,
`[89abAB]` variant bit). Az MSW handler adatokban valid UUID v4 formátumot kellett alkalmazni
(`00000000-0000-4000-8000-000000000001` mintára).

**Contract teszt node env:** `vitest.config.ts` `environmentMatchGlobs` alapján a
`tests/contract/**` node környezetben fut (nem jsdom). MSW handler abszolút URL-t használ
(`http://localhost/bff/auth/me`), keycloak.config.ts `vi.mock`-kal izoláva.

**E2E react-hooks ESLint:** Playwright `use` callback false-positive react-hooks/rules-of-hooks
triggert okoz. Fix: `tests/e2e/**` ESLint override, `react-hooks/rules-of-hooks: off`.

## Új fájlok

| Fájl | Leírás |
|---|---|
| `tests/contract/setup.ts` | MSW setupServer export |
| `tests/contract/handlers/auth.handlers.ts` | /bff/auth/me MSW node handler (abszolút URL) |
| `tests/contract/schemas/authMe.schema.ts` | Zod AuthMeResponseSchema + AuthMeResponse type |
| `tests/contract/auth-contract.spec.ts` | 3 contract teszt (schema, roles, tenantId UUID) |
| `tests/e2e/playwright.config.ts` | Playwright konfig, chromium, globalSetup |
| `tests/e2e/global-setup.ts` | Test BFF reset stub (TEST_BFF_ENABLED feature flag) |
| `tests/e2e/fixtures/auth.fixture.ts` | PKCE login auth fixture, authenticatedPage |
| `tests/e2e/flows/01-login.spec.ts` | Login flow skeleton (2 teszt) |
| `.github/workflows/ci.yml` | FE CI: typecheck + lint + unit + contract |

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `vitest.config.ts` | include + environmentMatchGlobs (contract → node) |
| `package.json` | test:unit, test:contract, test:e2e scriptek |
| `eslint.config.js` | tests/e2e override: react-hooks/rules-of-hooks off |
