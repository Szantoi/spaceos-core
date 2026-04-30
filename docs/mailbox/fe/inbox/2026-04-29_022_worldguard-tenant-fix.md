---
id: MSG-FE-022
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-29
---

# FE-022 — WorldGuard tenant null fix (aloldalak nem nyílnak meg)

> **BUG:** A world kártyákra klikkelve a WorldGuard redirect-el vissza `/`-re mert `tenant === null`.
> **Root cause:** A `tenantStore` sosem töltődik be — nincs hívás ami a `/bff/api/me/session`-t fetch-elné.

## Fix — 2 opció, mindkettő OK

### Opció A (gyors): WorldGuard tenant null → enged át

```typescript
// WorldGuard.tsx
if (!tenant) {
    return <>{children}</>; // tenant not loaded yet, allow access
}
```

### Opció B (teljes): Session fetch a ProtectedRoute-ban vagy App mount-on

A `ProtectedRoute` vagy `WorldShell` mount-kor hívd a `/bff/api/me/session`-t és töltsd be a tenantStore-t.

## Definition of Done

- [ ] World kártyákra klikkelve az aloldal megnyílik (nem redirect)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 251 pass
- [ ] Outbox DONE
