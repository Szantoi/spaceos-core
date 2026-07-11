---
id: MSG-FRONTEND-023
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ARCHITECT-OUT-001
created: 2026-06-23
content_hash: 961ea8939684c34c2211c1609f05d9a183ba2d6b39aabfd23a3b2fe12aee5a04
---

# Week 1: Catalog Filter MVP (7 tasks)

## Epic Context

**CATALOG-EHS-HYBRID** - Week 1 Track A: Frontend Catalog Filter implementation.

Az Architect v1→v4 review pipeline-t futtatott (DB, Security, Backend review).
Week 1 célja: **Catalog Filter MVP** fuzzy search-sel, XSS fix-szel, voice search-sel.

## Architecture Reference

**TELJES ARCHITEKTÚRA:**
`docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`

## Week 1 Frontend Tasks (7 × 1-2h)

### FE-CAT-001: App Store Catalog Filter State (1h)
- Extend `app-store.jsx` with catalog filter state
- State: `catalogFilters` (search, category[], priceRange, stockStatus)
- Persist to localStorage + URL sync
- **Files:** `design-portal/src/store/app-store.jsx`

### FE-CAT-002: SmartSearchBar Component (1.5h) 🔴 SECURITY FIX
- Install `fuzzysort` npm package
- Debounced search (300ms)
- **CRITICAL FIX (v3-H1):** Strip HTML tags from input (XSS protection)
  ```jsx
  value = value.replace(/<[^>]*>/g, '');  // ✅ XSS fix
  ```
- **Test:** XSS payload `<script>alert(1)</script>` is escaped
- **Files:** `design-portal/src/components/catalog/CatalogFilterBar/SmartSearchBar.jsx`

### FE-CAT-003: Category Chips Component (1h)
- Multi-select category filter chips
- Load categories from catalog data (distinct values)
- Active state styling (blue for selected)
- **Files:** `design-portal/src/components/catalog/CatalogFilterBar/CategoryChips.jsx`

### FE-CAT-004: Price Range Slider + Stock Status Toggle (1.5h)
- Dual-thumb price range slider
- Stock status toggle (In Stock / All Items)
- Integrate with `catalogFilters` state
- **Files:** `design-portal/src/components/catalog/CatalogFilterBar/PriceRangeSlider.jsx`, `StockStatusToggle.jsx`

### FE-CAT-005: Fuzzy Search Hook (1.5h)
- Custom hook `useFuzzySearch` with fuzzysort integration
- Scoring: exact match = 1.0, partial = 0.8, fuzzy = 0.5
- Multi-field search (name, category, description)
- **Files:** `design-portal/src/hooks/useFuzzySearch.js`

### FE-CAT-006: Virtualized Catalog Grid (1h)
- Implement `react-window` for large catalog rendering
- Grid layout: 3 columns desktop, 1 column mobile
- Lazy load product images
- **Files:** `design-portal/src/components/catalog/CatalogGrid.jsx`

### FE-CAT-007: Voice Search Integration (1h) 🔴 SECURITY FIX
- Integrate Web Speech API (`webkitSpeechRecognition`)
- Microphone button in search bar
- **CRITICAL FIX (v3-M2):** Sanitize voice search results (strip HTML)
- Fallback: "Voice search not supported" message
- **Files:** `design-portal/src/hooks/useVoiceSearch.js`

## Critical Security Fixes (Week 1)

**v3-H1 (HIGH):** XSS in Catalog Filter
- Tasks: FE-CAT-002, FE-CAT-007
- Mitigation: Strip HTML tags from search input + voice search

## Week 1 Checkpoint Criteria

✅ All 7 tasks DONE
✅ E2E test: XSS payload `<script>alert(1)</script>` escaped
✅ Fuzzy search working (example: "wod pnal" finds "wood panel")
✅ URL state sync working (refresh preserves filters)
✅ Voice search functional (Chrome/Edge)
✅ Performance: Catalog grid renders 1000+ items without lag

## Implementation Order

**Recommended sequence:**
1. FE-CAT-001 (foundation - state management)
2. FE-CAT-002 (search bar with XSS fix)
3. FE-CAT-003 (category chips)
4. FE-CAT-004 (price slider + stock toggle)
5. FE-CAT-005 (fuzzy search hook)
6. FE-CAT-006 (virtualized grid)
7. FE-CAT-007 (voice search with XSS fix)

## Dependencies

- None - this is Track A (frontend-only MVP)
- Backend EHS API (Week 1 Track B) runs in parallel

## Next Steps

1. Read full architecture: `docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`
2. Implement FE-CAT-001 → FE-CAT-007 sequentially
3. Run E2E tests (XSS, fuzzy search, performance)
4. DONE outbox when all 7 tasks complete

## Estimated Effort

⏱️ **9 hours** (7 tasks × 1-1.5h average)

---

**Conductor**
2026-06-23
