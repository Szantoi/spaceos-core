---
id: MSG-ORCH-070-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-070
created: 2026-04-15
---

# MSG-ORCH-070 — Cutting BFF proxy route-ok DONE

## Összefoglaló

3 új proxy route létrehozva, regisztrálva, tesztelve. Commit pushed.

## Implementáció

| Route | Cél | Fájl |
|---|---|---|
| `/bff/inventory/*` | `http://127.0.0.1:5004/api/*` | `src/routes/inventory.route.ts` |
| `/bff/cutting/*` | `http://127.0.0.1:5005/api/*` | `src/routes/cutting.route.ts` |
| `/bff/procurement/*` | `http://127.0.0.1:5006/api/*` | `src/routes/procurement.route.ts` |

Mindhárom: `proxyLimiter + requireAuth + X-SpaceOS-Brand forwarding + 502 error handler`

## Env változók

`src/config/env.ts` + `.env.example` frissítve:
- `INVENTORY_BASE_URL` (default: `http://127.0.0.1:5004`)
- `CUTTING_BASE_URL` (default: `http://127.0.0.1:5005`)
- `PROCUREMENT_BASE_URL` (default: `http://127.0.0.1:5006`)

## Build / Teszt

- `npm run build` → 0 TS error ✅
- `npm test` → 207/207 zöld ✅

## Commit

`d825ab1` — pushed to `origin develop`

## ⚠️ VPS deploy szükséges

A futó instance még a régi kódot futtatja (404 helyett 401 jön `/bff/inventory/stock`-ra).

```bash
pm2 restart spaceos-orchestrator
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:3000/bff/inventory/stock
# Várt eredmény: 401
```

Kérlek operátor futtassa a pm2 restart parancsot.
