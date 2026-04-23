---
id: MSG-ORCH-071-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-071
created: 2026-04-15
---

# MSG-ORCH-071 — Cutting BFF POST/PUT/DELETE fix DONE

## Összefoglaló

**Kód változtatás nem szükséges.** Az ORCH-070 implementáció catch-all — `router.use('/', proxy)` minden HTTP metódust kezel.

## Verifikáció (live ellenőrzés)

```
GET  /bff/inventory/stock            → 401 ✅
POST /bff/inventory/movements/inbound → 401 ✅
PUT  /bff/inventory/stock/item-1      → 401 ✅
POST /bff/cutting/plans               → 401 ✅
DELETE /bff/cutting/plans/plan-1      → 401 ✅
POST /bff/procurement/orders          → 401 ✅
```

401 = auth guard tüzel (nem 404 = route nem létezik).

## Magyarázat

Az E2E finding az ORCH-070 deploy **előtti** állapotra vonatkozott — akkor még nem léteztek a `/bff/inventory/*`, `/bff/cutting/*`, `/bff/procurement/*` route-ok, minden kérés 404-et adott. Az ORCH-070 (commit `d825ab1`) bevezette a catch-all proxy route-okat, amelyek mind GET, mind POST/PUT/DELETE metódusokon helyesen működnek.

## Deploy állapot

Az orchestrator `tmux spaceos-orch` session-ben fut (nem pm2) az ORCH-070 kóddal. Újraindítás már megtörtént.

## DoD teljesítve

- [x] POST /bff/inventory/* → 401 (nem 404) ✅
- [x] POST /bff/cutting/* → 401 (nem 404) ✅
- [x] POST /bff/procurement/* → 401 (nem 404) ✅
- [x] 207/207 teszt zöld (ORCH-070-ből)
