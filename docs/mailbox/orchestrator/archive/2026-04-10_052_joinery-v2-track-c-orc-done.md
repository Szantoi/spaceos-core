---
id: MSG-ORCHESTRATOR-052-DONE
from: orchestrator
to: root
type: response
status: DONE
ref: MSG-ORCHESTRATOR-052
created: 2026-04-10
---

# Track C-Orc — DONE: /internal/* guard + Joinery proxy route

## Összefoglaló

A `X-SpaceOS-Internal` header guard (SEC-01) és a Joinery kalkuláció-eredmény proxy route implementálva.

### Változott fájlok

| Fájl | Változás |
|---|---|
| `src/middleware/internal.middleware.ts` | ÚJ — SEC-01 guard: `requireInternalHeader` |
| `src/routes/internal.route.ts` | ÚJ — `PUT /bff/internal/joinery/results` proxy |
| `src/routes/internal.route.test.ts` | ÚJ — 6 teszt (guard + proxy) |
| `src/index.ts` | Import + mount: `/bff/internal` proxyLimiter + guard |

### Routing architektúra

```
PUT /bff/internal/joinery/results
  ↓ proxyLimiter
  ↓ requireInternalHeader (SEC-01) — 403 ha hiányzik X-SpaceOS-Internal: true
  ↓ internalRouter
  → axios.put JOINERY_BASE_URL/internal/results  (X-SpaceOS-Internal: true forwarded)
```

**Fontos:** Nincs `requireAuth` — service-to-service kommunikáció, a guard adja az autentikációt.

## Tesztek

- **169 passed / 0 failed** (163 baseline + 6 új)
- Új tesztek: `src/routes/internal.route.test.ts`
  1. Hiányzó header → 403
  2. Hibás header érték ("false") → 403
  3. 403 audit log → `console.warn` `[SEC-01]` prefix-szel
  4. Helyes header → guard engedi, axios hívás megtörténik
  5. Proxy helyes body + header forward (Joinery URL, X-SpaceOS-Internal)
  6. Joinery API hiba → upstream státusz forward (422)

## Security review

- **SEC-01 guard**: `x-spaceos-internal !== 'true'` → 403 + `console.warn` audit log
- **Guard nem vonatkozik**: `/bff/api/*`, `/bff/joinery/*` — csak `/bff/internal/*`
- **Rate limiting**: `proxyLimiter` alkalmazva
- **Zod validáció**: nem szükséges — a Joinery API validál; body pass-through
- **Token nem kerül logba**: csak method + path logolódik a warn-ban
- **OWASP**: nincs command injection, nincs path traversal (fix endpoint)

## Kockázatok / kérdések

Nincs blokkoló kérdés. A SEC-01 guard a `X-SpaceOS-Internal` headerre épít — ez belső hálózati szegregáción alapul (Joinery csak 127.0.0.1-ről érhető el). Ha publikus hálózatból is elérhető lesz a `/bff/internal` prefix, akkor erősebb autentikáció (pl. shared secret) szükséges.
