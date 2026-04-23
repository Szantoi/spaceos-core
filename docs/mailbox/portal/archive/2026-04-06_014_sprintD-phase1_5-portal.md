---
id: MSG-P014
from: architect
to: portal
type: task
status: UNREAD
priority: P0
sprint: "Sprint D · Phase 1.5"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase1_5_v4.md"
---

# Sprint D · Phase 1.5 — Token Refresh Interceptor (Portal)

## Összefoglaló

A Kernel ES256-ra vált és az Access Token lifetime **8 óráról 15 percre** csökken. A Portal-nak automatikus token refresh mechanizmust kell implementálnia, különben a felhasználók 15 percenként kijelentkeznek.

**Blokkolva:** Az Orchestrator és Kernel refresh endpoint implementációjáig NEM tudod end-to-end tesztelni. Addig mock-kal dolgozz.

---

## Feladat 1 — Token Refresh Interceptor

### Működés

```
1. API hívás → 401 Unauthorized (lejárt AT)
2. Interceptor automatikusan hívja: POST /bff/auth/refresh { refreshToken }
3. Új AT + RT pár érkezik
4. Eredeti kérés újrapróbálása az új AT-vel
5. Ha a refresh is fail-el → kijelentkeztetés (login page redirect)
```

### Implementáció

1. **Axios/fetch interceptor** (`src/api/` vagy `src/lib/`):
   ```typescript
   // Pseudo-kód — a pontos implementáció a meglévő API réteghez igazodjon

   // Response interceptor:
   // if (response.status === 401 && !originalRequest._retry) {
   //   originalRequest._retry = true;
   //   const { accessToken, refreshToken } = await refreshTokens();
   //   updateStoredTokens(accessToken, refreshToken);
   //   originalRequest.headers.Authorization = `Bearer ${accessToken}`;
   //   return apiClient(originalRequest);
   // }
   ```

2. **Token tárolás frissítés:**
   - Access Token: memóriában (változatlan)
   - Refresh Token: memóriában VAGY `localStorage` (secure — opaque token, nem JWT)
   - **NE** tárold a refresh token-t cookie-ban (CSRF kockázat)

3. **Concurrent request kezelés:**
   - Ha több request kap egyszerre 401-et, **CSAK EGY** refresh hívás induljon
   - A többi request várakozzon a refresh eredményére (promise queue pattern)
   - Ez kritikus — refresh token rotation miatt a második refresh hívás FAIL-elne (az első már revokálta a régi RT-t)

4. **Logout kezelés:**
   - `POST /bff/auth/logout` hívás a jelenlegi refresh token-nel
   - Token-ek törlése memóriából/localStorage-ból
   - Redirect login page-re

### Fájlok (javasolt, a meglévő struktúrához igazítsd)

| Fájl | Tartalom |
|------|----------|
| `src/api/tokenRefresh.ts` (vagy `src/lib/`) | Refresh interceptor logika + concurrent queue |
| `src/api/authService.ts` (meglévő bővítés) | `refreshTokens()` + `logout()` hívások |
| `src/stores/` vagy `src/hooks/` | Token state management (ha van auth store) |

---

## Feladat 2 — Token Expiry Proaktív Refresh (opcionális, ajánlott)

A 401-alapú refresh reaktív — a felhasználó rövid lag-ot érezhet. Proaktív megoldás:

```typescript
// A JWT payload-ból olvasd ki az exp claim-et
// Ha exp - now < 2 perc → háttérben refresh
// Így a legtöbb esetben a felhasználó nem is érzékeli a token váltást
```

---

## Feladat 3 — Tesztek

### Unit tesztek (Vitest + Testing Library)
- [ ] 401 response → automatikus refresh hívás
- [ ] Sikeres refresh → eredeti request újrapróbálása új AT-vel
- [ ] Refresh fail (pl. expired RT) → kijelentkezés + redirect
- [ ] Concurrent 401-ek → egyetlen refresh hívás (queue test)
- [ ] Logout → token-ek törlése + `/bff/auth/logout` hívás

### Edge case-ek
- [ ] Network error refresh közben → kijelentkezés
- [ ] Refresh válaszban hiányzó token → kijelentkezés
- [ ] Infinite retry loop védelem (max 1 retry per request)

---

## DoD

- [ ] Token refresh interceptor implementálva
- [ ] Concurrent request queue működik (1 refresh / N waiting request)
- [ ] Logout implementálva (BFF hívás + local cleanup)
- [ ] Meglévő tesztek zöldek (224+)
- [ ] Új tesztek: ≥ 5 db
- [ ] 0 TypeScript error
- [ ] `npm audit` → 0 high/critical

---

## ⚠️ Fontos kontextus

- **BFF endpointok (Orchestrator):**
  - `POST /bff/auth/refresh` — body: `{ "refreshToken": "..." }` → response: `{ "accessToken": "...", "refreshToken": "..." }`
  - `POST /bff/auth/logout` — body: `{ "refreshToken": "..." }` → response: 200 OK
- **Access Token lifetime:** 15 perc (régi: 8 óra)
- **Refresh Token lifetime:** 8 óra
- **Token rotation:** Minden refresh hívás új AT + RT párt ad — a régi RT érvénytelen lesz
- **KRITIKUS:** Concurrent refresh queue nélkül a token rotation BREAK-eli a párhuzamos request-eket!
