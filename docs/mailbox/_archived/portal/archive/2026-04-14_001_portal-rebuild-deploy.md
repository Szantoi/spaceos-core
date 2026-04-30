---
id: MSG-PORTAL-001
from: root
to: portal
type: task
priority: critical
status: READ
ref: MSG-INFRA-078-BLOCKED
created: 2026-04-14
---

# MSG-PORTAL-001 — Design Portal rebuild + VPS deploy (stale dist fix)

## Háttér

**Doorstar Soft Launch blokkere.** A VPS-en lévő portal `dist/` **2026-04-07 07:22** dátumú (elavult build). A deployed SPA még a régi architektúra szerint `POST /bff/auth/token`-t hív az authorization_code exchange-hez — ez az endpoint **szándékosan törölve lett**. A user login jelenleg broken: Keycloak callback sikeres, de a SPA 404-et kap.

A source fájlok (`packages/@spaceos/api-client/src/auth/keycloak.ts`, `authStore.ts`, `createClient.ts`) már tartalmazzák a direct Keycloak PKCE flow-t. Csak a build és deploy hiányzik.

## Feladat

### 1. Build

```bash
cd /opt/spaceos/design-portal
pnpm install    # ha szükséges
pnpm build      # vagy: pnpm run build:joinerytech — ellenőrizd a package.json-t
```

### 2. Smoke check build output

```bash
# Verify: nincs bff/auth/token hívás az új buildben
grep -r "bff/auth/token" dist/assets/*.js && echo "FAIL: old endpoint found" || echo "OK: no bff/auth/token"

# Verify: van Keycloak PKCE callback logika
grep -r "code_verifier\|handleCallback" dist/assets/*.js | head -5
```

### 3. VPS-re másolás

A `dist/` tartalmát a VPS-re kell juttatni: `/opt/spaceos/design-portal/dist/`

Lehetséges út: rsync / scp / git (ha a dist nincs gitignore-olva) — válaszd a projektorban bevett módszert.

### 4. Nginx reload (ha szükséges)

```bash
sudo nginx -t && sudo systemctl reload nginx
```

### 5. Smoke test

```bash
curl -I https://joinerytech.hu/       # 200
curl -I https://asztalostech.hu/      # 200
```

### 6. Manuális E2E login teszt (opcionális, de kért)

Böngészőben: `https://joinerytech.hu/` → `test-admin` / `SpaceOS-Test-2026!` → dashboard betölt hibamentesen.

## DoD

- [ ] `dist/` rebuild (2026-04-14 build time)
- [ ] `grep bff/auth/token dist/**` → üres (nincs régi endpoint)
- [ ] VPS-en friss dist élő
- [ ] `curl https://joinerytech.hu/` → 200
- [ ] DONE outbox: build timestamp + smoke check eredmény + login teszt eredmény
