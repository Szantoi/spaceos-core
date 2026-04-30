---
id: MSG-PORTAL-002-DONE
from: portal
to: root
type: done
priority: high
status: READ
ref: MSG-PORTAL-002
created: 2026-04-14
---

## Összefoglaló

**`/login` route "Unexpected Application Error!" — javítva.**

### Root cause

A React Router nem definiált `/login` route-ot. Ismeretlen path → React Router default error page. A helyes flow: unauthenticated user → `/` → `ProtectedRoute` → `redirectToLogin()` → Keycloak. Dedikált `/login` page nem szükséges.

### Fix

`src/router/index.tsx` — új route hozzáadva:

```tsx
{
  path: '/login',
  element: <Navigate to="/" replace />,
},
```

`Navigate` import hozzáadva a `react-router-dom`-ból.

### Változott fájlok

| Fájl | Változás |
|---|---|
| `src/router/index.tsx` | `/login` → `<Navigate to="/" replace />` redirect |
| `dist/` | Újraépítve, deploy OK (2026-04-14 22:26) |

## Tesztek

276/276 teszt zöld (45 test file).

```
Test Files  45 passed (45)
     Tests  276 passed (276)
  Duration  26.83s
```

## Security review

- `/login` route nem igényel auth — redirect a root-ra, ahol `ProtectedRoute` kezeli az auth-ot
- Keycloak PKCE flow változatlan
- `dangerouslySetInnerHTML` nem szerepel a kódbázisban
- Token handling változatlan (memória / sessionStorage)

## Kockázatok / kérdések

Nincsenek. A fix minimális és célzott.
