---
id: MSG-FE-032
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

# FE-032 — Raktár (Warehouse world) teljes design implementáció

## Kontextus

A Raktár world (`/w/warehouse`) routing hiányos — a `WarehouseWorldPage` csak a `movements`
screent kezeli explicit módon, a többi screen (`dash`, `inventory`, `procurement`) hiányzik.
Emellett az `/w/procurement` orphaned standalone route-ot el kell távolítani.

A komponensek (`InventoryPage`, `ProcurementPage`, `MovementsPage`) tartalmukban helyesek,
csak a routing szorul javításra.

---

## 1. `App.tsx` — `WarehouseWorldPage` javítása

### Jelenlegi (HIBÁS):
```tsx
function WarehouseWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = screen ?? 'dash'

  function renderContent() {
    if (currentScreen === 'movements') return <MovementsPage />
    return <InventoryPage />
  }

  return (
    <WorldShell
      worldKey="warehouse"
      screen={currentScreen}
      onScreen={(key) => navigate(`/w/warehouse/${key}`)}
      onHome={() => navigate('/')}
    >
      {renderContent()}
    </WorldShell>
  )
}
```

### Elvárás (HELYES — SalesWorldPage/DesignWorldPage mintájára):
```tsx
function WarehouseWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = screen ?? 'dash'

  function renderContent() {
    if (currentScreen === 'dash')        return <InventoryPage />
    if (currentScreen === 'inventory')   return <InventoryPage />
    if (currentScreen === 'procurement') return <ProcurementPage />
    if (currentScreen === 'movements')   return <MovementsPage />
    return <InventoryPage />
  }

  return (
    <WorldShell
      worldKey="warehouse"
      screen={currentScreen}
      onScreen={(key) => navigate(`/w/warehouse/${key}`)}
      onHome={() => navigate('/')}
    >
      <div key={currentScreen} className="contents">{renderContent()}</div>
    </WorldShell>
  )
}
```

**Változások:**
- `dash` és `inventory` → `InventoryPage`
- `procurement` → `ProcurementPage`
- `movements` → `MovementsPage`
- `<div key={currentScreen} className="contents">` wrapper hozzáadva (force remount)

---

## 2. `App.tsx` — orphaned `/w/procurement` route törlése

Az alábbi route-ot **törlni kell** (a procurement most már `/w/warehouse/procurement`-on él):

```tsx
// TÖRLENDŐ:
<Route path="/w/procurement" element={
  <RequireAuth>
    <WorldPage worldKey="warehouse"><ProcurementPage /></WorldPage>
  </RequireAuth>
} />
```

---

## 3. Ellenőrzés

A következő URL-ek mindegyike helyesen betölt és a WorldShell sidebar aktív menüpontja
az aktuális screen-re mutat:

| URL | Megjelenő komponens |
|-----|---------------------|
| `/w/warehouse` | `InventoryPage` (dash default) |
| `/w/warehouse/dash` | `InventoryPage` |
| `/w/warehouse/inventory` | `InventoryPage` |
| `/w/warehouse/procurement` | `ProcurementPage` |
| `/w/warehouse/movements` | `MovementsPage` |
| `/w/procurement` | 404 / redirect (nincs ilyen route) |

---

## Definition of Done

- [ ] `WarehouseWorldPage.renderContent` kezeli mind a 4 screent: `dash`, `inventory`, `procurement`, `movements`
- [ ] `key={currentScreen}` wrapper div megvan
- [ ] `/w/procurement` orphaned route eltávolítva az `App.tsx`-ből
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind pass
- [ ] `pnpm lint` → 0 új hiba

## Fájlok érintve

- `src/App.tsx` — `WarehouseWorldPage` javítása + orphaned route törlése

## Megjegyzés

Az `InventoryPage`, `ProcurementPage`, `MovementsPage` komponensek tartalmukban helyesek,
nem szükséges módosítani őket. Ez kizárólag routing-javítás.
