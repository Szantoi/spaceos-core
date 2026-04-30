---
id: MSG-PORTAL-009
from: root
to: portal
type: task
priority: critical
status: READ
ref: MSG-INFRA-020-BLOCKED
created: 2026-04-18
---

# BUG-016 — Logout parameter debug: post_logout_redirect_uri paraméter

## Helyzet (INFRA nyomozás)

KC `postLogoutRedirectUris` config él (`https://joinerytech.hu##...`), de a Portal `logoutUrl()` által küldött URL explicit paraméterrel **400 Bad Request**:

```
GET /auth/realms/spaceos/protocol/openid-connect/logout
  ?client_id=portal-app
  &post_logout_redirect_uri=https://joinerytech.hu/
→ 400 ❌
```

Paraméter nélkül:
```
GET /auth/realms/spaceos/protocol/openid-connect/logout?client_id=portal-app
→ 200 ✅
```

## Valószínű root cause

A KC verzió (24.0) vagy a realm config nem támogatja a `post_logout_redirect_uri` **paraméter nevet** az OpenID Connect logout végpontnál. Az OIDC spec szerint ez az expectedName, de Keycloak régebbi verzióiban vagy szokás szerint `redirect_uri` vagy `ReturnTo` lehet.

## Teendő — 2 opcióból választ

### Option A: logoutUrl() paraméter név fix

Módosítsd `packages/@spaceos/api-client/src/auth/keycloak.ts` `logoutUrl()` függvényt:

```ts
// Jelenlegi (400):
const params = new URLSearchParams({
  client_id: kcClientId(),
  post_logout_redirect_uri: window.location.origin,
});

// Próbáld ezeket sorban (egyik működni fog):
// Option A1: redirect_uri helyett
const params = new URLSearchParams({
  client_id: kcClientId(),
  redirect_uri: window.location.origin, // KC-specifikus
});

// Option A2: ReturnTo helyett
const params = new URLSearchParams({
  client_id: kcClientId(),
  ReturnTo: window.location.origin,
});
```

Tesztelj kurllal:
```bash
curl -s -o /dev/null -w "%{http_code}" "http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/logout?client_id=portal-app&redirect_uri=https://joinerytech.hu/"
```

### Option B: KC realm defaultPostLogoutRedirectUri (INFRA feladata)

INFRA-ben állítsd be a KC realm config-ban a default logout redirect URI-t:

```bash
# KC admin API-n vagy UI-n
defaultPostLogoutRedirectUri = https://joinerytech.hu/
```

Ezután a Portal `logoutUrl()` nem küld paraméter — KC automatikusan erre az URI-ra irányít.

## DoD

- [ ] Logout gomb → Keycloak logout oldal → https://joinerytech.hu/
- [ ] Nincs 400 hiba
- [ ] `npm test` → 323+ zöld
- [ ] INFRA deploy szükséges (ha Option B) → jelezd DONE-ban

---

*Skill: `/spaceos-terminal`*
