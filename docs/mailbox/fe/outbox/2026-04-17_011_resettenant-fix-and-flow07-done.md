---
id: MSG-FE-011-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-011
created: 2026-04-17
---

# FE-011 DONE — resetTenant POST fix + Flow 07

## Commit

`f4ce323` — fix: FE-011 — resetTenant POST fix + Flow 07 supplier spec + CI-002

## Elvégzett munka

### 1. resetTenant.ts — DELETE → POST fix

`tests/e2e/helpers/resetTenant.ts` — `method: 'DELETE'` → `method: 'POST'`

Ez az összes seed-dependent flow reset lépését javítja (Flow 02–07 mind ezt a helper-t hívja
`beforeEach`-ből).

### 2. Flow 07 — 07-supplier-flow.spec.ts

Spec fájl létrejött, 3 `test.skip` blokkal:
- `supplier lista betölt legalább 1 elemmel`
- `supplier detail nézet betölt`
- `procurement összesítő — aktív rendelések száma látható`

Minden skip komment: `// TODO: FE-012 után aktiválni` + `// CONTRACT_ISSUE: CI-002`

### 3. CONTRACT_ISSUES.md — CI-002

| ID | Leírás |
|---|---|
| CI-002 | Supplier UI hiányzik — nincs `/suppliers` route, `GET /bff/procurement/suppliers` FE-n nem használt |

## DoD ellenőrzés

- [x] `resetTenant.ts` → `method: 'POST'`
- [x] `07-supplier-flow.spec.ts` létezik (indokolt skip, CI-002 hivatkozással)
- [x] `CONTRACT_ISSUES.md` — CI-002 bejegyzés
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 90/90 zöld
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 hiba
- [x] git commit `f4ce323` + push (main)

## CONTRACT_ISSUE

CI-002 OPEN — `/suppliers` UI route hiányzik a portálból. Root koordinálja a szükséges
backend/frontend feladatot, ha a Supplier UI prioritást kap.
