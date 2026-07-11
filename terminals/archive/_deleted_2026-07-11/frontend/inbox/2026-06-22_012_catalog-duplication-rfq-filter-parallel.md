---
id: MSG-FRONTEND-012
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_1521_consensus.md
created: 2026-06-22
content_hash: 93da7a2e25875d06264440edbb010d9f8a79a1010097661770f5bcefc512be50
---

# Frontend Parallel Track — Katalógus Duplikálás + RFQ Filter (Sprint 1-2)

## Összefoglaló

Implementálj **2 párhuzamos frontend feature-t** mock-first megközelítéssel, backend dependency nélkül:

1. **TOP 2 — Katalógus duplikálás + inline szerkesztés**
2. **TOP 3 — Beszállítói RFQ filter/search**

Mindkét feature újrahasznosítható komponenseket épít (inline edit, filter pattern) későbbi horizontális skálázásra.

## Konsenzus Dokumentum

**Teljes spec:** `/opt/spaceos/docs/planning/queue/2026-06-22_1521_consensus.md`

Olvasd el részletesen a következőket:
- Frontend komponens struktúra (sor 63-127)
- State management pattern (sor 78-156)
- UX features és keyboard shortcuts (sor 104-160)
- Kockázatkezelési mátrix (sor 188-197)
- Implementációs ütemterv (sor 199-207)

## TOP 2 — Katalógus Duplikálás

### Scope (Sprint 1)

**Új komponensek:**
```
src/pages/catalog/
├── components/
│   ├── ProductRow.jsx (új)
│   ├── InlineEditCell.jsx (új, reusable!)
│   ├── RowActionsMenu.jsx (új: ⋯ dropdown)
│   └── DuplicateToast.jsx (optimista feedback)
└── hooks/
    └── useProductMutations.js (duplicate/update logic)
```

**State management (app-store.jsx):**
```javascript
catalog: {
  products: [...],
  editingCell: { productId, field } | null,
  draftValue: string,
  tempProducts: [] // Optimista duplikáció, "TEMP-" prefix ID
}

duplicateProduct(id, overrides) {
  const temp = { ...findProduct(id), id: `TEMP-${uuid()}`, ...overrides }
  this.tempProducts.push(temp)

  // localStorage mock (MVP)
  localStorage.setItem('catalog', JSON.stringify([...this.products, temp]))

  // Backend ready (Sprint 3+):
  // api.duplicate(id).then(real => { ... })
}
```

**UX Features (kötelező Sprint 1):**
- ✅ Keyboard shortcuts: `Cmd+D` duplicate, `Enter` save, `Esc` cancel
- ✅ Smart duplicate: `suggestNextVariant()` (pl. "60x200" → "80x200")
- ✅ Auto-focus: Duplicate után új sor név cellája szerkesztési módba
- ✅ Debounce save: **500ms** (nem 300ms!)
- ✅ Mobil fallback: <768px modal, nem inline edit (44px touch target)

**Kockázat mitigáció:**
- SKU collision: `${original}-COPY-${timestamp}` suffix
- Race condition: disable input save közben
- Safari private mode: `try-catch` wrapper, fallback sessionStorage

## TOP 3 — RFQ Filter/Search

### Scope (Sprint 1)

**Új komponensek:**
```
src/pages/supplier-portal/
├── RfqListView.jsx (main)
├── components/
│   ├── RfqFilterBar.jsx (sticky header)
│   │   ├── StatusTabs.jsx (open|quoted|closed|all + badge)
│   │   └── SearchInput.jsx (debounced 300ms)
│   ├── RfqCard.jsx (compact, mobil-optimalizált)
│   └── EmptyState.jsx (illusztráció + CTA)
└── hooks/
    ├── useRfqFilters.js (filter + search logic)
    └── useQueryParams.js (URL sync!)
```

**State management (custom hook):**
```javascript
// useRfqFilters.js
const useRfqFilters = () => {
  const [filters, setFilters] = useQueryParams({
    status: 'open',
    q: ''
  })

  const filtered = useMemo(() => {
    return sim.quotes
      .filter(rfq => filters.status === 'all' || rfq.status === filters.status)
      .filter(rfq =>
        rfq.rfqNumber.toLowerCase().includes(filters.q.toLowerCase()) ||
        rfq.items.some(item => item.name.toLowerCase().includes(filters.q))
      )
  }, [sim.quotes, filters])

  const counts = useMemo(() => ({
    open: sim.quotes.filter(q => q.status === 'open').length,
    quoted: sim.quotes.filter(q => q.status === 'quoted').length,
    closed: sim.quotes.filter(q => q.status === 'closed').length,
    all: sim.quotes.length
  }), [sim.quotes])

  return { filtered, filters, setFilters, counts }
}
```

**UX Features (kötelező Sprint 1):**
- ✅ URL-based filter state: `/supplier/rfqs?status=open&q=RFQ-2024` (shareable!)
- ✅ Search scope toggle: "○ RFQ számban ● Termékekben" inline switch
- ✅ Badge counts: Status tabs mellett `(12)` aktív RFQ számláló
- ✅ Debounced search: 300ms (jobb UX mint instant)

**NINCS első MVP-ben (túl komplex):**
- ❌ IndexedDB offline cache → Sprint 3+
- ❌ Swipe gesture filter → extra library
- ❌ Batch actions → later

## Backend API (Sprint 3+ — MOCK MOST!)

### TOP 2 — Éles fázis
```http
POST /api/v1/products/{id}/duplicate
Body: { "overrides": { "sku": "string", "name": "string" } }
Response: 201 { id, sku, name, ...clonedFields }

PATCH /api/v1/products/{id}
Body: { "field": "price", "value": 15000 }
Response: 200 { id, field, value }
```

### TOP 3 — Éles fázis
```http
GET /api/v1/suppliers/rfqs?status=open&search=RFQ-2024&limit=100
Response: 200 {
  data: [...],
  meta: { total: 450, filtered: 23, page: 1 }
}
```

## Definition of Done (Sprint 1)

**TOP 2:**
- [ ] `InlineEditCell` reusable komponens (Storybook story)
- [ ] `duplicateProduct()` mock logic + localStorage
- [ ] `RowActionsMenu` dropdown (⋯ ikon + duplicate/delete/edit)
- [ ] `suggestNextVariant()` helper implementálva
- [ ] Keyboard shortcuts működnek (Cmd+D, Enter, Esc)
- [ ] Mobil responsive (<768px modal fallback)
- [ ] Manual test: hover → ... menu → duplicate → inline edit → save

**TOP 3:**
- [ ] `RfqFilterBar` sticky header (4 status tab + search input)
- [ ] `useRfqFilters` hook client-side filter + useMemo
- [ ] `useQueryParams` hook URL sync
- [ ] Badge counts minden tab mellett
- [ ] Search scope toggle működik
- [ ] EmptyState komponens (pl. "Nincs találat 'XYZ' keresésre")
- [ ] Manual test: status switch → badge update, search → URL sync, share link → state restored

**Közös:**
- [ ] 0 ESLint warning
- [ ] 0 console error production build-ben
- [ ] Komponensek izoláltan tesztelve (Vitest unit test)
- [ ] E2E smoke test (Playwright): duplicate product, filter RFQ

## Implementációs Megjegyzések

### Párhuzamos Munka
- **Két dev vagy szekvenciális?** Ha 1 dev: TOP 2 először (alacsonyabb kockázat), ha 2 dev: párhuzamos.
- **Merge conflict elkerülés:** Napi sync meeting, külön feature branch (`feature/catalog-duplicate`, `feature/rfq-filter`)

### Reusable Pattern Vízió
- `InlineEditCell` → átültethető Work Order listára, Material listára
- `useQueryParams` hook → filter state minden listánál
- `useRfqFilters` logika → template Work Order / Invoice filterhez

### Kockázatok (TOP priority)
| Kockázat | Mitigáció |
|----------|-----------|
| Inline edit race condition | 500ms debounce + disable input + timestamp conflict |
| Duplikált SKU collision | `sku-COPY-${timestamp}` + backend 409 Conflict (Sprint 3) |
| Search performance 500+ RFQ | `useMemo` + 300ms debounce, backend pagination Sprint 3+ |
| Mobil touch target | Min 44px height + <768px modal fallback |
| Safari localStorage | `try-catch` wrapper, fallback sessionStorage |

## Becsült Effort

**TOP 2:** 2-3 nap (1 dev)
**TOP 3:** 2-3 nap (1 dev)
**Párhuzamos (2 dev):** 2-3 nap (mindkettő kész)
**Szekvenciális (1 dev):** 4-6 nap (TOP 2 → TOP 3)

## Segítség

Ha blokkol valami:
- **Design kérdés:** Designer terminálnak eszkalálás
- **Backend spec tisztázás:** Conductor-nak kérdés (de Sprint 1-ben NEM kell backend!)
- **Reusable pattern döntés:** Architect konzultáció

Olvasd el a teljes konsenzust részletesen mielőtt kezded: `/opt/spaceos/docs/planning/queue/2026-06-22_1521_consensus.md`

**Let's build! 🚀**
