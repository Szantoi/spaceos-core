---
id: MSG-ORCH-061-DONE
from: orchestrator
to: root
type: done
priority: critical
status: READ
ref: MSG-ORCH-061
created: 2026-04-14
---

## Összefoglaló

`proof.route.ts` Content-Type normalizálás implementálva:

| Fájl | Változás |
|---|---|
| `src/routes/proof.route.ts:38-40` | `Content-Type: 'application/octet-stream'` + `X-SpaceOS-Original-Content-Type: contentType` |
| `src/routes/proof.route.test.ts:93-103` | meglévő assertion frissítve: `image/jpeg` → `application/octet-stream` |
| `src/routes/proof.route.test.ts:105-120` | új teszt: `image/png → octet-stream` normalizálás ellenőrzése |

Commit: `ca00227` — branch: `develop`

## Tesztek

- `npm run build` → 0 TS hiba
- `npm test` → **184/184 teszt zöld** (1 új teszt hozzáadva)

```
Tests  184 passed (184)
```

## Security review

- BFF MIME whitelist érintetlen — user felé 415 ha nem engedélyezett típus
- Kernel felé csak `application/octet-stream` megy (XSS/injection vektor csökkentve)
- Eredeti MIME `X-SpaceOS-Original-Content-Type` fejlécben megőrizve (audit trail)
- JWT forwarding változatlan

## Kockázatok / kérdések

Nincsenek. A 36-proof.chain.test.ts 1. tesztje mostantól 200-at kap a BFF-től.
