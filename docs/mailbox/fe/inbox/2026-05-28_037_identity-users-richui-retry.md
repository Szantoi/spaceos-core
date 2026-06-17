---
id: MSG-FE-037
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-036-DONE
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-FE-037 — FE-036 visszadobás: gazdag UI hiányzik

## Értékelés

A FE-036 DONE az alap API integrációt elvégezte (useApi, fetch, loading, error, inline form) — ez helyes alap, de a specifikáció második, gazdagabb részét **nem implementálta**.

A felhasználó kifejezetten kérte a vizuálisan gazdagabb megjelenést. A gazdag UI spec a MSG-FE-036-ban szerepelt.

## Hiányok — ezeket kell megvalósítani

### 1. `UsersPanel.tsx` — külön komponens (kötelező)

Hozd létre: `src/components/settings/UsersPanel.tsx`

Az összes logikát mozgasd ide a `SettingsPage.tsx` `UsersTab` funkciójából. A `SettingsPage.tsx`-ben ezután csak:
```tsx
import { UsersPanel } from '../components/settings/UsersPanel'
// ...
{tab === 'users' && <UsersPanel />}
```

### 2. Kétoszlopos layout (kötelező)

```tsx
<div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
  {/* Bal: user lista — lg:col-span-2 */}
  {/* Jobb: szinkronizáció összesítő */}
</div>
```

**Jobb panel (Card, self-start):**
- Header: `"Szinkronizáció"`
- 3 sor a betöltött adatokból számolva:
  - `●` Synced: `text-teal-600` + darabszám
  - `○` Pending: `text-amber-500` + darabszám
  - `✕` Failed: `text-red-500` + darabszám
- Separator + `"Utolsó lekérés: most"` szöveg

### 3. User sor → SlideOver (kötelező)

Minden user sor kattintható → `UserDetailSlideOver` megnyílik.

`width={440}`, title = `"${u.firstName} ${u.lastName}"`, subtitle = `u.email`

**SlideOver tartalma (px-5 py-4 space-y-5):**

```
[Avatar 56×56]  Kovács Péter
                kovacs.peter@…
                [Aktív]  [Synced]

── Adatok ──────────────────────────
Keresztnév   Kovács
Vezetéknév   Péter
Email        kovacs.peter@…
Azonosító    xxxxxxxx-…  [📋]

── Műveletek ───────────────────────
[🔑 Jelszó reset]   [⛔ Tiltás / ✅ Engedélyezés]
```

- Nagy avatar 56×56 (Active: `from-teal-400 to-teal-600`, Disabled: `from-stone-300 to-stone-400`)
- UUID copy: `navigator.clipboard.writeText(u.id)` → `"✓ Másolva"` 2s-ig
- `"Adatok"` / `"Műveletek"` section label: `text-[11px] font-semibold text-stone-500 uppercase tracking-wide mb-2`
- Key-value sorok: `<dt className="text-stone-500 w-28 shrink-0">`, `<dd className="text-stone-900">`
- **Jelszó reset gomb:** `POST /identity/users/{id}/reset-password` → `"✓ Jelszó reset elküldve"` inline visszajelzés
- **Tiltás gomb** (ha Active): `POST /identity/users/{id}/disable` → refetch lista + SlideOver frissül
- **Engedélyezés gomb** (ha Disabled): `POST /identity/users/{id}/enable` → refetch + frissül
- Gomb közben: `disabled + opacity-60`

**SlideOver footer:** `<GhostBtn onClick={onClose}>Bezárás</GhostBtn>`

### 4. Meghívás → SlideOver (kötelező, az inline form helyett)

A jelenlegi inline form helyett `InviteUserSlideOver` komponens:

`width={400}`, title = `"Új felhasználó meghívása"`, subtitle = `"A felhasználó email értesítőt kap."`

```
Keresztnév *   [________________]
Vezetéknév *   [________________]
Email *        [________________]

              [Mégse]  [Meghívás →]
```

- Ha bármely mező üres: piros border + `"Kötelező mező"` a mező alatt
- Submit: `POST /identity/users` → success: SlideOver bezár + lista refetch
- API hiba: `text-red-500 text-[12px]` a footer felett

### 5. Tesztek (kötelező)

Új fájl: `src/components/settings/__tests__/UsersPanel.test.tsx`

```typescript
// Tesztelendő esetek:
// - Disabled user → "Tiltott" badge + szürke avatar class
// - Failed sync → "Szinkron hiba" szöveg megjelenik
// - Loading state → skeleton / spinner látható
// - Error fallback → hibaüzenet megjelenik
// - Sync összesítő → Synced/Pending/Failed számok helyesek
```

A meglévő `SettingsPage.test.tsx` users tesztek maradhatnak — kiegészíteni kell hogy a `<UsersPanel />` importot mock-olja, vagy a panel testjei lefedik.

---

## Ami maradhat az FE-036 DONE-ból (NEM kell újraírni)

- `API_BASE.identity: '/identity'` — ✅ marad
- `UserDto` / `CreateUserDto` interface-ek — ✅ maradhatnak (esetleg `UsersPanel.tsx`-be mozgatva)
- `GET /identity/users` fetch logika — ✅ átvinni `UsersPanel.tsx`-be
- Loading skeleton, error fallback — ✅ átvinni
- `SettingsPage.test.tsx` új tesztek — ✅ maradnak, kiegészíteni kell

## Definition of Done

- [ ] `UsersPanel.tsx` létrehozva, `SettingsPage.tsx`-ben `<UsersPanel />` importálva
- [ ] Kétoszlopos layout + szinkronizáció összesítő jobb panelen
- [ ] User sor kattintásra `UserDetailSlideOver` nyílik (440px)
- [ ] SlideOver: adatok szekció + UUID copy + műveletek (reset/disable/enable)
- [ ] Meghívás `InviteUserSlideOver` (400px, validált form, POST)
- [ ] `UsersPanel.test.tsx` létrehozva (≥4 teszt)
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → minden teszt zöld
