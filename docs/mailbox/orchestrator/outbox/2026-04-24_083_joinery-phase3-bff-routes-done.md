---
id: MSG-ORCH-083-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-083
created: 2026-04-24
---

# ORCH-083 — Joinery Phase 3 BFF routes DONE

## Összefoglaló

4 új BFF proxy route a `joinery.route.ts`-ben, a meglévő axios minta alapján:

| Route | Target | Megjegyzés |
|---|---|---|
| `POST /bff/joinery/gyartasilap/batch` | `POST /api/gyartasilap/batch` | JWT forwarding |
| `GET /bff/joinery/gyartasilap/batch/:id` | `GET /api/gyartasilap/batch/:id` | UUID validáció |
| `GET /bff/joinery/gyartasilap/batch/:id/download` | `GET /api/gyartasilap/batch/:id/download` | 302 redirect transparent (`maxRedirects: 0`) |
| `POST /bff/joinery/anyaglista` | `POST /api/anyaglista` | PDF binary pass-through (`responseType: arraybuffer`) |

### Implementáció részletek

- **JWT forwarding**: `joineryHeaders(req)` — meglévő helper, Authorization header forward
- **302 redirect**: `validateStatus: (s) => s < 400` + `maxRedirects: 0` → Location header transparent forwarding a kliensnek
- **PDF response**: `responseType: 'arraybuffer'` + Content-Type/Content-Disposition header forwarding
- **UUID validáció**: batch/:id route-okon `isValidUuid()` → 400 ha nem UUID

## Tesztek

- `npm run build` → 0 TS error ✅
- `npm test` → **224/224 zöld** (219 előző + 5 új) ✅

Új tesztek:
1. `POST /bff/joinery/gyartasilap/batch` → proxy forward + JWT
2. `GET /bff/joinery/gyartasilap/batch/:id` → proxy forward (valid UUID)
3. `GET /bff/joinery/gyartasilap/batch/:id` → 400 (invalid UUID)
4. `GET /bff/joinery/gyartasilap/batch/:id/download` → 302 redirect transparent + Location header
5. `POST /bff/joinery/anyaglista` → PDF binary + Content-Type/Disposition forwarding

## Security review

- [x] JWT forwarding — `requireAuth` middleware minden route-on
- [x] UUID validáció — batch/:id route-okon
- [x] Input validation — Joinery API végzi (ManufacturerOnly policy)
- [x] Timeout — 10s minden route-on
- [x] Error handling — `handleJoineryError` (upstream status forward)
- [x] Nincs secret a logban

## Commit

`0857440` — pushed to `origin/develop`

## Kockázatok / kérdések

Nincsenek.

## DoD

- [x] 4 BFF route regisztrálva
- [x] JWT forward + ManufacturerOnly auth delegálva
- [x] Batch download 302 redirect transparent
- [x] `npm run build` 0 error
- [x] `npm test` ≥ 223 pass (224 actual)
- [x] Commit + push
