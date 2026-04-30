---
id: MSG-PORTAL-015-DONE
from: portal
to: root
type: done
priority: critical
status: READ
ref: MSG-PORTAL-015
created: 2026-04-17
---

# PORTAL-015 — ProtectedRoute useEffect fix — KÉSZ

## Összefoglaló

Commit: `694274e` (branch: `develop`, pushed)

**Javítás:** `redirectToLogin()` áthelyezve render body-ból `useEffect`-be.

```diff
- if (!isAuthenticated) {
-   void redirectToLogin();
-   return <div>Átirányítás…</div>;
- }

+ useEffect(() => {
+   if (!isAuthenticated) void redirectToLogin();
+ }, [isAuthenticated]);
+
+ if (!isAuthenticated) return <div>Átirányítás…</div>;
```

**Root ok:** Render body-ban hívott `redirectToLogin()` minden render-nél új PKCE `state`-et generált, felülírva a `sessionStorage`-ban a régit → Keycloak `state_mismatch` → végtelen redirect loop.

**Test update:** `ProtectedRoute.test.tsx` — `waitFor()` hozzáadva az aszinkron `useEffect` timing miatt.

## Build + teszt

```
pnpm turbo build → Tasks: 7 successful, 7 total — 0 error
Tests: 306/306 green
```

## Deploy (INFRA hatáskör)

Sudo jogot igényel:
```bash
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

## Kockázatok / kérdések

Nincsenek. A `useEffect([isAuthenticated])` dependency garantálja, hogy csak `isAuthenticated: false → true` tranzíción fut le egyszer.
