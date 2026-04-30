---
id: MSG-PORTAL-007
from: root
to: portal
type: task
priority: high
status: READ
ref: MSG-TESTER-017
created: 2026-04-18
---

# BUG-015 — Browser Back button: auth state elvész → Keycloak login

## Szimptóma (TESTER-017)

**Lépések:** 
1. Bejelentkezés megtörtént, Dashboard-on vagy / oldal
2. Bármelyik link (pl. Suppliers, Audit, Chat)
3. Browser Back gomb
4. **Kapott:** Keycloak login oldal jelenik meg (nem bejelentkezve tűnik)
5. **Elvárt:** Dashboard/home old megjelenik bejelentkezve

## Root cause

A `CallbackPage` (OIDC callback után) beállítja az auth tokent (localStorage), de a browser history-ban a previous state nem tartalmazza az auth context-et. Back button után a React app újraindít, de az auth state nem inicializálódik helyes módon.

## Fix

**1. useAuth hook — init localStorage-ből:**

A `packages/@spaceos/api-client/src/auth/useAuth.ts` hook legyen singleton, és init közt ellenőrizze:

```typescript
const [isAuthenticated, setIsAuthenticated] = useState(() => {
  // Init: localStorage-ből restore
  const token = localStorage.getItem('auth_token');
  return !!token && !isTokenExpired(token);
});
```

**2. App.tsx / Router — private route guard:**

```typescript
if (isLoading) return <LoadingPage />;
if (!isAuthenticated) return <Redirect to="/login" />;
// else: render private routes
```

**3. useEffect — browser popstate:**

```typescript
useEffect(() => {
  const handlePopState = () => {
    // Restore auth state localStorage-ből
    const token = localStorage.getItem('auth_token');
    if (token) {
      setIsAuthenticated(true);
    }
  };
  window.addEventListener('popstate', handlePopState);
  return () => window.removeEventListener('popstate', handlePopState);
}, []);
```

## DoD

- [ ] Back button után: auth session megmarad (localStorage-ből restore)
- [ ] Logout után: localStorage token törlődik
- [ ] `npm test` → 318 zöld (nem tesztelünk popstate-et, de manual OK)
- [ ] INFRA deploy szükséges → jelezd DONE-ban

---

*Skill: `/spaceos-terminal`*
