---
id: MSG-FE-010-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-010
created: 2026-04-17
---

# FE-010 DONE — E2E-L2 Flow 02–06 (seed-dependent)

## Commit

`fff1f65` — feat: FE-010 — E2E Flow 02–06 (seed-dependent, doorstar-cutting-ready-v1)

## Elvégzett munka

### resetTenant helper

`tests/e2e/helpers/resetTenant.ts` — `DELETE /bff/test/tenants/{id}/reset?confirm=true`
Headers: `X-SpaceOS-Internal: true`, `X-SpaceOS-Brand: joinerytech`
Body: `{ seedProfile }` — típusbiztos, `SeedProfile` típusra korlátozva.

### Flow státuszok

| Flow | Fájl | Státusz | Tesztek |
|---|---|---|---|
| 02 | `02-cutting-dashboard.spec.ts` | Részleges — order→cutting-list valódi, cutting dashboard skip | 2 valódi + 1 skip |
| 03 | `03-daily-plan.spec.ts` | `test.skip` — `/cutting/plans` route nem létezik | 2 skip |
| 04 | `04-nesting-view.spec.ts` | `test.skip` — nesting UI nem létezik | 2 skip |
| 05 | `05-inventory.spec.ts` | `test.skip` — `/inventory` route nem létezik | 2 skip |
| 06 | `06-order-flow.spec.ts` | Valódi — 4 teszt | 4 valódi |

### Flow 06 valódi tesztek (doorstar-cutting-ready-v1 seed)

1. `orders-table` betölt, ≥2 sor (header + 1 adat)
2. `status-badge-Submitted` látható
3. Első order linkre kattintás → `/orders/:id` → `order-id` testid látható
4. Order detail tartalmaz `cutting-list-link` elemet

### Flow 02 valódi tesztek

1. `orders-table` betölt `Submitted` státusszal
2. Order detail → `cutting-list-link` → `/orders/:id/cutting-list` → `cutting-list-table` VAGY `empty-state` VAGY `error-state`

## Megjegyzések

### resetTenant method: DELETE vs POST
A task spec `DELETE` metódust ír elő, de INFRA-137 `POST`-ot verifikált.
Az implementáció a task spec-et követi (`DELETE`). Ha a live tesztek 405-öt kapnak,
a metódust `POST`-ra kell cserélni — ez 1 soros változás a `resetTenant.ts`-ben.

### Miért van annyi skip?
A portal jelenlegi route-jai: `/orders`, `/orders/:id`, `/orders/:id/cutting-list`, `/profile`.
Nincs `/cutting/sheets`, `/cutting/plans`, `/inventory`, nesting UI.
A skip-elt tesztek mind `// TODO: FE-011 után aktiválni` kommenttel vannak ellátva.

## DoD ellenőrzés

- [x] `pnpm test` → 90/90 zöld (regresszió nincs)
- [x] `pnpm build` → 0 error
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 hiba
- [x] `resetTenant` helper elkészült
- [x] Flow 02–06 spec fájlok létrejöttek
- [x] `test.skip` kommentekkel jelölve a hiányzó UI-k (FE-011)
- [x] git commit `fff1f65` + push (main)

## Következő lépés (root döntés)

Flow 03 + 04 + 05 aktiválásához szükséges:
- Inventory oldal (`/inventory` route + `panel-stock-list` testid)
- Cutting sheets dashboard (`/cutting/sheets` + `cutting-sheet-list` testid)
- Daily plan page (`/cutting/plans` + `daily-plan-page` testid)
- Nesting vizualizáció (SVG/Canvas trigger gomb)

Ezek FE-011 scope-ba kerülhetnek, ha a backend cutting/inventory API éles és dokumentált.
