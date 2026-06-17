# Sub-skill: Senior Security Review (Frontend)

Alkalmazd ezt az analitikai keretrendszert a frontend tervdokumentumra.
Minden finding-et `SEC-FE-NN` prefixszel dokumentálj.

---

## Review checklist — 18 pont

### Authentication & Token Handling (SEC-FE-01..06)

1. **Token storage** — JWT access token CSAK memóriában (Zustand, nem persist). Refresh token httpOnly cookie-ban. LocalStorage/sessionStorage → 🔴 CRITICAL.
2. **Token refresh** — silent refresh flow implementálva? Token lejárat előtt automatikus refresh?
3. **Logout cleanup** — logout-nál MINDEN store resetelődik? Zustand `reset()` + TanStack Query `queryClient.clear()`?
4. **JWT decode** — a frontend SOHA nem bízik a JWT payload-ban döntésekhez. `enabledModules` és `role` az Orchestrator BFF verified response-ból jön.
5. **PKCE flow** — Keycloak PKCE code_verifier nem log-olódik console-ra?
6. **Session timeout** — inaktivitás után (15 min) automatikus kijelentkezés + redirect login-ra?

### Authorization & Route Protection (SEC-FE-07..10)

7. **enabledModules trust boundary** — a `enabledModules` SOHA nem a JWT-ből jön közvetlenül. Az Orchestrator validálja és a BFF response body-ban adja vissza.
8. **Route guard** — nem-engedélyezett world URL-be navigálás → redirect Home, SOHA nem renderel tartalmat röviden sem (flash of unauthorized content).
9. **Role-based rendering** — admin-only UI elemek (Settings, Roles mátrix) a szerveren is validálva vannak? A frontend hide != security.
10. **Shop Floor PIN isolation** — a PIN login flow nem ad teljes portál hozzáférést. A Shop Floor session korlátozott scope-ú (csak assigned machines, csak task view).

### XSS & Injection (SEC-FE-11..14)

11. **dangerouslySetInnerHTML** — használva van valahol? Ha igen → 🔴 CRITICAL (nincs indokolt use case a portálon).
12. **URL parameter injection** — a route paraméterekből vett értékek sanitizálva vannak mielőtt renderelésre kerülnek?
13. **Search input** — keresési query-k escaped-ek API hívás előtt?
14. **Parametric template formula** — a sablon szerkesztő `resolveFormula` funkciója `new Function()`-t használ? Ha igen → sandbox szükséges (𝐖eb Worker vagy controlled evaluator).

### Data Exposure (SEC-FE-15..18)

15. **API key masking** — PartnerTier API kulcsok SOHA nem jelennek meg teljes formában. Maszkolt: `egr_••••••••a3f4`.
16. **Error message leak** — backend error response-ok nem jelennek meg raw formában a UI-on. Generic "Hiba történt" + error code.
17. **Console logging** — production build-ben NINCS `console.log` sensitive data-val (token, user data, API response body).
18. **Network tab** — a BFF response-ok nem tartalmaznak más tenant adatait (RLS server-side, de ellenőrizd).

---

## Shop Floor specifikus biztonsági kérdések

| Kérdés | Elvárás |
|--------|---------|
| PIN brute force | Max 5 próbálkozás → 5 perc lockout |
| PIN → session scope | Shop Floor session SOHA nem ad hozzáférést a portal többi világához |
| QR scan | A QR kód tartalmaz machine ID-t, NEM auth token-t |
| Offline mode | Ha a tablet elveszíti a kapcsolatot → SEMMILYEN adat nem cached lokálisan (nincs offline queue) |
| Photo upload (proof) | A fotó közvetlenül a BFF-re megy (nem third-party), max 5MB, MIME check |

---

## Finding súlyok — security specifikus

| Súly | Mikor |
|------|-------|
| 🔴 CRITICAL | Token in localStorage, JWT payload trust, dangerouslySetInnerHTML, enabledModules bypass, PIN unlimited retry |
| 🟠 HIGH | Missing route guard, flash of unauthorized content, raw error display, Formula eval without sandbox |
| 🟡 MEDIUM | Missing session timeout, console.log in prod, incomplete logout cleanup |
| 🟢 LOW | ARIA security attributes, CSP reporting |
