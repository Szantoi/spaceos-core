---
id: MSG-PORTAL-004
from: root
to: portal
type: task
priority: high
status: READ
ref: R-17
created: 2026-04-15
---

# MSG-PORTAL-004 — Sprint 5: Test Coverage — PKCE auth + refresh race

## Háttér

Devils-advocate audit (2026-04-15) két kritikus gap-et azonosított (R-17):

1. A `CallbackPage.tsx` nincs tesztelve egyetlen OAuth hibaesetére sem — a state mismatch CSRF vektor
2. Az `AuthStore.refreshToken()` párhuzamos hívása session kill-t okozhat Keycloak-ban (refresh token reuse detection)

## Feladat

### 1. `CallbackPage.test.tsx`

Tesztelj minden OAuth callback hibát:

| Eset | Elvárás |
|---|---|
| `?error=access_denied` | Hibaoldal megjelenik, redirect login-ra |
| `?error=invalid_state` (state mismatch) | Hibaoldal + security log bejegyzés |
| `code_verifier` hiányzik sessionStorage-ból | Hibaoldal, nem fagy |
| Keycloak token endpoint 400 | Hibaoldal, nem végtelen loop |
| Keycloak token endpoint 500 | Hibaoldal + retry lehetőség |

### 2. `authStore.concurrent.test.ts`

```typescript
// Két párhuzamos refresh — csak 1 hálózati kérés kell
const [r1, r2] = await Promise.all([
  authStore.refreshToken(),
  authStore.refreshToken()
]);
// Assert: fetch hívások száma === 1 (deduplicated)
```

`vi.useFakeTimers()` + `vi.spyOn(fetch)` segítségével.

### 3. Coverage threshold

Adj hozzá a `vitest.config.ts`-hez coverage threshold-ot:
- `src/auth/**` → min 80% branch coverage
- `src/stores/**` → min 80% branch coverage

## DoD

- [ ] `CallbackPage.test.tsx` — 5 OAuth hibaeset zöld
- [ ] `authStore.concurrent.test.ts` — dedupe teszt zöld
- [ ] Coverage threshold beállítva `vitest.config.ts`-ben
- [ ] Tesztszám ≥ 291
- [ ] DONE outbox: új tesztszám + összefoglaló

