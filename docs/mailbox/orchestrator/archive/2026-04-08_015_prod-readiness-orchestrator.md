---
id: MSG-O015
from: architect
to: orchestrator
type: task
priority: P0
date: 2026-04-08
sprint: "Sprint D · Production Readiness"
---

# Production Readiness — Orchestrator: Track A.5 (JWKS URI + AUTH_PROVIDER)

## Kontextus

Phase 3C+ DoD ✅ teljes. Az Orchestrator felelőssége:
- `jwks-rsa` csomag → JWKS URI alapú JWT verify (Keycloak production)
- `AUTH_PROVIDER=keycloak|dev` env var → backward compatible

---

## T1 — `jwks-rsa` csomag telepítése

```bash
npm install jwks-rsa
```

---

## T2 — `AUTH_PROVIDER` env var + middleware

Fájl: `src/config/env.ts`

```typescript
export const env = {
  // ... meglévők ...
  AUTH_PROVIDER:  process.env['AUTH_PROVIDER']  ?? 'dev',      // 'keycloak' | 'dev'
  JWKS_URI:       process.env['JWKS_URI']        ?? '',         // pl. https://auth.joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/certs
  JWT_AUTHORITY:  process.env['JWT_AUTHORITY']   ?? '',
  JWT_AUDIENCE:   process.env['JWT_AUDIENCE']    ?? 'spaceos-kernel-api',
};
```

---

## T3 — JWT middleware bővítés

Fájl: `src/middleware/auth.middleware.ts`

```typescript
import jwksRsa from 'jwks-rsa';
import jwt from 'jsonwebtoken';
import { env } from '../config/env';

// Keycloak mode: JWKS URI alapú verify
const jwksClient = env.AUTH_PROVIDER === 'keycloak' && env.JWKS_URI
  ? jwksRsa({ jwksUri: env.JWKS_URI, cache: true, cacheMaxAge: 600_000 })
  : null;

function getKey(header: jwt.JwtHeader, callback: jwt.SigningKeyCallback) {
  if (!jwksClient) { callback(new Error('JWKS client not configured')); return; }
  jwksClient.getSigningKey(header.kid, (err, key) => {
    callback(err, key?.getPublicKey());
  });
}

export function requireAuth(req, res, next) {
  const authHeader = req.headers.authorization ?? '';
  const token = authHeader.startsWith('Bearer ') ? authHeader.slice(7) : null;
  if (!token) { res.status(401).json({ error: 'Unauthorized' }); return; }

  if (env.AUTH_PROVIDER === 'keycloak') {
    jwt.verify(token, getKey, { algorithms: ['RS256'], audience: env.JWT_AUDIENCE }, (err, decoded) => {
      if (err) { res.status(401).json({ error: 'Unauthorized' }); return; }
      req.user = decoded;
      next();
    });
  } else {
    // dev mode: meglévő ES256 verify (nem változik)
    // ... meglévő logika marad ...
    next(); // egyszerűsített placeholder — meglévő kóddal merge
  }
}
```

**FONTOS:** `AUTH_PROVIDER=dev` esetén a meglévő ES256 / hardcoded secret verify marad — nem törik el a dev/test workflow.

---

## T4 — Tesztek (+4 új)

Fájl: `src/middleware/auth.middleware.test.ts`

- `AUTH_PROVIDER=dev: valid ES256 token → 200`
- `AUTH_PROVIDER=dev: invalid token → 401`
- `AUTH_PROVIDER=keycloak: JWKS_URI not set → 401 gracefully`
- `AUTH_PROVIDER=keycloak: expired token → 401`

---

## DoD gate-ek (Orchestrator)

```bash
npm run build  # 0 error
npm test       # ≥164 pass (160 + ≥4 új), 0 fail
```

Checklist:
- [ ] `AUTH_PROVIDER=dev` → meglévő ES256 verify — semmi sem törik
- [ ] `AUTH_PROVIDER=keycloak` + `JWKS_URI` → JWKS alapú RS256 verify
- [ ] `JWKS_URI` nincs beállítva + `keycloak` mode → 401 graceful (nem crash)
- [ ] env.ts bővítve: `AUTH_PROVIDER`, `JWKS_URI`, `JWT_AUTHORITY`, `JWT_AUDIENCE`

## Válaszban kérem

Mailbox outbox: `docs/mailbox/orchestrator/outbox/2026-04-08_015_prod-readiness-orchestrator-done.md`
