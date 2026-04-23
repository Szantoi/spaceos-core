---
id: MSG-ORCH-072
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-E2E-039-DONE
created: 2026-04-16
---

# MSG-ORCH-072 — BFF pathRewrite fix: cutting / inventory / procurement

## Gyökérok

A `cutting.route.ts`, `inventory.route.ts`, `procurement.route.ts` mindegyikében
a pathRewrite hibás: `'^/' → '/api/'`.

Az Express `/bff/cutting` mount-pont lestrippeli a prefixet → router `/sheets`-t kap →
pathRewrite: `/api/sheets` → de a cutting service `/api/cutting/sheets`-t vár → **404**.

Ugyanígy inventory (`/api/inventory/...`) és procurement (`/api/procurement/...`).

## Szükséges javítások

| Fájl | Jelenlegi | Helyes |
|---|---|---|
| `src/routes/cutting.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/cutting/'` |
| `src/routes/inventory.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/inventory/'` |
| `src/routes/procurement.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/procurement/'` |

## Service endpoint struktúra (referencia)

```
spaceos-cutting-svc  (5005):  POST /api/cutting/sheets
                               GET  /api/cutting/sheets/{id}/nesting
                               GET  /api/cutting/sheets/{id}/status
                               GET  /api/cutting/waste
                               POST /api/cutting/plans
                               GET  /api/cutting/plans/{date}

spaceos-inventory    (5004):  GET  /api/inventory/stock
                               GET  /api/inventory/offcuts
                               POST /api/inventory/movements/consumption
                               POST /api/inventory/movements/inbound
                               POST /api/inventory/movements/offcut
                               GET  /api/inventory/trend

spaceos-procurement  (5006):  POST /api/procurement/orders
                               GET  /api/procurement/orders/{id}
                               GET  /api/procurement/prices
                               POST /api/procurement/deliveries
```

## Ellenőrzés

Build + tesztek után a 41-cutting-smoke teszteknek változatlanul zöldnek kell maradniuk.

Az E2E `42-cutting-flow` probe (`POST /bff/cutting/sheets`) elvártan 401-et (auth szükséges)
vagy 422-t/400-at (hiányos request body) ad vissza a fix után — nem 404-et.
Ez elegendő ahhoz, hogy `cuttingAvailable = true` legyen és a tesztek aktiválódjanak.

## DoD

- [ ] Mindhárom route fájl pathRewrite javítva
- [ ] `npm run build` → 0 TS error
- [ ] `npm test` → minden teszt zöld (legalább 207)
- [ ] Outbox: `MSG-ORCH-072-DONE`
