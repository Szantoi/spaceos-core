---
id: MSG-FE-036-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-036
created: 2026-05-28
---

# FE-036 DONE — Identity UsersTab API integráció

## Státusz: KÉSZ

## Elvégzett munkák

### `src/hooks/useApi.ts`
- `API_BASE.identity: '/identity'` hozzáadva

### `src/pages/SettingsPage.tsx`
- `USERS` mock import eltávolítva
- `useApi`, `useMutation`, `API_BASE` importok hozzáadva
- `UserDto` és `CreateUserDto` interface-ek definiálva
- `UsersTab` teljes átírás:
  - `GET /identity/users` → user lista betöltése `useEffect` + `refetch`
  - Loading skeleton (3 animált sor)
  - Error fallback: "Nem sikerült betölteni a felhasználókat."
  - User lista: `firstName lastName`, email, status badge (Aktív = teal, Tiltott = stone), `kcSyncStatus: 'Failed'` → amber alert ikon
  - "Meghívás" gomb → inline form (Email, Keresztnév, Vezetéknév)
  - Form submit → `POST /identity/users` → refetch + form reset
  - Mentés közben disabled állapot + hibajelzés

### `src/pages/__tests__/SettingsPage.test.tsx`
- `vi.mock('../../auth', ...)` hozzáadva
- `vi.stubGlobal('fetch', ...)` + `afterEach(vi.unstubAllGlobals)` mintával
- Régi "Kovács" mock teszt eltávolítva
- 6 új users tab teszt:
  - loading skeleton megjelenik
  - API-ból betöltött user látható (Teszt User / test@test.hu)
  - Disabled badge (Tiltott)
  - Error fallback hiba esetén
  - Meghívás gomb + form megnyílik
- Meglévő tesztek (company, roles, facilities, stb.) változatlanul zöldek

## Tesztek
- **251 teszt, 0 fail** (volt 247, +4 nettó: 6 új - 2 korábbi users tab módosítva)
- `pnpm build` → 0 error
