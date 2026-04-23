---
id: MSG-ORCH-071
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-E2E-032-DONE
created: 2026-04-15
---

# MSG-ORCH-071 — Cutting BFF proxy catch-all fix (POST/PUT/DELETE)

## Probléma

Az ORCH-070-ben létrehozott cutting proxy route-ok csak GET kéréseket proxyznak — POST/PUT/DELETE 404-et ad. Az E2E finding azonosította.

A meglévő `abstractions.route.ts` catch-all mintát kell követni, amely minden HTTP metódust kezel.

## Feladat

Javítsd a 3 cutting proxy route-ot catch-all-ra:

```typescript
// inventory.route.ts, cutting.route.ts, procurement.route.ts
// Minden metódus: GET, POST, PUT, DELETE, PATCH
router.all('/*', proxyLimiter, requireAuth, async (req, res) => {
  // proxy logic — ugyanaz mint abstractions.route.ts
})
```

Nézd meg az `abstractions.route.ts`-t mintaként — pontosan ugyanezt a mintát kell követni.

## Build + test + deploy

```bash
npm run build   → 0 TS error
npm test        → ≥207 pass
pm2 restart spaceos-orchestrator
```

## Ellenőrzés

```bash
curl -s -o /dev/null -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d '{"test":1}' \
  http://127.0.0.1:3000/bff/inventory/movements/inbound
# Elvárás: 401 (nem 404) — token nélkül
```

## DoD

- [ ] `npm run build` → 0 error
- [ ] `npm test` → ≥207 pass
- [ ] POST /bff/inventory/* → 401 (nem 404)
- [ ] POST /bff/cutting/* → 401 (nem 404)
- [ ] POST /bff/procurement/* → 401 (nem 404)
- [ ] pm2 restart + ellenőrzés
- [ ] Outbox: `MSG-ORCH-071-DONE`
