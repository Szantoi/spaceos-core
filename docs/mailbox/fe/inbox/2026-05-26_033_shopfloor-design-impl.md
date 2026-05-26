---
id: MSG-FE-033
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

# FE-033 — Üzem (ShopFloor) design igazítás + orphaned route cleanup

## Kontextus

A `ShopFloorPage.tsx` tartalmában helyes, de a design referenciától eltér néhány apró
ponton. Emellett az `App.tsx`-ben 3 orphaned route maradt (`/w/orders`, `/w/workflow`,
`/w/analytics`), amelyek `WorldPage` wrapperrel futnak, de ezek a screen-ek már a
`ProductionWorldPage`-ben kezelve vannak `/w/production/:screen` alatt.

---

## 1. `ShopFloorPage.tsx` — design igazítás

### 1a. Háttér és header színek
```tsx
// VOLT:
<div className="min-h-screen bg-[#0a0f1a] text-stone-100 flex flex-col">
  <header className="bg-stone-900 border-b border-stone-800 ...">

// LEGYEN:
<div className="min-h-screen bg-stone-900 text-stone-100 flex flex-col">
  <header className="bg-stone-800 border-b border-stone-700 ...">
```

### 1b. "Vissza a portálra" gomb — HIÁNYZIK
A header-be kell, mindig látható (nem csak bejelentkezve):
```tsx
// Import a fájl tetején:
import { useNavigate } from 'react-router-dom'

// ShopFloorPage()-ban:
const navigate = useNavigate()

// Header jobb oldal (a meglévő Kijelentkezés gomb mellett):
<div className="flex items-center gap-2">
  {operator && (
    <button onClick={handleLogout}
      className="h-9 px-3 rounded-lg bg-stone-700 hover:bg-stone-600 text-[11.5px] inline-flex items-center gap-1.5">
      <Icon name="logout" size={13} />Kijelentkezés
    </button>
  )}
  <button onClick={() => navigate('/')}
    className="h-9 px-3 rounded-lg bg-stone-700 hover:bg-stone-600 text-[11.5px]">
    Vissza a portálra
  </button>
</div>
```

### 1c. PinStage cím: "Shopfloor" → "Bejelentkezés"
```tsx
// VOLT:
<h1 className="text-[28px] font-semibold tracking-tight">Shopfloor</h1>

// LEGYEN:
<h1 className="text-[28px] font-semibold tracking-tight">Bejelentkezés</h1>
```

### 1d. PIN törlés gomb: `C` → `Töröl`
```tsx
// VOLT:
<button onClick={...}
  className="h-16 rounded-xl bg-stone-800 hover:bg-stone-700 text-[16px] font-semibold text-stone-400 border border-stone-700">
  C
</button>

// LEGYEN:
<button onClick={...}
  className="h-16 rounded-xl bg-stone-800 hover:bg-stone-700 text-[12px] uppercase tracking-wide text-stone-400 border border-stone-700">
  Töröl
</button>
```

---

## 2. `App.tsx` — orphaned route-ok eltávolítása

A következő 3 route **törlendő** — ezek a screen-ek már a `ProductionWorldPage`-ben
kezelve vannak (`/w/production/workflow`, `/w/production/analytics`), az `orders`
screen pedig a Sales world alatt van (`/w/sales/orders`):

```tsx
// TÖRLENDŐ (mindhármat):
<Route path="/w/orders" element={
  <RequireAuth>
    <WorldPage worldKey="production"><OrdersPage /></WorldPage>
  </RequireAuth>
} />
<Route path="/w/workflow" element={
  <RequireAuth>
    <WorldPage worldKey="production"><WorkflowPage /></WorldPage>
  </RequireAuth>
} />
<Route path="/w/analytics" element={
  <RequireAuth>
    <WorldPage worldKey="production"><AnalyticsPage /></WorldPage>
  </RequireAuth>
} />
```

> **Megjegyzés:** Az `OrdersPage` importja is eltávolítható, ha más helyen nincs
> használva. Ellenőrizd grep-pel mielőtt törölnéd az importot.

---

## Definition of Done

- [ ] `ShopFloorPage.tsx` háttér: `bg-[#0a0f1a]` → `bg-stone-900`
- [ ] `ShopFloorPage.tsx` header: `bg-stone-800 border-stone-700`
- [ ] "Vissza a portálra" gomb megjelenik a headerben, `navigate('/')` működik
- [ ] PinStage cím: "Bejelentkezés"
- [ ] PIN törlés gomb: "Töröl" felirat
- [ ] `/w/orders`, `/w/workflow`, `/w/analytics` orphaned route-ok törölve App.tsx-ből
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind pass
- [ ] `pnpm lint` → 0 új hiba

## Fájlok érintve

- `src/pages/ShopFloorPage.tsx` — kis igazítások
- `src/App.tsx` — 3 orphaned route törlése

## Megjegyzés

A ShopFloor funkcionálisan helyes, ez kizárólag vizuális/UX igazítás a design
referencia alapján.
