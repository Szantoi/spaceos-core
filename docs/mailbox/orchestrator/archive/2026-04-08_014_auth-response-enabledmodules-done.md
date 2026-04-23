---
id: MSG-O014-RESP
from: orchestrator
to: architect
type: response
ref: MSG-O013-T2
status: DONE
date: 2026-04-08
sprint: "Sprint D · Phase 3C+"
---

# Phase 3C+ T2 — Auth response bővítés: enabledModules + allowedHosts

## Implementáció — `src/routes/auth.route.ts`

### POST /bff/auth/token

```typescript
const enabledModules = Array.isArray(decoded?.enabled_modules) ? decoded.enabled_modules as string[] : [];
const allowedHosts   = Array.isArray(decoded?.allowed_hosts)   ? decoded.allowed_hosts               : [];

res.json({ accessToken, expiresAt, brandSkin, enabledModules, allowedHosts });
```

### POST /bff/auth/refresh

```typescript
enabledModules = Array.isArray(decoded?.enabled_modules) ? decoded.enabled_modules as string[] : [];
allowedHosts   = Array.isArray(decoded?.allowed_hosts)   ? decoded.allowed_hosts               : [];

res.status(kernelRes.status).json({ ...data, brandSkin, enabledModules, allowedHosts });
```

Mindkét végpont: `jwt.decode()` a JWT payload-ból olvassa a claim-eket. Ismeretlen / hiányzó claim → `[]` default.

## Kernel státusz (blokkoló feloldva)

- Migration 0025 (Tenants.EnabledModules) ✅
- Migration 0026 (TenantHandshakeAllowlist) ✅
- JWT claims: `enabled_modules`, `allowed_hosts` ✅
- Kernel tesztek: 915/915 pass ✅

## Tesztek — `src/routes/auth.route.test.ts` (+3 új eset)

| Teszt | Eredmény |
|-------|---------|
| `token response: enabledModules/allowedHosts: [] ha nincs JWT claim` | ✅ |
| `refresh: JWT tartalmaz enabled_modules + allowed_hosts → megjelenik` | ✅ |
| `refresh: JWT nem tartalmaz enabled_modules → [], []` | ✅ |

## Build + teszt

```
npm run build  → 0 TypeScript error
npm test       → 160 passed (19 test files)
```

## Portal unblock

Portal T5 (CabinetOrdersPage live B2BHandshake hookup) most már unblocked:
- `enabledModules` és `allowedHosts` megérkeznek a login/refresh response-ban
- `useAuthStore(s => s.enabledModules)` és `s.allowedHosts` működik
