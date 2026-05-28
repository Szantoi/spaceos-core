---
id: MSG-FE-036
from: root
to: fe
type: task
priority: high
status: UNREAD
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-FE-036 — Identity UsersTab API integráció

## Kontextus

Az Identity modul deployed és fut (5008, nginx `/identity/`). A SettingsPage `UsersTab`
jelenleg mock `USERS` adatot mutat. Ezt be kell kötni a valódi API-ra.

## API contract

**Base URL:** `/identity` (nginx proxy → 5008)

| Method | URL | Leírás |
|---|---|---|
| `GET` | `/identity/users` | Tenant userek listája (JWT: TenantMember) |
| `POST` | `/identity/users` | Új user létrehozása (JWT: TenantAdmin) |
| `POST` | `/identity/users/{id}/disable` | User tiltás |
| `POST` | `/identity/users/{id}/enable` | User engedélyezés |
| `POST` | `/identity/users/{id}/reset-password` | Jelszó reset (rate limited 5/user/hour) |

**UserDto (GET /identity/users response item):**
```typescript
interface UserDto {
  id: string           // UUID
  tenantId: string     // UUID
  email: string
  firstName: string
  lastName: string
  status: 'Active' | 'Disabled'
  kcSyncStatus: 'Pending' | 'Synced' | 'Failed'
}
```

**CreateUserDto (POST /identity/users body):**
```typescript
interface CreateUserDto {
  email: string
  firstName: string
  lastName: string
}
```

## Feladat

### 1. `useApi.ts` — `identity` hozzáadása az `API_BASE`-hez

```typescript
export const API_BASE = {
  // ... meglévők ...
  identity: '/identity',
} as const
```

### 2. `SettingsPage.tsx` — `UsersTab` átírása

- Mock `USERS` import eltávolítása a `UsersTab`-ból
- `useApi<UserDto[]>` → `GET /identity/users`
- `useEffect` → fetch on mount
- Loading skeleton (3 sor, a többi lap mintájára)
- Error fallback: `<p className="text-[12px] text-red-500 px-5 py-3">Nem sikerült betölteni a felhasználókat.</p>`
- User lista renderelése a jelenlegi designnal:
  - `${u.firstName} ${u.lastName}` → name
  - Initials: `${u.firstName[0]}${u.lastName[0]}`.toUpperCase()
  - `u.email`
  - `u.status` badge: Active → `bg-teal-100 text-teal-700`, Disabled → `bg-stone-100 text-stone-500`
  - `u.kcSyncStatus === 'Failed'` → sárga figyelmeztető ikon a sor végén
- "Meghívás" gomb: `POST /identity/users` — egyszerű modal vagy inline form:
  - 3 mező: Email, Keresztnév, Vezéték­név
  - Submit → `useMutation` → refetch lista
  - Hiba esetén inline error szöveg

### 3. Tesztek

`SettingsPage.test.tsx`-ben:
- Mock `fetch` → `[{ id: '...', email: 'test@test.hu', firstName: 'Teszt', lastName: 'User', status: 'Active', kcSyncStatus: 'Synced', tenantId: '...' }]`
- Ellenőrzés: `"Teszt User"` megjelenik
- Ellenőrzés: `"test@test.hu"` megjelenik
- Disabled user badge: `status: 'Disabled'` → badge szöveg megjelenik
- Loading state: fetch pending → skeleton látható (vagy legalább nem crash)

## Definition of Done

- [ ] `API_BASE.identity` definiálva
- [ ] `UsersTab`: `GET /identity/users` — valódi adat, nem mock
- [ ] Loading + error state kezelve
- [ ] User lista: name, email, status badge, kcSyncStatus Failed ikon
- [ ] "Meghívás" gomb: `POST /identity/users` működik (vagy legalább stateként megnyílik a form)
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → minden teszt zöld (új tesztek is)

## Megjegyzések

- A `USERS` mock importot csak a `UsersTab` funkcióból kell eltávolítani — ha más helyen is használt, azokat ne bántsd
- A `role` mező jelenleg nincs az Identity API-ban (KC szerepkörök — v2 scope) → a badge mostantól `status`-t mutat
- `kcSyncStatus: 'Pending'` normális újonnan létrehozott usernél — nem hiba, csak jelzés
- A `useMutation` hook már implementált a `useApi.ts`-ben
