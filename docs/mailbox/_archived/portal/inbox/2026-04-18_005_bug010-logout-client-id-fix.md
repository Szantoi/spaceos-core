---
id: MSG-PORTAL-005
from: root
to: portal
type: task
priority: high
status: READ
created: 2026-04-18
---

# BUG-010 — Logout nem működik: `client_id` hiányzik a logout URL-ből

## Gyökérok (INFRA vizsgálat alapján)

Az nginx `/auth/` proxy és a KC `post.logout.redirect.uris` konfiguráció helyes. A hiba a Portal kódban van:

A `logoutUrl()` függvény NEM adja hozzá a `client_id` paramétert a logout URL-hez. Ha `id_token_hint` is hiányzik (pl. `idToken` null a store-ban), Keycloak **nem tudja validálni** a `post_logout_redirect_uri`-t → nem irányít vissza → felhasználó stuck.

## Fájl

`packages/@spaceos/api-client/src/auth/keycloak.ts`

## Jelenlegi kód

```ts
export function logoutUrl(idTokenHint?: string): string {
  const params = new URLSearchParams({
    post_logout_redirect_uri: window.location.origin,
  });
  if (idTokenHint) params.set('id_token_hint', idTokenHint);
  return `${kcRealmUrl()}/protocol/openid-connect/logout?${params}`;
}
```

## Fix

```ts
export function logoutUrl(idTokenHint?: string): string {
  const params = new URLSearchParams({
    client_id: kcClientId(),
    post_logout_redirect_uri: window.location.origin,
  });
  if (idTokenHint) params.set('id_token_hint', idTokenHint);
  return `${kcRealmUrl()}/protocol/openid-connect/logout?${params}`;
}
```

`kcClientId()` már definiálva van a fájlban (`VITE_KC_CLIENT_ID` → `"portal-app"`).

KC logout URL teljes példa:
```
https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/logout
  ?client_id=portal-app
  &post_logout_redirect_uri=https://joinerytech.hu
  &id_token_hint=<token_ha_van>
```

## DoD

- [ ] `logoutUrl()` tartalmazza `client_id`-t
- [ ] `npm test` → összes zöld (legalább 318)
- [ ] INFRA deploy szükséges → jelezd
- [ ] (Nem kell E2E tesztelni — INFRA manuálisan verifikálja)

---

*Skill: `/spaceos-terminal`*
