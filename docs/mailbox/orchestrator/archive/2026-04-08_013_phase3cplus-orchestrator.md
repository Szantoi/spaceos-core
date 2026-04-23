---
id: MSG-O013
from: architect
to: orchestrator
type: task
priority: P1
date: 2026-04-08
sprint: "Sprint D · Phase 3C+"
---

# Phase 3C+ — Orchestrator Tasks: dist rebuild + handshakes proxy

## SÜRGŐS: MSG-O012 — dist rebuild

```bash
cd /opt/spaceos/spaceos.orchestrator && npm run build
grep -c "snapshots\|verify-chain\|proof" dist/index.js
```

Elvárt: minden route benne van a dist-ben.

## T1 — `/bff/handshakes` proxy route

Új fájl: `src/routes/handshakes.route.ts`
- Minta: `src/routes/snapshot.route.ts` (isValidUuid, kernelHeaders, handleKernelError)
- Routes: POST/GET /bff/handshakes, GET /bff/handshakes/:id, PUT /bff/handshakes/:id/accept, PUT /bff/handshakes/:id/reject
- Minden route: `requireAuth`
- POST: req.body pass-through

Regisztráció `src/index.ts`-ben:
```typescript
import { handshakesRouter } from './routes/handshakes.route';
app.use('/bff/handshakes', proxyLimiter, handshakesRouter);
```

4 teszt: `src/routes/handshakes.route.test.ts` (no-auth → 401, invalid UUID → 400)

## T2 — Auth response bővítés (BLOKKOLVA — Kernel 0025/0026 után)

A `POST /bff/auth/token` és `POST /bff/auth/refresh` response-t bővíteni kell:
```typescript
// Jelenleg: { accessToken, brandSkin, expiresAt }
// Cél:      { accessToken, brandSkin, expiresAt, enabledModules, allowedHosts }
```

`enabledModules` és `allowedHosts` a Kernel JWT `enabled_modules` és `allowed_hosts` claim-jéből jön.
`jwt.decode()` mintára (ahogy brandSkin-t most olvassuk).

**BLOKKOLÓ:** Kernel Migration 0025+0026 kész kell legyen + token claims bővítve.

## DoD

```bash
npm run build  # 0 error
npm test       # ≥157 pass (153 + 4 új handshake teszt)
```

## Válaszban kérem

Mailbox outbox: `docs/mailbox/orchestrator/outbox/2026-04-08_013_dist-rebuild-handshakes-done.md`
