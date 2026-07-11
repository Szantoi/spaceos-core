# ADR-047: JoineryTech Portal UI Enhancements

**Status:** IMPLEMENTÁCIÓRA KÉSZ
**Date:** 2026-06-22
**Author:** Architect Terminal
**Stakeholders:** Frontend, Designer, Conductor

---

## Kontextus

A JoineryTech prototípusból 6 planning idea érkezett, amelyeket éles portál funkcióvá kell alakítani. Ezek közül 3 feature-t kell megtervezni részletes implementációs specifikációval:

1. **Assembly Filter Panel** - Részegység szűrő kolapszálható kategóriákkal
2. **KPI Cards** - Beszállítói megbízhatóság mutatók élő dashboard
3. **Inline Editing** - Táblázat sorok helyben szerkesztése

## Döntés

### Architektúrális elvek

1. **Meglévő minták követése** - A portálban már van `KpiCard` komponens (`src/components/ui/KpiCard.tsx`), ezt használjuk
2. **Lokális state preferálása** - Filter state lokális React state, nem globális zustand store
3. **Reusable komponensek** - Assembly filter újrahasználható `FilterPanel` komponensként
4. **Backend API-függetlenség** - MVP-hez mock data elegendő, API integráció későbbi fázis

### Implementációs sorrend

| Prioritás | Feature | Komplexitás | Indoklás |
|---|---|---|---|
| **1. (MAGAS)** | Assembly Filter Panel | Közepes | Azonnali UX javulás, gyakran használt funkció |
| **2. (KÖZEPES)** | KPI Cards | Alacsony | Meglévő KpiCard komponens kiterjesztése |
| **3. (ALACSONYABB)** | Inline Editing | Magas | Permission check, backend API szükséges |

---

## Feature 1: Assembly Filter Panel

### Elhelyezés
- **Oldal:** `ProductionPage.tsx` → Cutting tab → bal oldali plan lista fölé
- **Alternatíva (jövőben):** Külön `AssemblyPage.tsx` ha a részegység-kezelés önálló világ lesz

### Komponens struktúra

```
src/components/filters/
├── FilterPanel.tsx          # Reusable filter container
├── FilterCategory.tsx       # Collapse-able category header + checkboxes
└── FilterCheckbox.tsx       # Single checkbox item
```

### Props interface

```typescript
// FilterPanel.tsx
interface FilterPanelProps {
  categories: FilterCategory[];
  selected: Record<string, string[]>;  // categoryKey -> selected values
  onChange: (categoryKey: string, values: string[]) => void;
  collapsed?: boolean;
  onCollapsedChange?: (collapsed: boolean) => void;
}

interface FilterCategory {
  key: string;
  label: string;
  options: Array<{ value: string; label: string; count?: number }>;
}
```

### State management

```typescript
// Lokális state ProductionPage-ben
const [filters, setFilters] = useState<Record<string, string[]>>({
  keretrendszer: [],
  felulet: [],
  vasalat: [],
  egyeb: [],
});

// Filtered plans
const filteredPlans = useMemo(() => {
  if (Object.values(filters).every(arr => arr.length === 0)) {
    return displayPlans;
  }
  return displayPlans.filter(plan => matchesFilters(plan, filters));
}, [displayPlans, filters]);
```

### Kategóriák (hardcoded MVP-hez)

```typescript
const ASSEMBLY_CATEGORIES: FilterCategory[] = [
  {
    key: 'keretrendszer',
    label: 'Keretrendszer',
    options: [
      { value: 'alu', label: 'Alumínium', count: 12 },
      { value: 'fa', label: 'Fa', count: 8 },
      { value: 'mdf', label: 'MDF', count: 15 },
    ],
  },
  {
    key: 'felulet',
    label: 'Felület',
    options: [
      { value: 'natúr', label: 'Natúr', count: 6 },
      { value: 'festett', label: 'Festett', count: 14 },
      { value: 'fóliázott', label: 'Fóliázott', count: 5 },
    ],
  },
  // ... vasalat, egyeb
];
```

### Tailwind styling

```tsx
// Collapse animation
<div className={`overflow-hidden transition-all duration-200 ${
  collapsed ? 'max-h-0' : 'max-h-96'
}`}>
  {/* content */}
</div>

// Checkbox styling
<label className="flex items-center gap-2 py-1.5 px-2 rounded hover:bg-stone-50 cursor-pointer">
  <input type="checkbox" className="w-4 h-4 rounded border-stone-300 text-teal-600 focus:ring-teal-500" />
  <span className="text-[12px] text-stone-700">{option.label}</span>
  {option.count !== undefined && (
    <span className="ml-auto text-[10.5px] text-stone-400 tabular-nums">{option.count}</span>
  )}
</label>
```

### Fájlok

| Művelet | Fájl |
|---|---|
| CREATE | `src/components/filters/FilterPanel.tsx` |
| CREATE | `src/components/filters/FilterCategory.tsx` |
| CREATE | `src/components/filters/index.ts` |
| MODIFY | `src/pages/ProductionPage.tsx` - FilterPanel import és használat |

---

## Feature 2: KPI Cards (Beszállítói megbízhatóság)

### Elhelyezés
- **Oldal:** `ProcurementPage.tsx` → orders tab teteje (aktív PO lista fölé)
- **Alternatíva:** `DashboardPage.tsx` ha cross-domain overview kell

### Komponens használat

A portálban már létezik `KpiCard` komponens (`src/components/ui/KpiCard.tsx`) ezekkel a props-okkal:
- `label` / `title` - KPI neve
- `value` - Fő érték
- `unit` - Mértékegység
- `delta` - Változás százalékban (zöld/piros)
- `spark` - Sparkline adatok (number[])
- `breakdowns` - Részletező kártyák

### KPI Grid implementáció

```tsx
// ProcurementPage.tsx
import { KpiCard } from '../components/ui'

function ProcurementKPIs() {
  // Mock data - MVP-hez elegendő
  const kpis = [
    {
      label: 'On-time szállítás',
      value: '94',
      unit: '%',
      delta: 3,
      spark: [88, 91, 89, 94, 92, 94],
      breakdowns: [
        { label: 'Q1', value: '91%' },
        { label: 'Q2', value: '94%' },
      ],
    },
    {
      label: 'Ár-stabilitás',
      value: '2.3',
      unit: '%',
      delta: -1.2,  // negatív = jó (kisebb árváltozás)
      spark: [4.1, 3.8, 2.9, 2.3],
    },
    {
      label: 'Minőségi értékelés',
      value: '4.6',
      unit: '★',
      delta: 0.2,
      spark: [4.2, 4.4, 4.5, 4.6],
    },
    {
      label: 'Aktív szállítók',
      value: '12',
      unit: '',
      delta: 0,
      breakdowns: [
        { label: 'Lap', value: '5' },
        { label: 'Vasalat', value: '4' },
        { label: 'Egyéb', value: '3' },
      ],
    },
  ];

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-5">
      {kpis.map((k) => (
        <KpiCard key={k.label} {...k} />
      ))}
    </div>
  );
}
```

### Frissítési logika (jövőbeli)

```typescript
// useSupplierKPIs.ts hook - későbbi API integráció
function useSupplierKPIs(refreshInterval = 5 * 60 * 1000) {
  const [kpis, setKpis] = useState<SupplierKPI[]>([]);

  useEffect(() => {
    const fetch = async () => {
      const data = await api.get('/api/procurement/kpis');
      setKpis(data);
    };

    fetch();
    const interval = setInterval(fetch, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshInterval]);

  return kpis;
}
```

### Szín indikátorok

| Metrika | Zöld | Sárga | Piros |
|---|---|---|---|
| On-time % | ≥95% | 85-94% | <85% |
| Ár-stabilitás | ≤3% | 3-5% | >5% |
| Minőség | ≥4.5 | 3.5-4.4 | <3.5 |

### Fájlok

| Művelet | Fájl |
|---|---|
| MODIFY | `src/pages/ProcurementPage.tsx` - KPI grid hozzáadása |
| CREATE (opcionális) | `src/hooks/useSupplierKPIs.ts` - API hook |

---

## Feature 3: Inline Editing

### Elhelyezés
- **Oldal:** `MasterdataPage.tsx` → products screen → termék táblázat

### Komponens struktúra

```
src/components/table/
├── EditableCell.tsx         # Inline editable cell wrapper
├── EditableRow.tsx          # Row with edit mode toggle
└── InlineEditContext.tsx    # Edit state management
```

### EditableCell Props

```typescript
interface EditableCellProps {
  value: string | number;
  field: string;
  type: 'text' | 'number' | 'currency';
  isEditing: boolean;
  onChange: (value: string | number) => void;
  onSave: () => void;
  onCancel: () => void;
}
```

### Row-level edit mode

```tsx
function EditableProductRow({ product, onUpdate }: Props) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValues, setEditValues] = useState({
    price: product.price,
    discount: product.discount,
  });

  const handleSave = async () => {
    await onUpdate(product.id, editValues);
    setIsEditing(false);
  };

  return (
    <tr className={isEditing ? 'bg-amber-50/50' : 'hover:bg-stone-50/40'}>
      {/* ... other cells ... */}

      <td className="px-4 py-2.5">
        {isEditing ? (
          <input
            type="number"
            value={editValues.price}
            onChange={(e) => setEditValues(v => ({ ...v, price: Number(e.target.value) }))}
            className="w-full h-8 px-2 rounded border border-amber-300 text-[12px] font-mono focus:outline-none focus:border-amber-500"
          />
        ) : (
          <span className="font-mono">{huf(product.price)}</span>
        )}
      </td>

      <td className="px-4 py-2.5">
        {isEditing ? (
          <div className="flex items-center gap-2">
            <button onClick={handleSave} className="h-7 px-2 rounded bg-emerald-600 text-white text-[11px]">
              <Icon name="check" size={12} /> Mentés
            </button>
            <button onClick={() => setIsEditing(false)} className="h-7 px-2 rounded border border-stone-200 text-[11px]">
              Mégse
            </button>
          </div>
        ) : (
          <button
            onClick={() => setIsEditing(true)}
            className="opacity-0 group-hover:opacity-100 h-7 px-2 rounded border border-stone-200 text-[11px] text-stone-600 hover:bg-stone-50"
          >
            <Icon name="edit" size={12} /> Szerk.
          </button>
        )}
      </td>
    </tr>
  );
}
```

### Permission check

```typescript
// useAuth hook-ból
const { hasPermission } = useAuth();
const canEdit = hasPermission('catalog.edit') || hasPermission('masterdata.manage');

// Vagy role-based
const canEdit = user?.roles?.includes('catalog_editor');
```

### Backend API szükséglet

```typescript
// PUT /api/masterdata/products/{id}
interface UpdateProductRequest {
  price?: number;
  discount?: number;
  // más szerkeszthető mezők
}

// Procurement API-val konzisztens pattern
await fetch(`${API_BASE.abstractions}/api/modules/products/${id}`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({ price, discount }),
});
```

### Fájlok

| Művelet | Fájl |
|---|---|
| CREATE | `src/components/table/EditableCell.tsx` |
| CREATE | `src/components/table/EditableRow.tsx` |
| MODIFY | `src/pages/MasterdataPage.tsx` - inline edit integráció |
| MODIFY (backend) | Abstractions API - PUT endpoint |

---

## Alternatívák megfontolva

### Filter Panel - State management

| Opció | Előny | Hátrány | Döntés |
|---|---|---|---|
| **Lokális useState** | Egyszerű, gyors | Nem perzisztens | ✅ VÁLASZTVA |
| Zustand store | Perzisztencia, megosztott | Over-engineering | ❌ |
| URL query params | Shareable links | Komplex sync | ❌ |

### KPI Cards - Data source

| Opció | Előny | Hátrány | Döntés |
|---|---|---|---|
| **Mock data (MVP)** | Gyors implementáció | Nem valós adat | ✅ MVP |
| Backend API | Valós idejű | Backend munka szükséges | Fázis 2 |
| localStorage sim | Offline-ready | Nincs backend | ❌ |

### Inline Editing - UX pattern

| Opció | Előny | Hátrány | Döntés |
|---|---|---|---|
| **Row edit button** | Explicit, discoverable | Extra kattintás | ✅ VÁLASZTVA |
| Double-click cell | Gyors | Nem intuitív | ❌ |
| Slide-over modal | Sok mező esetén jobb | Context switch | ❌ |

---

## Acceptance Criteria

### Feature 1: Assembly Filter Panel
- [ ] FilterPanel komponens újrahasználható
- [ ] Kategóriák collapse-elhetők
- [ ] "Összes" / "Egyik sem" gyorsválasztás
- [ ] Lista real-time frissül szűréskor
- [ ] Sticky header a filter panel

### Feature 2: KPI Cards
- [ ] 4 KPI card megjelenik ProcurementPage-en
- [ ] Sparkline-ok renderelődnek
- [ ] Delta zöld/piros színezés
- [ ] Breakdowns kinyithatók

### Feature 3: Inline Editing
- [ ] Edit gomb megjelenik hover-re
- [ ] Ár és kedvezmény szerkeszthető
- [ ] Mentés/Mégse gombok működnek
- [ ] Sárga háttér edit módban
- [ ] Permission check működik

---

## Referenciák

- Planning ideas: `docs/planning/ideas/2026-06-21_00*.md`
- Meglévő KpiCard: `frontend/joinerytech-portal/src/components/ui/KpiCard.tsx`
- DashboardPage KPI minta: `frontend/joinerytech-portal/src/pages/DashboardPage.tsx`
- MasterdataPage lista minta: `frontend/joinerytech-portal/src/pages/MasterdataPage.tsx`
