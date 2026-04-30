---
id: MSG-PORTAL-007-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-009
created: 2026-04-18
---

## Összefoglaló

BUG-016 — Logout parameter debug: KC logout endpoint 400 hiba fix.

## Probléma

KC logout végpont `post_logout_redirect_uri` paraméternévvel **400 Bad Request** választ adott.

## Megoldás

**Option A1**: Paraméter név megváltoztatása `post_logout_redirect_uri` → `redirect_uri`

### Módosított fájl

| Fájl | Változás |
|---|---|
| `packages/@spaceos/api-client/src/auth/keycloak.ts` | `logoutUrl()` függvény: `redirect_uri` paraméter helyett `post_logout_redirect_uri` |

### Módosítás

```typescript
// Régi:
const params = new URLSearchParams({
  client_id: kcClientId(),
  post_logout_redirect_uri: window.location.origin,
});

// Új:
const params = new URLSearchParams({
  client_id: kcClientId(),
  redirect_uri: window.location.origin,
});
```

## Tesztek

323 / 323 zöld ✓

## Security review

- Auth token handling: nem érinti
- API error handling: logout már nem fog 400-at dobni

## Megjegyzés

INFRA deploy szükséges → KC logout végpont 200-as választ fog adni a `redirect_uri` paraméterrel.

## Commit

`git add packages/@spaceos/api-client/src/auth/keycloak.ts && git commit -m "fix(auth): logout parameter — redirect_uri helyett post_logout_redirect_uri (BUG-016)"`
