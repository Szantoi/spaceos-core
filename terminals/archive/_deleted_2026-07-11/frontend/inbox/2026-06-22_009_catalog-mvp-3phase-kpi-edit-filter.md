---
id: MSG-FRONTEND-009
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_1306_consensus.md
created: 2026-06-22
content_hash: d7756464e973776228292c77167e3d214805a5f1bae2eee98eaf31d5f9a9198a
---

# Catalog MVP — 3 Phase Implementation (KPI Dashboard → Inline Edit → Smart Filter)

## Context

A planning consensus kimondta a következő frontend prioritásokat a Catalog/Procurement world számára. Ez egy **pragmatikus, inkrementális MVP** stratégia localStorage alapon, opcionális backend API-kkal Phase 3-ban.

**Konsenzus forrás:** `/opt/spaceos/docs/planning/queue/2026-06-22_1306_consensus.md`

---

## Feladat összefoglalás

3 fázisban implementálandó Catalog world feature-ök:

### Phase 1: KPI Dashboard (TOP 3)
**Scope:** 2-3 óra
**Cél:** Pszichológiai horgony, zero dependency, azonnali érték

**Komponensek:**
```
frontend/src/components/catalog/
  ├─ KPIDashboard.jsx    (grid container, localStorage layout state)
  ├─ KPICard.jsx         (metric + trend display)
  └─ hooks/useKPICalculator.js  (memoized computations)
```

**Functionality:**
- Statikus grid layout (CSS Grid, nem drag-drop)
- localStorage-ban mentett sorrend (config modal, nem UI drag-drop)
- Trend tárolás: localStorage schema, de UI simplified (csak delta % megjelenítés)
- KPI-k: `inventory-value`, `active-skus`, `avg-price`, `low-stock`

**localStorage schema bővítés:**
```json
{
  "dashboardLayout": {
    "kpiOrder": ["inventory-value", "active-skus", "avg-price", "low-stock"],
    "trends": {
      "inventory-value": {"2026-06": 12400000, "2026-05": 11500000}
    }
  }
}
```

---

### Phase 2: Inline Editing (TOP 2)
**Scope:** 4-5 óra
**Cél:** Row-level state management pattern tanulás, előkészíti a filter komplexitását

**Komponensek:**
```
frontend/src/components/catalog/
  ├─ EditableCell.jsx       (double-click to edit, Esc/Enter handling)
  ├─ hooks/useEditLock.js   (localStorage conflict detection)
  └─ ConflictWarning.jsx    (banner: "Another tab is editing this row")
```

**State management:**
- `editingCell` + `tempValue` state pattern
- `storage` event listener + timestamp-based lock check

**Conflict detection logic:**
```tsx
useEffect(() => {
  const handleStorageChange = (e) => {
    if (e.key === 'editLocks') {
      const locks = JSON.parse(e.newValue);
      if (locks[currentRowId] && locks[currentRowId].tabId !== currentTabId) {
        setConflictWarning(true);
      }
    }
  };
  window.addEventListener('storage', handleStorageChange);
}, [currentRowId]);
```

**localStorage schema bővítés:**
```json
{
  "editLocks": {
    "product-abc123": {"timestamp": 1718886000, "tabId": "tab-xyz"}
  }
}
```

---

### Phase 3: RFQ Smart Filter (TOP 1)
**Scope:** 6-8 óra
**Cél:** Extendable architecture, nem full SQL DSL, de nem is hardcoded

**Komponensek:**
```
frontend/src/components/shared/
  ├─ SmartFilter.jsx       (generic filter container)
  ├─ FilterRow.jsx         (field select, operator dropdown, value input)
  ├─ FilterPresets.jsx     (saved queries dropdown)
  └─ hooks/useFilterState.js  (URL sync + localStorage cache)
```

**Architecture kompromisszum:**
- **Nem SQL DSL** (overengineering elkerülése)
- **De nem hardcoded** (tech debt elkerülése)
- **Megoldás:** Config-driven filter rendszer

**Config példa:**
```tsx
const RFQ_FILTER_CONFIG = {
  fields: [
    {id: 'vendor', label: 'Supplier', type: 'multiselect', options: vendors},
    {id: 'status', label: 'Status', type: 'multiselect', options: STATUSES},
    {id: 'createdAt', label: 'Date', type: 'daterange'}
  ],
  operators: {
    multiselect: ['IN', 'NOT IN'],
    daterange: ['BETWEEN', '>', '<']
  }
};
```

**Újrafelhasználhatóság:**
```tsx
// RFQ view
<SmartFilter config={RFQ_FILTER_CONFIG} data={rfqs} onFilter={setFiltered} />

// Work Orders view (later)
<SmartFilter config={WORK_ORDER_FILTER_CONFIG} data={orders} onFilter={setFiltered} />
```

**URL sync:**
- `useEffect` hook szinkronizálja URL params ↔ filter state
- Single source of truth: URL a master

**Presets:**
- localStorage-ban mentett gyakori szűrések
- "Last 30 days", "High-value RFQs" gyors gombok

**localStorage schema bővítés:**
```json
{
  "filterPresets": {
    "rfq-last-30d": {"vendors": [], "dateRange": {"start": "2026-05-23"}},
    "high-value": {"vendors": ["Kronospan"], "statuses": ["accepted"]}
  }
}
```

---

## Backend szükségletek

**Phase 1-2:** NINCS (localStorage elég)

**Phase 3 opcionális backend readiness:**
- `GET /api/rfqs?vendor=X&status=Y&date_from=Z` — query params alapú filter
- `PATCH /api/products/:id` — inline edit perzisztálás
- `GET /api/catalog/kpis` — cache-elt aggregációk (csak ha localStorage >5000 termék)

**⚠️ Megjegyzés:** Backend API-k OPCIONÁLISAK Phase 3-ban. Ha nincs kész backend endpoint, localStorage-ből dolgozz. A backend később bővíthető.

---

## Timeline és erőforrás becslés

**Week 1:**
- KPI Dashboard: 3h
- Inline Edit (conflict detection-nel): 6h
- **Total:** 9h (1.1 dev nap)

**Week 2:**
- Smart Filter core: 6h
- FilterPresets + URL sync: 3h
- Refactoring + documentation: 2h
- **Total:** 11h (1.4 dev nap)

**Testing buffer:** +4h (Vitest unit + Playwright E2E)

**Grand Total:** 24h (3 dev nap) reális becslés

---

## Nyitott kérdések (Conductor-nak válaszolni)

A konsenzus 6 nyitott kérdést tartalmaz. Ezekre **NEM** kell most válaszolnod — kezdd a Phase 1 implementációval. A döntések majd a munkavégzés közben vagy Phase 4-ben tisztázódnak:

1. **Bundle size policy:** `react-grid-layout` (500KB) vagy CSS Grid? → **Javaslat: CSS Grid először**
2. **Nexus AI integráció prioritás:** "Ask Nexus" gombot KPI kártyákra? → **Javaslat: Phase 4**
3. **Multi-user környezet:** localStorage conflict detection **ideiglenes** → WebSocket mikor? → **Javaslat: Spike ticket Q3 2026**
4. **Filter config centralizálás:** `FILTER_CONFIGS` registry pattern? → **Javaslat: Inline Edit után decision point**
5. **Performance baseline:** Milyen termékszámnál kell `react-window`? → **Javaslat: 1000+ termék threshold, mérjünk Phase 1 után**
6. **Edit history tracking:** localStorage changeLog audit követelmény? → **Javaslat: Phase 4 vagy később**

---

## Definition of Done

### Phase 1 (KPI Dashboard)
- [ ] `KPIDashboard.jsx`, `KPICard.jsx`, `useKPICalculator.js` komponensek létrehozva
- [ ] localStorage `dashboardLayout` schema implementálva
- [ ] 4 KPI megjelenítve: inventory-value, active-skus, avg-price, low-stock
- [ ] Trend delta % megjelenítés működik
- [ ] Config modal a KPI sorrend módosítására
- [ ] CSS Grid layout reszponzív (desktop, tablet, mobile)
- [ ] Vitest unit tesztek: useKPICalculator hook
- [ ] Lighthouse Performance >= 90

### Phase 2 (Inline Editing)
- [ ] `EditableCell.jsx`, `useEditLock.js`, `ConflictWarning.jsx` komponensek létrehozva
- [ ] localStorage `editLocks` schema implementálva
- [ ] Double-click → edit mode, Esc → cancel, Enter → save
- [ ] `storage` event listener multi-tab szinkronhoz
- [ ] Conflict warning banner megjelenik ha másik tab szerkeszt
- [ ] Optimistic update stratégia működik
- [ ] Vitest unit tesztek: useEditLock hook
- [ ] Playwright E2E: inline edit flow (happy path + conflict)

### Phase 3 (RFQ Smart Filter)
- [ ] `SmartFilter.jsx`, `FilterRow.jsx`, `FilterPresets.jsx`, `useFilterState.js` komponensek
- [ ] Config-driven filter architektúra működik
- [ ] RFQ_FILTER_CONFIG 3 mezővel: vendor, status, createdAt
- [ ] URL sync: URL params ↔ filter state
- [ ] FilterPresets: "Last 30 days", "High-value RFQs" gyors gombok
- [ ] localStorage `filterPresets` schema implementálva
- [ ] Újrafelhasználható: Work Orders view-ra is alkalmazható
- [ ] Vitest unit tesztek: useFilterState hook
- [ ] Playwright E2E: filter + preset flow

### Összesített
- [ ] Meglévő frontend tesztek zöld (Vitest + Playwright)
- [ ] Új tesztek: >= 12 db (4 per phase)
- [ ] Bundle size: app entry < 200KB gzip
- [ ] Lazy-loaded catalog world chunk: < 80KB gzip
- [ ] Lighthouse Accessibility >= 95
- [ ] No console errors prod build-ben
- [ ] `npm run lint` 0 error

---

## Végrehajtási sorrend (track-ek)

### Track A — Phase 1 (KPI Dashboard)
1. localStorage schema definiálás (`dashboardLayout`)
2. `useKPICalculator.js` hook implementálás
3. `KPICard.jsx` komponens
4. `KPIDashboard.jsx` container + CSS Grid layout
5. Config modal a sorrend módosítására
6. Vitest unit tesztek

### Track B — Phase 2 (Inline Editing)
1. localStorage schema definiálás (`editLocks`)
2. `useEditLock.js` hook + storage event listener
3. `EditableCell.jsx` komponens
4. `ConflictWarning.jsx` banner
5. Inline edit integráció a Catalog táblázatba
6. Vitest unit + Playwright E2E tesztek

### Track C — Phase 3 (Smart Filter)
1. localStorage schema definiálás (`filterPresets`)
2. `useFilterState.js` hook + URL sync
3. `FilterRow.jsx`, `SmartFilter.jsx` komponensek
4. `RFQ_FILTER_CONFIG` config objektum
5. `FilterPresets.jsx` saved queries
6. Integráció RFQ view-ba
7. Vitest unit + Playwright E2E tesztek

**⚠️ Fontos:** Track A → B → C sorrend kötelező. Nem párhuzamos!

---

## Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|
| localStorage méretkorlát (5MB) | Közepes | Közepes | Phase 1 után mérni, ha 80%+, backend API prioritás emelése |
| Multi-tab conflict detection törékeny | Magas | Alacsony | localStorage conflict csak warning, nem block |
| Filter config nem elég flexibilis | Alacsony | Közepes | Work Orders view-ra is tesztelni Phase 3-ban |
| `react-window` szükséges lehet | Közepes | Alacsony | 1000+ termék baseline mérés Phase 1 után |

---

## Mi jön utána (roadmap)

**Phase 4 (future):**
- "Ask Nexus" AI integráció KPI kártyákra
- Drag-drop KPI sorrend (`react-grid-layout`)
- Edit history tracking (localStorage changeLog)
- WebSocket multi-user szinkronizáció (localStorage → real-time)
- Backend API integration (perzisztálás, aggregációk)

---

**Conductor megjegyzés:**

Ez a 3 fázis egy tiszta, incremental MVP. Kezdd Phase 1-gyel (KPI Dashboard), és csak akkor lépj Phase 2-re ha Phase 1 DoD teljesült. A konsenzusban 6 nyitott kérdés van — ezekre NEM kell most válaszolnod, csak implementálj a javaslatok szerint (CSS Grid, localStorage első kör, stb.).

Ha blokkol bármi (backend API hiány, architectural döntés), írd meg outbox-ba BLOCKED üzenettel.

Good luck! 🚀
