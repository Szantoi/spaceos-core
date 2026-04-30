---
id: MSG-P014-DONE
from: portal
to: architect
type: done
ref: MSG-P014
sprint: "Sprint D · Phase 1.5"
---

# Sprint D · Phase 1.5 — Token Refresh Interceptor — DONE

## Összefoglaló

Token refresh interceptor implementálva a concurrent request queue pattern-nel.
Az Orchestrator/Kernel refresh endpoint-ok nélkül mock-kal tesztelve (ahogy a feladat kérte).

---

## DoD Checklist

- [x] Token refresh interceptor implementálva (`src/api/client.ts`)
- [x] Concurrent request queue működik (1 refresh / N waiting request)
- [x] Logout bővítve (BFF hívás + local cleanup) — `authService.logout()`
- [x] Meglévő tesztek zöldek: **237 PASSED** (224 → 237, +13 új)
- [x] Új tesztek: **13 db** (8 token refresh + 4 auth store + 1 router fix)
- [x] 0 TypeScript error
- [x] `npm audit` → 0 high/critical (4 moderate — meglévők)

---

## Implementált fájlok

| Fájl | Változás | Tartalom |
|------|----------|----------|
| `src/api/client.ts` | **MODIFIED** | 401 interceptor → token refresh + concurrent queue |
| `src/api/auth.service.ts` | **MODIFIED** | + `refreshTokens()` + `logout()` endpointok |
| `src/store/auth.store.ts` | **MODIFIED** | + `refreshToken` state, + `setTokens()` action, persist bővítve |
| `src/types/index.ts` | **MODIFIED** | + `RefreshTokenRequest`, `RefreshTokenResponse`, `TokenResponse.refreshToken?` |
| `src/features/auth/LoginPage.tsx` | **MODIFIED** | `login(token, refreshToken)` hívás |
| `src/api/tokenRefresh.test.ts` | **NEW** | 8 teszt — refresh flow, concurrent queue, logout, edge cases |
| `src/store/auth.store.test.ts` | **MODIFIED** | + 4 teszt — refreshToken, setTokens |
| `src/router/index.test.tsx` | **MODIFIED** | Mock AuthState kiegészítve refreshToken + setTokens |
| `src/api/client.test.ts` | **MODIFIED** | Mock store kiegészítve refreshToken |

---

## Architektúra

```
Request → 401 Unauthorized
  ├─ Van refreshToken? → NEM → forceLogout() → /login
  └─ Van refreshToken? → IGEN
       ├─ Már fut refresh? → IGEN → queue-ba (Promise wait)
       └─ Első 401 → axios.post('/bff/auth/refresh', { refreshToken })
            ├─ Sikeres → setTokens(newAT, newRT) → retry original + queue drain
            └─ Sikertelen → forceLogout() → /login + queue reject
```

**Concurrent queue**: Token rotation miatt EGY refresh hívás fut egyszerre.
A többi 401-es request Promise-ban vár, majd az új token-nel retry-ol.

**Circular dependency megoldás**: A refresh hívás `axios.post`-ot használ közvetlenül (nem `apiClient`-et), így az interceptor nem triggerelődik rekurzívan.

---

## Tesztek részletezése (13 új)

### tokenRefresh.test.ts (8 teszt)
1. 401 → automatikus refresh hívás
2. Sikeres refresh → token update a store-ban
3. Refresh fail → logout + redirect /login
4. Concurrent 401-ek → egyetlen refresh hívás
5. Nincs refreshToken → logout refresh nélkül
6. _retry=true → logout refresh nélkül (infinite loop védelem)
7. Invalid refresh response → logout
8. Network error refresh közben → logout

### auth.store.test.ts (+4 teszt)
9. login() tárolja a refreshToken-t
10. login() refreshToken nélkül → null
11. setTokens() frissíti mindkét token-t + user-t
12. logout() törli a refreshToken-t

### router/index.test.tsx (+1 fix)
13. Mock AuthState kiegészítve az új mezőkkel

---

## Build eredmények

| Metrika | Érték |
|---------|-------|
| TypeScript errors | 0 |
| Test files | 31 passed |
| Tests | 237 passed |
| npm audit high/critical | 0 |

---

## Megjegyzés

A Feladat 2 (proaktív refresh — JWT exp figyelés) opcionális volt, NEM implementáltam.
Egyszerűen bővíthető: a request interceptor-ban ellenőrizni kell az `exp` claim-et,
és ha `exp - now < 120s`, háttérben refresh-elni. Az infrastruktúra (queue, setTokens) kész hozzá.
