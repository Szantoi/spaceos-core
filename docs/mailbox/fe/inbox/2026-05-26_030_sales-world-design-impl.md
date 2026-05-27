---
id: MSG-FE-030
from: root
to: fe
type: task
priority: high
status: READ
created: 2026-05-26
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# FE-030 — Értékesítés (Sales world) teljes design implementáció

## Kontextus

A design referencia bundle-ből (`page-sales.jsx`) a Sales world-höz tartozó három komponens
(`SalesDashboard`, `SalesQuotes`, `SalesCustomers`) teljes és maradéktalan implementálása szükséges.
A jelenlegi `SalesPage.tsx` belső tab-alapú navigációt használ, ami eltér a design architektúrától —
és több vizuális/funkcionális rész is hiányzik.

## Design referencia

A teljes design source a következő komponenseket tartalmazza:

### `SalesDashboard({ onScreen })`

```jsx
// Wrapper: <div className="px-7 py-6 space-y-5">
// 4 KPI kártya: md:grid-cols-4
// Pipeline funnel: grid-cols-5 + SVG vizualizáció
// Lejáró ajánlatok kártya (col-span-2) + Top ügyfelek kártya (col-span-1)
// onScreen("quotes") → ajánlatok navigáció
// onScreen("orders") → rendelések navigáció
```

### `SalesQuotes`

```jsx
// Wrapper: <div className="px-7 py-6 space-y-4">
// Filter bar: all / draft / sent / approved / expired + REJECTED FILTER IS REQUIRED
// "Új ajánlat" gomb a filter sorban (jobb oldalon, indigo-600)
// Quotes tábla: 8 oszlop, státusz pill dot-tal
```

**Hiányzó `rejected` filter a jelenlegi kódban:**
```tsx
// Hozzáadandó a FILTER_KEYS tömbhöz:
{ key: 'rejected', label: 'Elutasítva' }
// és QUOTE_TONE-ban már létezik a rejected kulcs (bg-rose-50 / text-rose-700)
```

### `SalesCustomers`

```jsx
// Wrapper: <div className="px-7 py-6">
// Keresés input + "Új ügyfél" gomb (indigo-600)
// 3 oszlopos kártyarács
// Kártyán: avatar initials (indigo-100), kontakt adatok, nyitott rendelések + LTV
```

## Feladatok

### 1. SalesPage.tsx refaktor — screen-alapú navigáció

**Jelenlegi (helytelen):**
```tsx
// SalesPage belső tab state-tel dolgozik
const [tab, setTab] = useState<SalesTab>('pipeline')
// onTab={setTab} prop
```

**Elvárás (design szerint):**
```tsx
// Mint ProductionWorldPage — useParams-ból olvasni a screen-t
// src/App.tsx már tartalmazza: <Route path="/w/sales/:screen" ...>
// A SalesPage-t átalakítani úgy, hogy a WorldShell onScreen callback-je
// URL navigációt váltson ki (/w/sales/:screen), és a screen param
// alapján renderelődjön a megfelelő sub-komponens

function SalesWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = screen ?? 'dash'

  function renderContent() {
    if (currentScreen === 'dash')      return <SalesDashboard onScreen={(s) => navigate(`/w/sales/${s}`)} />
    if (currentScreen === 'orders')    return <SalesOrders />      // lásd 2. pont
    if (currentScreen === 'quotes')    return <SalesQuotes />
    if (currentScreen === 'customers') return <SalesCustomers />
    return <SalesDashboard onScreen={(s) => navigate(`/w/sales/${s}`)} />
  }

  return (
    <WorldShell worldKey="sales" screen={currentScreen}
      onScreen={(key) => navigate(`/w/sales/${key}`)}
      onHome={() => navigate('/')}>
      {renderContent()}
    </WorldShell>
  )
}
```

**App.tsx frissítés:** A sales route-okat cseréld ki `SalesWorldPage`-re (a mintája `ProductionWorldPage`):
```tsx
<Route path="/w/sales" element={<RequireAuth><SalesWorldPage /></RequireAuth>} />
<Route path="/w/sales/:screen" element={<RequireAuth><SalesWorldPage /></RequireAuth>} />
```

### 2. `orders` screen — Sales world Rendelések

A design-ban a Sales worldnek van egy `orders` screenje (a WorldShell sidebar mutatja).
Ez nem ugyanaz mint a globális `/w/orders` — ez a Sales world kontextusában megjelenő rendelés lista.

**Opció:** `SalesOrders` komponens a `SalesPage.tsx`-ben — egyszerű rendelés lista (az `OrdersPage` tartalmát beágyazva, vagy az `ORDERS` mock adatból egy kisebb verzió). A lényeg: a WorldShell sidebar "Rendelések" menüpontja ne vezessen sehova (fehér oldal), hanem jelenítsen meg tartalmat.

### 3. `SalesDashboard` padding + prop

- Wrap: `<div className="px-7 py-6 space-y-5">` (jelenleg nincs saját padding, a SalesPage adja)
- Prop: `onScreen` (nem `onTab`)
- Az "Ajánlatok →" link: `onScreen("quotes")`

### 4. `SalesQuotes` — rejected filter + saját wrapper + "Új ajánlat" gomb

```tsx
// Filter opciók (a design szerint):
const FILTER_KEYS = [
  { key: 'all',      label: 'Összes' },
  { key: 'draft',    label: 'Vázlat' },
  { key: 'sent',     label: 'Kiküldve' },
  { key: 'approved', label: 'Elfogadva' },
  { key: 'rejected', label: 'Elutasítva' },   // ← HIÁNYZOTT
  { key: 'expired',  label: 'Lejárt' },
]

// Wrapper:
<div className="px-7 py-6 space-y-4">

// "Új ajánlat" gomb a filter sorban (jobb oldalra igazítva):
<span className="flex-1" />
<button className="h-8 px-3 bg-indigo-600 text-white text-[11.5px] font-medium rounded-lg hover:bg-indigo-700 inline-flex items-center gap-1.5">
  <Icon name="plus" size={12} />Új ajánlat
</button>
```

### 5. `SalesCustomers` — saját wrapper

```tsx
// Wrapper:
<div className="px-7 py-6">
```

### 6. `SalesPage` fejléc eltávolítása

A jelenlegi `SalesPage` wrapper (`px-7 py-5 max-w-[1600px]`) és a belső tab bar törlendő — a sub-komponensek saját paddingot kapnak, a navigációt a WorldShell végzi.

## Definition of Done

- [ ] `SalesWorldPage` (vagy refaktorált `SalesPage`) screen-alapú navigációval működik
- [ ] `/w/sales`, `/w/sales/dash`, `/w/sales/quotes`, `/w/sales/customers`, `/w/sales/orders` mind rendesen navigálható
- [ ] `SalesDashboard` "Ajánlatok →" link a quotes screenre navigál (URL-ben is változik)
- [ ] `SalesQuotes` tartalmaz `rejected` filtert
- [ ] `SalesQuotes` tartalmaz "Új ajánlat" gombot a filter sorban
- [ ] Minden sub-komponensnek saját `px-7 py-6` paddingja van
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind pass (meglévő tesztek nem törnek)
- [ ] `pnpm lint` → 0 error

## Fájlok érintve

- `src/pages/SalesPage.tsx` — teljes refaktor
- `src/App.tsx` — sales route-ok frissítése (SalesWorldPage minta)
- `src/__tests__/` — ha van SalesPage teszt, frissíteni

## Megjegyzés

Az `src/mocks/worlds.ts`-ben a `QUOTES`, `QUOTE_TONE`, `CUSTOMERS` adatok már helyesek (megegyeznek a design referenciával). Adat módosítás nem szükséges.
