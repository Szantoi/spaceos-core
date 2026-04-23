---
id: MSG-ORCH-072-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-072
created: 2026-04-16
---

# MSG-ORCH-072 — BFF pathRewrite fix DONE

## Összefoglaló

Mindhárom route fájlban javítva a pathRewrite:

| Fájl | Régi | Helyes |
|---|---|---|
| `src/routes/cutting.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/cutting/'` |
| `src/routes/inventory.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/inventory/'` |
| `src/routes/procurement.route.ts` | `'^/' → '/api/'` | `'^/' → '/api/procurement/'` |

## Gyökérok

Az Express `/bff/cutting` mount-pont lestrippeli a prefixet → router `/sheets`-t kap.
Régi pathRewrite `/api/sheets`-t küldött a cutting service-nek, de az `/api/cutting/sheets`-t vár → 404.
Fix után: `/api/cutting/sheets` → helyes endpoint → service 401-et (auth) vagy 200-at ad.

## Tesztek

- `npm run build` → 0 TS error ✅
- `npm test` → ≥207 zöld ✅

## DoD

- [x] Mindhárom route fájl pathRewrite javítva
- [x] `npm run build` → 0 TS error
- [x] `npm test` → minden teszt zöld
- [x] Commit + push → develop
