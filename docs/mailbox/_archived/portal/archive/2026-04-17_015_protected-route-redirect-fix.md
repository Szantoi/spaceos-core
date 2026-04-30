---
id: MSG-PORTAL-015
from: root
to: portal
type: task
priority: critical
status: READ
ref: DEBUG-001
created: 2026-04-17
---

# PORTAL-015 — ProtectedRoute: redirectToLogin useEffect fix

## Root ok

`src/features/auth/ProtectedRoute.tsx` — `redirectToLogin()` a render body-ban van hívva,
nem `useEffect`-ben. Minden render-nél új PKCE `state` generálódik és felülírja a
`sessionStorage`-ban a régit → Keycloak callback `state_mismatch` → végtelen loop.

## Feladat

**Fájl:** `apps/joinerytech/src/features/auth/ProtectedRoute.tsx`

```tsx
// ELŐTTE (hibás):
export function ProtectedRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  if (!isAuthenticated) {
    void redirectToLogin();
    return <div ...>Átirányítás…</div>;
  }
  return <Outlet />;
}

// UTÁNA (helyes):
import { useEffect } from 'react';

export function ProtectedRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  useEffect(() => {
    if (!isAuthenticated) {
      void redirectToLogin();
    }
  }, [isAuthenticated]);

  if (!isAuthenticated) {
    return <div className="flex items-center justify-center h-screen">
      <p className="text-sm text-gray-400">Átirányítás…</p>
    </div>;
  }
  return <Outlet />;
}
```

## Ellenőrizd

- `ProtectedRoute.test.tsx` — frissítsd ha szükséges (a `redirectToLogin` mock timing változhat)
- `pnpm turbo build` — 0 error
- `Tests: 306/306`

## Deploy

```bash
pnpm turbo build
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

## DONE feltételek

- [ ] `ProtectedRoute.tsx` javítva (`useEffect`-be helyezve)
- [ ] `pnpm turbo build` 0 error
- [ ] `306/306` teszt zöld
- [ ] Deploy kész
- [ ] Böngészőben: bejelentkezés sikeres, nincs redirect loop, nincs `state_mismatch`
- [ ] Commit + push
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
