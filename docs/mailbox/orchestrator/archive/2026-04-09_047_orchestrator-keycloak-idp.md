---
id: MSG-KC02
from: architect
to: orchestrator
type: task
priority: P0
date: 2026-04-09
sprint: "Keycloak IdP Integration — Orchestrator"
effort: "~2 nap (Nap 5-6 a végrehajtási sorrendben)"
---

# Keycloak IdP Integration — Orchestrator

## Kontextus

Ref: `/opt/spaceos/docs/SpaceOS_Keycloak_IdP_Architecture_v4.md`

Az Orchestrator (Node.js BFF) a static public key betöltés helyett JWKS auto-discovery-re vált, eltávolítja a dev auth route-okat, és egy új `/bff/api/auth/me` endpointot kap.

**Függőség:** Keycloak realm fut (Infra Track A, Nap 1-2), Kernel átállva (Track B, Nap 3-4).

---

## T1 — jwks-rsa csomag + JWKS verify middleware (Nap 5)

### Package: `jwks-rsa` telepítése

```bash
npm install jwks-rsa
```

### Fájl: `src/middleware/jwtVerify.ts` — TELJES CSERE

```typescript
import jwksRsa from 'jwks-rsa';
import jwt, { JwtPayload, JwtHeader, SigningKeyCallback } from 'jsonwebtoken';

const jwksClient = jwksRsa({
  jwksUri: process.env.JWKS_URI!,
  cache: true,
  cacheMaxEntries: 5,
  cacheMaxAge: 600_000,          // 10 perc
  jwksRequestsPerMinute: 10,     // BE-03: burst on key rotation
  rateLimit: true
});

function getKey(header: JwtHeader, callback: SigningKeyCallback): void {
  jwksClient.getSigningKey(header.kid, (err, key) => {
    if (err) return callback(err);
    callback(null, key?.getPublicKey());
  });
}

export async function verifyToken(token: string): Promise<JwtPayload> {
  try {
    return await verifyOnce(token);
  } catch (err: any) {
    // BE-03: key rotation — flush cache and retry once
    if (err.name === 'JsonWebTokenError' && err.message.includes('signature')) {
      jwksClient.getSigningKeys(); // cache flush
      return verifyOnce(token);
    }
    throw err;
  }
}

function verifyOnce(token: string): Promise<JwtPayload> {
  return new Promise((resolve, reject) => {
    jwt.verify(token, getKey, {
      issuer: process.env.JWT_ISSUER,
      audience: process.env.JWT_AUDIENCE,
      algorithms: ['RS256']
    }, (err, decoded) => {
      if (err) return reject(err);
      resolve(decoded as JwtPayload);
    });
  });
}
```

### `.env` frissítés:

```bash
# .env.production
JWKS_URI=https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/certs
JWT_ISSUER=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=kernel-api

# .env.development
JWKS_URI=http://localhost:8080/realms/spaceos/protocol/openid-connect/certs
JWT_ISSUER=http://localhost:8080/realms/spaceos
JWT_AUDIENCE=kernel-api
```

**Megjegyzés:** Orchestrator audience = `kernel-api` (nem külön BFF audience) — D-03 passthrough, ugyanazt a token-t validálja mint a Kernel.

---

## T2 — Régi auth kód eltávolítása (Nap 5)

| Fájl | Akció |
|------|-------|
| `auth.routes.ts` → `POST /bff/api/auth/token` | Endpoint törlése |
| `auth.routes.ts` → `POST /bff/api/auth/refresh` | Endpoint törlése |
| `jwtKeys/` directory + static key loading | Teljes törlés |
| `generateToken()` utility (ha van) | Törlés |

**Ellenőrzés:** `grep -r "auth/token\|auth/refresh\|generateToken\|jwtKeys" src/` → 0 találat

---

## T3 — Új endpoint: `GET /bff/api/auth/me` (Nap 5)

### Fájl: `src/routes/auth.routes.ts` — ÚJ / FRISSÍTVE

```typescript
import { Router } from 'express';
import { authMiddleware } from '../middleware/authMiddleware';

const router = Router();

router.get('/bff/api/auth/me', authMiddleware, (req, res) => {
  const claims = req.user!;

  // BE-01: double deserialization for spaceos_tenants
  let tenants: TenantInfo[] = [];
  try {
    const raw = claims.spaceos_tenants as string;
    if (raw) {
      const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
      tenants = Array.isArray(parsed) ? parsed : JSON.parse(parsed);
    }
  } catch {
    tenants = [];
  }

  res.json({
    sub: claims.sub,
    email: claims.email,
    name: `${claims.given_name ?? ''} ${claims.family_name ?? ''}`.trim(),
    tenants: tenants.map((t: any) => ({
      tenantId: t.tenant_id,
      tenantType: t.tenant_type,
      enabledModules: t.enabled_modules ?? [],
      brandSkin: t.brand_skin ?? 'joinerytech'
    })),
    activeTenantId: tenants[0]?.tenant_id ?? null,
    roles: claims.realm_access?.roles ?? []
  });
});

interface TenantInfo {
  tenant_id: string;
  tenant_type: string;
  enabled_modules: string[];
  brand_skin: string;
}

export default router;
```

### App regisztráció: importáld és mount-old az auth router-t.

---

## T4 — Tesztek (Nap 6)

**≥11 új teszt** (részletek az arch doc Section 9.2-ben):

```
- JwksVerify_ValidToken × 2        (mock JWKS endpoint nock-kal + valid JWT)
- JwksVerify_ExpiredToken × 1      (expired JWT → 401)
- JwksVerify_KeyRotation_Retry × 1 (first verify fail → cache flush → retry success, BE-03)
- AuthMe_Endpoint × 3              (spaceos_tenants parse, double-deser, tenant mapping)
- Auth_Routes_Removed × 2          (POST /bff/api/auth/token → 404)
- BearerPassthrough × 2            (token forwarded to Kernel unchanged, D-03)
```

---

## DoD gates

```bash
cd /opt/spaceos/spaceos-orchestrator
npm run build 2>&1 | tail -3      # 0 TypeScript error
npm test 2>&1 | tail -5           # ≥153 meglévő + ≥11 új = ≥164 pass, 0 fail
grep -r "auth/token\|auth/refresh\|generateToken\|jwtKeys" src/  # 0 találat
```

---

## Output

Ha kész: `mailbox/outbox/2026-04-09_047_orchestrator-keycloak-idp-done.md`

Tartalom:
- Teszt összesítő (meglévő + új)
- Törölt fájlok listája
- Esetleges eltérések az arch doc-tól
