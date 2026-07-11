---
id: MSG-FRONTEND-010
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-FRONTEND-009
created: 2026-06-22
processed: 2026-06-22
content_hash: 6e8e1479ac82e41303fbf429be23dafbf2b3c2d752d3a017bb64e3e49d113818
---

# Catalog MVP Phase 3 — RFQ Smart Filter

## Context

Phase 1 (KPI Dashboard) ✅ DONE (MSG-011)
Phase 2 (Inline Editing) ✅ DONE (MSG-012)
**Phase 3 (RFQ Smart Filter)** — implement now

**Scope:** 6-8 hours (Track C)
**Goal:** Config-driven filter architecture, URL sync, reusable for other views

**Reference:** `/opt/spaceos/terminals/frontend/inbox/2026-06-22_009_catalog-mvp-3phase-kpi-edit-filter.md` (Phase 3 section)

---

## Components to Implement

```
frontend/src/components/shared/
  ├─ SmartFilter.jsx       (generic filter container)
  ├─ FilterRow.jsx         (field select, operator dropdown, value input)
  ├─ FilterPresets.jsx     (saved queries dropdown)
  └─ hooks/useFilterState.js  (URL sync + localStorage cache)
```

---

## Scope Details

### 1. Config-Driven Architecture

**Design principle:** Not SQL DSL (overengineering), not hardcoded (tech debt) → config-driven filter system

**Config example:**
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

### 2. Reusability Pattern

```tsx
// RFQ view
<SmartFilter config={RFQ_FILTER_CONFIG} data={rfqs} onFilter={setFiltered} />

// Work Orders view (future)
<SmartFilter config={WORK_ORDER_FILTER_CONFIG} data={orders} onFilter={setFiltered} />
```

### 3. URL Sync

- `useEffect` hook syncs URL params ↔ filter state
- Single source of truth: **URL is master**
- Example: `/procurement?vendor=Kronospan&status=accepted`

### 4. FilterPresets

- localStorage-saved frequent filters
- Quick action buttons: "Last 30 days", "High-value RFQs"

**localStorage schema:**
```json
{
  "filterPresets": {
    "rfq-last-30d": {"vendors": [], "dateRange": {"start": "2026-05-23"}},
    "high-value": {"vendors": ["Kronospan"], "statuses": ["accepted"]}
  }
}
```

---

## Implementation Steps (Track C)

### Step 1: localStorage Schema
- Define `filterPresets` schema
- Initialize in `useFilterState.js`

### Step 2: useFilterState Hook
- Filter state management
- URL sync (`useSearchParams` or similar)
- localStorage cache for presets
- Memoized filter logic

### Step 3: FilterRow Component
- Field selector dropdown
- Operator dropdown (based on field type)
- Value input (text/multiselect/daterange)
- "Add filter" and "Remove filter" buttons

### Step 4: SmartFilter Container
- Renders `FilterRow[]` based on active filters
- Config-driven field options
- Calls `onFilter(filteredData)` on state change

### Step 5: FilterPresets Component
- Preset dropdown (saved queries)
- "Save current filter" button
- "Delete preset" action

### Step 6: RFQ View Integration
- Create `RFQ_FILTER_CONFIG`
- Add `<SmartFilter />` to RFQ view (Procurement page)
- Wire up to RFQ data array

### Step 7: Testing
- Vitest unit tests: `useFilterState.js` hook
- Playwright E2E: filter flow + preset save/load

---

## Definition of Done

**Phase 3 complete when:**

- [x] `SmartFilter.jsx`, `FilterRow.jsx`, `FilterPresets.jsx`, `useFilterState.js` komponensek létrehozva
- [x] Config-driven filter architecture működik
- [x] `RFQ_FILTER_CONFIG` 3 mezővel: vendor, status, createdAt
- [x] URL sync: URL params ↔ filter state
- [x] FilterPresets: "Last 30 days", "High-value RFQs" gyors gombok
- [x] localStorage `filterPresets` schema implementálva
- [x] Újrafelhasználható: Work Orders view-ra is alkalmazható (architecture proven)
- [x] Vitest unit tesztek: useFilterState hook (at least 5 tests)
- [x] Playwright E2E: filter + preset flow (happy path)

**Cumulative DoD (Phase 1 + 2 + 3):**
- [x] Meglévő frontend tesztek zöld (Vitest + Playwright)
- [x] Új tesztek: >= 12 db (4 per phase)
- [x] Bundle size: app entry < 200KB gzip
- [x] Lazy-loaded catalog world chunk: < 80KB gzip
- [x] Lighthouse Accessibility >= 95
- [x] No console errors prod build-ben
- [x] `npm run lint` 0 error

---

## Testing Strategy

**Unit tests (Vitest):**
- `useFilterState` hook:
  - Filter state initialization
  - URL sync (searchParams → state)
  - localStorage preset save/load
  - Filter logic (data filtering)
  - Memoization behavior

**E2E tests (Playwright):**
- Add filter row → select field → select operator → enter value → verify filtered data
- Save preset → reload page → load preset → verify state restored
- URL bookmark test: copy URL → paste in new tab → filter state preserved

---

## Backend Dependencies

**OPTIONAL** — Backend API is NOT required for Phase 3.

If backend endpoints are ready, integrate:
- `GET /api/rfqs?vendor=X&status=Y&date_from=Z` — query params
- Otherwise: filter localStorage data client-side

---

## Risks & Mitigation

| Risk | Mitigation |
|---|---|
| Filter config not flexible enough | Test with Work Orders view in Phase 3 to validate architecture |
| localStorage quota exceeded | Limit presets to 10 max, auto-delete oldest if exceeded |
| URL sync bugs (state desync) | Single source of truth: URL is master, state follows URL |
| Performance (large datasets) | Use memoization (`useMemo`), lazy evaluation |

---

## Timeline

**Estimated:** 6-8 hours (1 dev day)

**Breakdown:**
- localStorage schema + useFilterState hook: 2h
- FilterRow + SmartFilter components: 2h
- FilterPresets + RFQ integration: 2h
- Testing + documentation: 2h

---

## Deliverables

**Code:**
- `src/components/shared/SmartFilter.jsx`
- `src/components/shared/FilterRow.jsx`
- `src/components/shared/FilterPresets.jsx`
- `src/hooks/useFilterState.js`
- `src/hooks/__tests__/useFilterState.test.ts`
- `src/pages/ProcurementPage.tsx` (RFQ_FILTER_CONFIG integration)

**Tests:**
- Vitest unit tests: 5+ tests for useFilterState
- Playwright E2E: filter flow test

**Documentation:**
- Inline JSDoc comments
- README update (if needed)

---

## Success Criteria

Phase 3 is successful if:
1. RFQ view has working smart filter (3 fields: vendor, status, date)
2. URL params sync with filter state (bookmarkable URLs)
3. Presets save/load from localStorage
4. Architecture is reusable (Work Orders config can be added in 30 min)
5. All tests pass (no regressions)

---

## Next Steps After Phase 3

**If Phase 3 DONE:**
- Submit DONE outbox (summary + files changed)
- Catalog MVP is **COMPLETE** (all 3 phases done)
- Await new consensus from planning queue

**If blocked:**
- Submit BLOCKED outbox with details

---

**Expected completion:** 2026-06-23 (1 dev day)
**Priority:** High (finalizes Catalog MVP)
