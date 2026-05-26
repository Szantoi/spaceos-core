---
id: MSG-FE-035
from: root
to: fe
type: task
priority: high
status: UNREAD
created: 2026-05-26
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# FE-035 — Raktár bugfix: embedded Mozgások + Beszerzés nem jelenik meg

## Kontextus

A felhasználó két bugot jelzett az FE-032–034 lezárása után:

1. **Raktár Áttekintés → Készletmozgás tab: nincsenek szűrés gombok**
2. **Raktár Beszerzés screen nem jelenik meg**

---

## Bug 1 — `MovementsPage` embedded módban: szűrés gombok nem láthatók

### Diagnózis

Az `InventoryPage` tartalmazza a `MovementsPage`-t `MovementsTab` aliassal, a `movements` tab aktíválásakor. Az `InventoryPage` saját `px-7 py-6` wrapperrel rendelkezik, és a `MovementsPage` saját belső `px-7 py-6 space-y-4` wrapperrel is — ez **duplázott padding**. Ráadásul a `MovementsPage` rendereli a 4 db summary carddal (`grid grid-cols-2 md:grid-cols-4 gap-3`) is, ami ~100px magasságot foglal el, és kisebb viewport esetén a szűrés gombok (filter bar) a fold alá eshetnek.

### Fix

#### `src/pages/warehouse/MovementsPage.tsx` — `embedded` prop hozzáadása

```tsx
interface MovementsPageProps {
  embedded?: boolean
}

export function MovementsPage({ embedded = false }: MovementsPageProps) {
  const [filter, setFilter] = useState<FilterKey>('all')
  const [range, setRange] = useState('week')
  const [search, setSearch] = useState('')

  // ... types, filtered, totals változatlan ...

  return (
    <div className={embedded ? 'space-y-4' : 'px-7 py-6 space-y-4'}>
      {/* Summary cards — CSAK standalone módban */}
      {!embedded && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {/* ... 4 Card változatlan ... */}
        </div>
      )}

      {/* Filter bar — mindig látható */}
      <div className="flex items-center gap-2 flex-wrap">
        {/* ... változatlan ... */}
      </div>

      {/* Movement timeline — változatlan */}
      <Card className="p-0 overflow-hidden">
        {/* ... változatlan ... */}
      </Card>
    </div>
  )
}
```

#### `src/pages/InventoryPage.tsx` — embedded prop átadása

```tsx
// VOLT:
{tab === 'movements' && <MovementsTab />}

// LEGYEN:
{tab === 'movements' && <MovementsTab embedded />}
```

---

## Bug 2 — `ProcurementPage` nem jelenik meg a `/w/warehouse/procurement` route-on

### Diagnózis

A kód helyesnek tűnik (`WarehouseWorldPage` renderContent + route + import), az izolált `ProcurementPage` tesztek is pass-olnak (5 teszt). Viszont **nincs egyetlen route-szintű teszt sem** a raktár worldra. Kell:

1. Ellenőrizni, hogy a WorldShell kontextusában a ProcurementPage megjelenik-e
2. App.test.tsx-be warehouse route teszteket írni

### Fix

#### `src/__tests__/App.test.tsx` — warehouse route tesztek hozzáadása

Az App.test.tsx meglévő `describe('App Router', ...)` blokkba add hozzá:

```tsx
it('renders warehouse overview', () => {
  renderApp('/w/warehouse')
  // InventoryPage renders
  const matches = screen.getAllByText(/Akt\u00edv megrendel/)
  // fallback: ha nem inventory, legalább warehouse world shell
  const shells = screen.getAllByText(/Rakt\u00e1r/)
  expect(shells.length).toBeGreaterThan(0)
})

it('renders warehouse procurement screen', () => {
  renderApp('/w/warehouse/procurement')
  expect(screen.getByText('Akt\u00edv megrendel\u00e9sek')).toBeTruthy()
  expect(screen.getByText('Sz\u00e1ll\u00edt\u00f3k')).toBeTruthy()
})

it('renders warehouse movements screen', () => {
  renderApp('/w/warehouse/movements')
  expect(screen.getByText('Anyagmozg\u00e1s napl\u00f3')).toBeTruthy()
})
```

Ha a `warehouse procurement` teszt **elbukik**, az azt jelenti hogy a ProcurementPage nem renderel a route kontextusban. Ez esetben: ellenőrizd a console errorokat a tesztben (`console.error` spy), és debug-old mi okozza a render-blokkolást.

**Ha a tesztek zöldek de a böngészőben fehér/üres a lap:** a `ProcurementPage` outer div-jének adjunk explicit `w-full`-t:

```tsx
// VOLT:
<div className="px-7 py-6 max-w-[1400px] mx-auto">

// LEGYEN:
<div className="w-full px-7 py-6 max-w-[1400px] mx-auto">
```

---

## Definition of Done

- [ ] `MovementsPage` fogad `embedded?: boolean` propot
- [ ] `embedded=true` esetén: nincs külső `px-7 py-6`, nincsenek summary cardok
- [ ] `InventoryPage` `<MovementsTab embedded />` használ
- [ ] `App.test.tsx`: warehouse route tesztek hozzáadva (procurement, movements)
- [ ] `ProcurementPage` renderel a `/w/warehouse/procurement` route-on (böngészőben ellenőrizve)
- [ ] `pnpm test` → mind pass
- [ ] `pnpm build` → 0 error
- [ ] `pnpm lint` → 0 új hiba

## Érintett fájlok

- `src/pages/warehouse/MovementsPage.tsx` — embedded prop
- `src/pages/InventoryPage.tsx` — embedded átadás
- `src/__tests__/App.test.tsx` — warehouse route tesztek
- `src/pages/ProcurementPage.tsx` — esetleg `w-full` fix
