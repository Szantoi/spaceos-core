---
id: MSG-FE-031
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

# FE-031 — Tervezés (Design world) teljes design implementáció

## Kontextus

A design referencia bundle (`page-design.jsx`) alapján a Tervezés world teljes és maradéktalan
implementálása szükséges. A jelenlegi `DesignPage.tsx` belső tab-alapú navigációt használ —
ezt screen-alapú WorldShell navigációra kell cserélni (mint FE-030-ban a Sales worldnél),
és több komponens hiányzik vagy hiányos.

---

## 1. `DesignWorldPage` — screen-alapú navigáció (mint SalesWorldPage minta)

**Jelenlegi App.tsx:**
```tsx
<Route path="/w/design" element={<RequireAuth><WorldPage worldKey="design"><DesignPage /></WorldPage></RequireAuth>} />
```

**Elvárás:**
```tsx
// DesignWorldPage — useParams + useNavigate alapú, pontosan mint SalesWorldPage
export function DesignWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = screen ?? 'dash'

  function renderContent() {
    if (currentScreen === 'dash')     return <DesignDashboard onScreen={(s) => navigate(`/w/design/${s}`)} />
    if (currentScreen === 'editor')   return <TemplateEditor />
    if (currentScreen === 'generate') return <MaterialsGenerator />
    if (currentScreen === 'catalog')  return <DesignCatalog />
    return <DesignDashboard onScreen={(s) => navigate(`/w/design/${s}`)} />
  }

  return (
    <WorldShell worldKey="design" screen={currentScreen}
      onScreen={(key) => navigate(`/w/design/${key}`)}
      onHome={() => navigate('/')}>
      <div key={currentScreen} className="contents">{renderContent()}</div>
    </WorldShell>
  )
}
```

**App.tsx frissítés:**
```tsx
import { DesignWorldPage } from './pages/DesignPage'
// ...
<Route path="/w/design" element={<RequireAuth><DesignWorldPage /></RequireAuth>} />
<Route path="/w/design/:screen" element={<RequireAuth><DesignWorldPage /></RequireAuth>} />
```

---

## 2. `DesignDashboard` — HIÁNYZÓ komponens

A jelenlegi kódban nincs `DesignDashboard`. Ez a `/w/design/dash` alapértelmezett screen.

**Design szerint:**
```tsx
function DesignDashboard({ onScreen }: { onScreen: (s: string) => void }) {
  // Wrapper: <div className="px-7 py-6 space-y-6">

  // 4 KPI kártya (md:grid-cols-4):
  const stats = [
    { label: "Aktív sablonok",       value: 24,       delta: "+3 e hónapban" },
    { label: "Generált anyaglisták", value: 142,      delta: "+18 e héten" },
    { label: "Aktív projektek",      value: 2,        delta: "Doorstar, Bognár" },
    { label: "Sablon átlag rating",  value: "4.6 ★",  delta: "76 értékelés" },
  ]

  // Bal kártya (lg:col-span-2): "Népszerű sablonok"
  // - PARAM_TEMPLATES első 3 eleme
  // - Soronként: TemplateThumb(48px) + név/típus/verzió + uses szám + rating
  // - "Sablonok megnyitása →" link → onScreen("editor") [amber-700]

  // Jobb kártya (lg:col-span-1): "Aktív projektek"
  // - 2 projekt progress bar-ral:
  //   "Doorstar — 12 ajtó beállítás" · Belső ajtó · Tölgy · 7/12 kész · 58% amber
  //   "Bognár — Konyhabútor" · 14 alsó + 8 felső · 22/22 kész · 100% emerald
}
```

---

## 3. `TemplateEditor` hiányok

### 3a. Saját wrapper
A jelenlegi `TemplateEditor` `space-y-4`-et használ padding nélkül.
Design szerint: `<div className="px-7 py-6">` legyen a wrapper.

### 3b. Advanced mód — CNC preview panel (HIÁNYZIK)
A `PartDetail` komponensben (vagy inline a detail card-ban) advanced módban meg kell jeleníteni
egy dark CNC preview box-ot:

```tsx
{mode === 'advanced' && (
  <div className="mt-4 p-3 rounded-lg bg-stone-900 text-emerald-300 font-mono text-[11px] leading-relaxed overflow-x-auto">
    <div className="text-stone-400 mb-1">// CNC deriválás preview</div>
    <div>PART {part.name.toUpperCase()}</div>
    <div>  MATERIAL = {resolved.rMat}</div>
    <div>  DIM      = {resolved.rW} x {resolved.rH} x {resolved.rT}</div>
    <div>  QUANTITY = {resolved.rQty}</div>
    <div>  EDGES    = TOP, BOTTOM, LEFT, RIGHT</div>
    <div className="text-stone-400 mt-1">// → Holzma optimizer</div>
  </div>
)}
```

### 3c. Advanced mód constraint — formula megjelenítés
A constraint listában advanced módban a formula is látható legyen:
```tsx
{mode === 'advanced' && (
  <span className="ml-2 font-mono text-[10.5px] opacity-60">{c.expr}</span>
)}
```

---

## 4. `MaterialsGenerator` hiányok

### 4a. Saját wrapper
Design szerint: `<div className="px-7 py-6">` wrapper.

### 4b. Step 1 — Üres sablon kizárása
A design-ban `PARAM_TEMPLATES.filter(t => t.id !== "T-04")` — az üres sablon ne jelenjen meg a választóban.

### 4c. Step 1 → Step 2 közötti "Hozzárendelés rendeléshez" kártya (HIÁNYZIK)
A step 1 (paraméterek) alatt legyen egy második kártya:
```tsx
<Card className="p-5">
  <div className="text-[13px] font-semibold mb-2">Hozzárendelés rendeléshez</div>
  <div className="grid grid-cols-2 gap-3">
    <div>
      <div className="text-[10.5px] uppercase tracking-wide text-stone-500 font-medium mb-1">Rendelés</div>
      <select value={orderRef} onChange={(e) => setOrderRef(e.target.value)}
        className="w-full h-9 px-3 rounded-lg border border-stone-200 text-[12px] bg-white">
        <option>JT-2426-0184 — Bognár Bútor Kft.</option>
        <option>JT-2426-0182 — Doorstar Hungary Zrt.</option>
        <option>JT-2426-0180 — Hegyi Lakberendezés</option>
      </select>
    </div>
    <div>
      <div className="text-[10.5px] uppercase tracking-wide text-stone-500 font-medium mb-1">Mennyiség</div>
      <input type="number" defaultValue="1" min="1"
        className="w-full h-9 px-3 rounded-lg border border-stone-200 text-[12px] bg-white font-mono" />
    </div>
  </div>
</Card>
```
Az `orderRef` state legyen `useState('JT-2426-0184 — Bognár Bútor Kft.')` és `setOrderRef`-ként exportálva a step 1 kártyában.

### 4d. Step 2 — "Egyedi hozzáadása" gomb + extras state (HIÁNYZIK)
```tsx
// State:
const [extras, setExtras] = useState<ResolvedPart[]>([])
const allParts = [...resolved, ...extras]

// A step 2 kártya fejlécébe:
<button onClick={() => setExtras(p => [...p, {
  name: 'Egyedi alkatrész ' + (p.length + 1),
  mat: 'EG-3303-18', w: 400, h: 400, t: 18, qty: 1
}])}
  className="h-8 px-3 rounded-lg border border-stone-200 text-[11.5px] font-medium text-stone-700 hover:bg-stone-50 inline-flex items-center gap-1.5">
  <Icon name="plus" size={12} />Egyedi hozzáadása
</button>

// A táblában az extras soroknál "egyedi" badge:
{i >= resolved.length && (
  <span className="ml-2 px-1.5 py-0.5 text-[9.5px] uppercase rounded bg-stone-100 text-stone-600">egyedi</span>
)}
```

### 4e. Step 3 (elküldve) — bővítés
```tsx
// Cutting Plan ID megjelenítése:
<div className="text-[11px] text-stone-400 mt-1">
  Cutting Plan ID: <span className="font-mono">CP-184-{Math.random().toString(36).slice(2,5).toUpperCase()}</span>
</div>

// Második gomb:
<button className="h-9 px-4 bg-stone-900 text-white text-[12px] rounded-lg hover:bg-stone-800">
  Megnyitás Gyártás → Szabászat
</button>
```

---

## 5. `DesignCatalog` — wrapper + filter sorrend

```tsx
// Wrapper: <div className="px-7 py-6">

// Filter sorrend (design szerint: cats először, majd flex-1, majd "Új tétel"):
<div className="flex items-center gap-2 flex-wrap mb-4">
  {cats.map((c, i) => (
    <button key={c} onClick={() => setCat(i)} ...>{c}</button>
  ))}
  <span className="flex-1" />
  <button ...><Icon name="plus" size={12} />Új tétel</button>
</div>
```

---

## Definition of Done

- [ ] `DesignWorldPage` screen-alapú navigációval működik
- [ ] `/w/design`, `/w/design/dash`, `/w/design/editor`, `/w/design/generate`, `/w/design/catalog` mind navigálható
- [ ] `DesignDashboard` megjelenik KPI kártyákkal, Népszerű sablonokkal, Aktív projektekkel
- [ ] "Sablonok megnyitása →" link az `editor` screenre navigál (URL-ben is)
- [ ] `TemplateEditor` advanced módban CNC preview panel megjelenik
- [ ] `TemplateEditor` advanced módban constraint formula látható
- [ ] `MaterialsGenerator` step 1-ben van "Hozzárendelés rendeléshez" kártya
- [ ] `MaterialsGenerator` step 2-ben van "Egyedi hozzáadása" gomb és működik
- [ ] `MaterialsGenerator` step 3 tartalmazza a Cutting Plan ID-t és a második gombot
- [ ] Minden sub-komponensnek saját `px-7 py-6` paddingja van
- [ ] `App.tsx` frissítve: `DesignWorldPage` import + route
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind pass
- [ ] `pnpm lint` → 0 error

## Fájlok érintve

- `src/pages/DesignPage.tsx` — teljes refaktor
- `src/App.tsx` — design route-ok frissítése

## Megjegyzés

A `PARAM_TEMPLATES`, `CATALOG_LOOKUP` mock adatok a `src/mocks/worlds.ts`-ben helyesek, nem kell módosítani.
A régi `TEMPLATES` import az `extra2`-ből eltávolítható, ha a `DesignDashboard` átveszi a PARAM_TEMPLATES-t.
