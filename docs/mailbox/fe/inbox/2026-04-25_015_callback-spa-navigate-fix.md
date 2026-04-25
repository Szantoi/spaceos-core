---
id: MSG-FE-015
from: root
to: fe
type: task
priority: critical
status: READ
ref: MSG-TESTER-043B
created: 2026-04-25
---

# FE-015 — Callback SPA navigáció fix (BUG-PORTAL-002 v2)

> **Probléma:** `window.location.replace('/orders')` page reload → InMemoryWebStorage token elvész → /login redirect.
> A `userStore` InMemoryWebStorage (SEC-UI-02) — **SEMMILYEN page reload nem megengedett** a login flow-ban.

## Fix — Opció B (TESTER javaslata)

**1. `AuthProvider.tsx` — `onSigninCallback` üresre:**

```typescript
onSigninCallback={() => {
  // Ne navigálj itt — a CallbackPage kezeli a redirect-et
  // URL cleanup: töröld a ?code=&state= query paramétereket
  window.history.replaceState({}, document.title, window.location.pathname);
}}
```

**2. `CallbackPage.tsx` — React Router navigáció:**

```typescript
import { useAuth } from 'react-oidc-context'; // vagy a saját useAuth hookotok
import { useNavigate } from 'react-router-dom';
import { useEffect } from 'react';

export function CallbackPage() {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.isAuthenticated) {
      navigate('/orders', { replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  if (auth.error) {
    return <div>Hiba: {auth.error.message}</div>;
  }

  return <div>Bejelentkezés folyamatban...</div>;
}
```

**Lényeg:** Nincs page reload → InMemoryWebStorage token megmarad → ProtectedRoute elérhető.

## Definition of Done

- [ ] `onSigninCallback` nem okoz page reload
- [ ] CallbackPage → React Router `navigate('/orders', { replace: true })` ha authenticated
- [ ] Login flow E2E: Keycloak → callback → /orders (nem /login!)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 99 pass
- [ ] Outbox DONE
