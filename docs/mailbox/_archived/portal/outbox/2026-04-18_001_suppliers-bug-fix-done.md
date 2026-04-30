---
id: MSG-PORTAL-001-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-001
created: 2026-04-18
---

## Összefoglaló

**BUG-001 (email/phone nem mentődik):**
Portal kód elemzés alapján a frontend HELYES — a `CreateSupplierModal` onSubmit handlere tartalmazza `email` és `phone` mezőket a POST request bodyban, a `suppliersApi.create` ezeket átadja az axiosnak, és a `SuppliersTable` rendereli `{s.email || '—'}` formában.

**Ez BFF/Kernel oldali hiba** — a `/bff/procurement/suppliers` POST valószínűleg nem menti vagy nem adja vissza az email/phone mezőket a lista nézetben. Portal oldalon nincs teendő.

**BUG-002 (közvetlen URL → redirect főoldalra):**
Javítva. Két fájl módosult:
- `ProtectedRoute.tsx`: `redirectToLogin()` előtt a jelenlegi URL-t `sessionStorage`-be menti (`spaceos:redirect_after_login`)
- `CallbackPage.tsx`: sikeres auth után a `sessionStorage`-ből olvassa vissza a célútvonalat, navigál oda, majd törli a bejegyzést

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/features/auth/ProtectedRoute.tsx` | sessionStorage mentés redirectToLogin előtt |
| `src/features/auth/CallbackPage.tsx` | sessionStorage olvasás navigate helyett hardcoded `/` |
| `src/features/auth/ProtectedRoute.test.tsx` | új teszt: sessionStorage tartalmát ellenőrzi |
| `src/features/auth/CallbackPage.test.tsx` | új teszt: /suppliers-re navigál tárolt path esetén |

## Tesztek

309 / 309 zöld. +2 új teszt (BUG-002 coverage).

## Security review

- XSS: `sessionStorage` plain string kulcs+érték, nincs HTML inject
- Auth: redirect path csak relatív útvonalas string (pl. `/suppliers`) — teljes URL injection nem lehetséges (navigate() Router-en belül dolgozik)
- Token handling: tokenek memóriában, sessionStorage csak a redirect path-t tárolja
- Input sanitization: n/a
- OWASP: open redirect nem lehetséges mivel `navigate()` React Router-en belüli navigáció

## BFF/Kernel issue — nyitott kérdés

BUG-001: A Portal POST body tartalmazza az `email` és `phone` mezőket. Javasolt: BFF/Kernel terminál vizsgálja meg, hogy a `POST /procurement/suppliers` handler ezeket menti-e, és a `GET /procurement/suppliers` lista response tartalmazza-e.

## Commit

`e01f403` — fix(auth): preserve redirect URL on deep-link + direct URL (PORTAL-001 BUG-002)
