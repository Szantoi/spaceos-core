---
id: MSG-FE-037-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-037
created: 2026-05-28
---

# FE-037 DONE — Identity UsersPanel gazdag UI

## Státusz: KÉSZ

## Elvégzett munkák

### `src/components/settings/UsersPanel.tsx` — új komponens
- `UserDto` és `CreateUserDto` interface-ek ide mozgatva
- `useApi<UserDto[]>` → `GET /identity/users`, `useEffect` + `refetch` auto-fetch
- **Kétoszlopos layout**: `grid grid-cols-1 lg:grid-cols-3 gap-3`
  - Bal (`lg:col-span-2`): user lista Card
  - Jobb: `SyncSummary` Card (Synced/Pending/Failed számokkal + "Utolsó lekérés: most")
- **User sorok kattinthatók** → `UserDetailSlideOver` megnyílik
- **`UserDetailSlideOver`** (width=440):
  - 56×56 avatar (Active: teal gradiens, Disabled: stone gradiens)
  - Aktív/Tiltott + kcSyncStatus badge
  - "Adatok" szekció: Keresztnév, Vezetéknév, Email, Azonosító + 📋 UUID copy (`"✓ Másolva"` 2s)
  - "Műveletek" szekció: 🔑 Jelszó reset (`POST /identity/users/{id}/reset-password`) + ⛔ Tiltás / ✅ Engedélyezés (`POST /identity/users/{id}/disable|enable`) → refetch
  - Footer: `GhostBtn` "Bezárás"
- **`InviteUserSlideOver`** (width=400) — az inline form helyett:
  - Keresztnév / Vezetéknév / Email mezők validációval (üres → piros border + "Kötelező mező")
  - Submit: `POST /identity/users` → SlideOver bezár + refetch
  - API hiba: footer felett `text-red-500`
  - Footer: Mégse + Meghívás → gomb
- Failed sync sor: `"Szinkron hiba"` szöveg (amber)

### `src/pages/SettingsPage.tsx`
- `UsersPanel` import hozzáadva
- `{tab === 'users' && <UsersPanel />}` — `UsersTab` lecserélve
- `UsersTab` function, `UserDto`, `CreateUserDto`, `useApi`/`useMutation`/`API_BASE` import eltávolítva

### `src/components/settings/__tests__/UsersPanel.test.tsx` — új fájl (7 teszt)
- Loading skeleton → `animate-pulse` elemek
- Error fallback → "Nem sikerült" szöveg
- Disabled user → "Tiltott" badge + `from-stone-300` avatar class
- Failed sync → "Szinkron hiba" szöveg megjelenik
- Sync összesítő → Synced/Pending/Failed label-ek
- Meghívás gomb → InviteUserSlideOver nyílik ("Új felhasználó meghívása")
- User sor kattintás → UserDetailSlideOver nyílik ("Adatok" / "Műveletek")

### `src/pages/__tests__/SettingsPage.test.tsx`
- "Meghívás küldése" teszt frissítve → "Új felhasználó meghívása" (SlideOver cím)

## Tesztek
- **258 teszt, 0 fail** (volt 251, +7 nettó)
- `pnpm build` → 0 error
