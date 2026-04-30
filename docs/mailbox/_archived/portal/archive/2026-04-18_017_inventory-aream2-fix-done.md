---
id: MSG-PORTAL-017-DONE
from: portal
to: root
type: done
priority: high
status: READ
ref: MSG-PORTAL-017
created: 2026-04-18
---

# PORTAL-017 — InventoryPage areaM2 crash + logout hotfix — KÉSZ

## Összefoglaló

Commit: `4e59f49` (branch: `develop`, pushed)

## PORTAL-017: areaM2 crash fix

3 sor `?.` és `?? '0.00'` védelem:
- `stock.areaM2?.toFixed(2) ?? '0.00'`
- `o.areaM2?.toFixed(2) ?? '0.00'`
- `row.areaM2?.toFixed(2) ?? '0.00'`

Új teszt: `renders 0.00 when areaM2 is undefined (API null field)` — zöld.

## Logout hotfix (MANUAL_TEST_GUIDE.md 2026-04-18 06:51 bejegyzés alapján)

**Root cause:** `handleLogout()` `else` ágában `redirectToLogin()` volt hívva ha `idToken` null — a KC aktív sessionnel azonnal visszalogolta a felhasználót → "semmi sem történik" illúzió.

**Fix:**
- `keycloak.ts`: `logoutUrl(idTokenHint?: string)` — opcionális param, csak ha van id_token_hint adja hozzá a query string-hez
- `Topbar.tsx`: mindig `window.location.href = logoutUrl(idToken ?? undefined)` — nincs többé `else redirectToLogin()` ág

Ezután a logout mindig a KC logout endpoint-ra redirectál (session törlés), `id_token_hint`-tel ha elérhető, anélkül ha nem.

## Build + teszt

```
pnpm turbo build → Tasks: 7 successful, 7 total — 0 error
Tests: 307/307 (+1 új: areaM2=undefined eset)
```

## Deploy (INFRA hatáskör)

```bash
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

## Kockázatok / kérdések

Nincsenek.
