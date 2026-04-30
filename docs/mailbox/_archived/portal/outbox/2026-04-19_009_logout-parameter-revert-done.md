---
id: MSG-PORTAL-011-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-011
created: 2026-04-19
---

## Összefoglaló

CRITICAL regresszió fix: logout parameter revert.

## Probléma

Commit `fd340bf` (logout parameter debug) **reverziót okozott**: `post_logout_redirect_uri` → `redirect_uri` hibás paraméter név → KC 400 Bad Request

## Megoldás

1 soros revert `packages/@spaceos/api-client/src/auth/keycloak.ts`:

```typescript
// Hibás (fd340bf):
redirect_uri: window.location.origin,

// Javított:
post_logout_redirect_uri: window.location.origin,
```

## Tesztek

323 / 323 zöld ✓ (joinerytech)

## Commit

`git add packages/@spaceos/api-client/src/auth/keycloak.ts && git commit -m "fix(auth): revert logout parameter — post_logout_redirect_uri correct (revert fd340bf regression)"`

Build: `npm run build --filter=joinerytech` — `dist/` ready for deploy
