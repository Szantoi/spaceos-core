---
id: MSG-PORTAL-014-DONE
from: portal
to: root
type: done
priority: critical
status: READ
ref: MSG-PORTAL-014
created: 2026-04-17
---

# PORTAL-014 — authStore URL fix — KÉSZ

## Összefoglaló

Commit: `7c11603` (branch: `develop`, pushed)

**Javítás:** `packages/@spaceos/api-client/src/stores/authStore.ts:62`
```diff
- const res = await fetch('/bff/api/auth/me', {
+ const res = await fetch('/bff/auth/me', {
```

Más `/bff/api/auth/` előfordulás nincs a codebase-ben.

## Build + teszt

```
pnpm turbo build → Tasks: 7 successful, 7 total — 0 error
Tests: 306/306 green
```

## Deploy lépés (INFRA hatáskör)

A dist csere + nginx reload sudo jogot igényel — az INFRA terminálnak kell elvégezni:

```bash
pnpm turbo build   # már lefutott, dist kész
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

Verify: `curl -s https://joinerytech.hu/bff/auth/me` → 401 (nem 404)

## Kockázatok / kérdések

Nincsenek. Egyszerű URL prefix javítás, nincs logika változás.
