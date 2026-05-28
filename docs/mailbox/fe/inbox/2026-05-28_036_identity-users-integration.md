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

# MSG-FE-036 — Identity UsersTab — valódi API + gazdag UI

## Kontextus

Az Identity modul deployed (5008, nginx `/identity/`). A SettingsPage `UsersTab`
jelenleg mock `USERS` adatot mutat. Ezt be kell kötni a valódi API-ra, **gazdagabb
vizuális megjelenéssel** — a PartnersPanel (SlideOver + részletes panel) legyen a minta.

---

## API contract

**Base URL:** `/identity`

| Method | URL | Policy | Leírás |
|---|---|---|---|
| `GET` | `/identity/users` | TenantMember | User lista |
| `POST` | `/identity/users` | TenantAdmin | Meghívás |
| `POST` | `/identity/users/{id}/disable` | TenantAdmin | Tiltás |
| `POST` | `/identity/users/{id}/enable` | TenantAdmin | Engedélyezés |
| `POST` | `/identity/users/{id}/reset-password` | TenantAdmin | Jelszó reset |

**UserDto:**
```typescript
interface UserDto {
  id: string
  tenantId: string
  email: string
  firstName: string
  lastName: string
  status: 'Active' | 'Disabled'
  kcSyncStatus: 'Pending' | 'Synced' | 'Failed'
}
```

**CreateUserDto (POST body):**
```typescript
{ email: string; firstName: string; lastName: string }
```

---

## UI specifikáció

### Layout — kétoszlopos (mint PartnersPanel)

```
┌─────────────────────────────────────┬──────────────────────┐
│ Felhasználók (N)       [+ Meghívás] │  Szinkronizáció      │
│─────────────────────────────────────│──────────────────────│
│ [Avatar] Kovács Péter               │  ● Synced   4        │
│          kovacs.peter@…   [Aktív]   │  ○ Pending  1        │
│          [chevron →]                │  ✕ Failed   0        │
│─────────────────────────────────────│                      │
│ [Avatar] Nagy János                 │──────────────────────│
│          nagy.j@…        [Tiltott]  │  Utolsó szinkron     │
│          [chevron →]                │  (lista frissítési   │
│                                     │   ideje)             │
└─────────────────────────────────────┴──────────────────────┘
```

**Bal panel (Card, lg:col-span-2, p-0):**
- Header: `"Felhasználók (N)"` + `<PrimaryBtn icon="plus">Meghívás</PrimaryBtn>`
- Soronként clickable (onClick → SlideOver megnyílik)
- Grid: `grid-cols-[auto_1fr_auto_auto]` gap-3 items-center px-5 py-3
  - Avatar (36×36, gradient teal, initials)
  - Név + email
  - Status badge
  - Chevron ikon

**Jobb panel (Card, self-start, p-0):**
- Header: `"Szinkronizáció"`
- 3 sor: Synced / Pending / Failed számok
  - Synced: `text-teal-600` ● pont
  - Pending: `text-amber-500` ○ pont
  - Failed: `text-red-500` ✕
- Separator + `"Utolsó lekérés: most"` felirat

---

### User kártya sorok

```
[TK] Tóth Kinga              [Aktív]    ›
     toth.k@…
     
[NJ] Nagy János    ⚠ Pending  [Aktív]    ›
     nagy.j@…
```

- **Avatar:** 36×36 kör, `bg-gradient-to-br from-teal-400 to-teal-600`, initials fehér 11px bold
  - Disabled user: `from-stone-300 to-stone-400` (szürke)
- **Status badge:**
  - `Active` → `bg-teal-50 text-teal-700 px-2 py-0.5 rounded-full text-[10.5px]` `"Aktív"`
  - `Disabled` → `bg-stone-100 text-stone-500` `"Tiltott"`
- **KcSync jelzés** (ha nem Synced):
  - `Pending` → `⏳` ikon + `"Szinkronban…"` `text-[10px] text-amber-500`
  - `Failed` → `⚠` ikon + `"Szinkron hiba"` `text-[10px] text-red-500`

---

### SlideOver — User részletek

`width={440}`, title = teljes név, subtitle = email

**Tartalom (px-5 py-4 space-y-5):**

```
┌──────────────────────────────────────┐
│ [Avatar 56×56]  Kovács Péter         │
│                 kovacs.peter@…       │
│                 [Aktív]  [Synced]    │
├──────────────────────────────────────│
│ Adatok                               │
│ Keresztnév   Kovács                  │
│ Vezetéknév   Péter                   │
│ Email        kovacs.peter@…          │
│ Azonosító    xxxxxxxx-…  [copy ikon] │
├──────────────────────────────────────│
│ Műveletek                            │
│ [🔑 Jelszó reset]   [⛔ Tiltás]     │
└──────────────────────────────────────┘
```

**Fejléc szekció:**
- Nagy avatar (56×56, gradient vagy szürke ha disabled)
- Név (`text-[15px] font-semibold`), email (`text-[12px] text-stone-500`)
- Status badge + KcSyncStatus badge egymás mellett

**Adatok szekció (`text-[12px]`):**
- `"Adatok"` label `text-[11px] font-semibold text-stone-500 uppercase tracking-wide`
- Key-value sorok: label `text-stone-500 w-28`, value `text-stone-900`
- UUID: truncate + copy gomb (`navigator.clipboard.writeText`)

**Műveletek szekció:**
- `"Műveletek"` label
- `<GhostBtn icon="key" onClick={resetPassword}>Jelszó reset</GhostBtn>`
- Ha `status === 'Active'`: `<GhostBtn className="text-red-600 hover:bg-red-50" onClick={disable}>Tiltás</GhostBtn>`
- Ha `status === 'Disabled'`: `<GhostBtn className="text-teal-600 hover:bg-teal-50" onClick={enable}>Engedélyezés</GhostBtn>`
- Műveletek után: rövid inline visszajelzés (`"✓ Jelszó reset elküldve"` stb.)

**SlideOver footer:**
- `<GhostBtn onClick={onClose}>Bezárás</GhostBtn>`

---

### Meghívás SlideOver

`width={400}`, title = `"Új felhasználó meghívása"`, subtitle = `"A felhasználó email értesítőt kap."`

**Form:**
```
Keresztnév *   [________________]
Vezetéknév *   [________________]
Email *        [________________]

              [Mégse]  [Meghívás →]
```

- Mindhárom mező kötelező — üres mező esetén piros border + `"Kötelező mező"`
- Submit → `useMutation POST /identity/users` → success: SlideOver bezár + lista refresh
- API hiba → `text-red-500 text-[12px]` a footer felett

---

## Kód szervezés

Új fájl: `src/components/settings/UsersPanel.tsx` — a teljes panel ide kerüljön ki (mint PartnersPanel.tsx). A `SettingsPage.tsx`-ben csak importálni kell:

```tsx
// SettingsPage.tsx
import { UsersPanel } from '../components/settings/UsersPanel'
// ...
{tab === 'users' && <UsersPanel />}
```

A `UsersPanel` maga kezeli a state-et, API hívásokat, SlideOveröket.

---

## `useApi.ts` kiegészítés

```typescript
export const API_BASE = {
  // meglévők változatlan...
  identity: '/identity',
} as const
```

---

## Tesztek

`src/pages/__tests__/SettingsPage.test.tsx` — meglévő users teszt frissítése:
- Mock fetch → `[{ id: 'u1', email: 'test@d.hu', firstName: 'Teszt', lastName: 'User', status: 'Active', kcSyncStatus: 'Synced', tenantId: 't1' }]`
- `"Teszt User"` megjelenik
- `"test@d.hu"` megjelenik
- `"Aktív"` badge megjelenik

`src/components/settings/__tests__/UsersPanel.test.tsx` (új):
- Disabled user → szürke avatar + `"Tiltott"` badge
- Failed sync → `"Szinkron hiba"` megjelenik
- Loading: fetch pending → skeleton / spinner látható
- Error fallback: fetch reject → hibaüzenet megjelenik
- Meghívás form: üres submit → validáció hiba megjelenik

---

## Definition of Done

- [ ] `API_BASE.identity` definiálva `useApi.ts`-ben
- [ ] `UsersPanel.tsx` létrehozva: valódi API, kétoszlopos layout, sync összesítő
- [ ] User sorok: avatar gradient, status badge, kcSync jelzés
- [ ] User SlideOver: adatok + műveletek (disable/enable/reset-password)
- [ ] Meghívás SlideOver: validált form, `POST /identity/users`
- [ ] `SettingsPage.tsx` `UsersTab` → `<UsersPanel />`
- [ ] `pnpm build` → 0 error, 0 warning
- [ ] `pnpm test` → minden teszt zöld
