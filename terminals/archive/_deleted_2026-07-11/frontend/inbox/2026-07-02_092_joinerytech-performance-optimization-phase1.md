---
id: MSG-FRONTEND-092
from: conductor
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-02
model: haiku
ref: MSG-FRONTEND-089
created: 2026-07-02
content_hash: af153a09287b6cbed52b1378fd9c707e07e06b74a7946a90391e2224cc3c951f
---

# JoineryTech Performance Optimization — Phase 1 (Quick Wins)

## Context

UI/UX/Performance Audit (MSG-FRONTEND-089-DONE) azonosította a kritikus performance problémákat:

**🔴 KRITIKUS:**
- app-store.jsx: 488 KB monolith (9,087 sor)
- Build folder: 4.2 MB
- Nincs lazy-loading, code splitting
- **Target:** 50%+ méret csökkentés

**Audit Report:** `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md`

## Task

Implementáld a **Performance Optimization Phase 1** quick win-eket (2 hét, 30-40% javulás).

### Phase 1 Scope (Quick Wins)

#### 1. Store Splitting (🔴 Kritikus - 200KB+ csökkentés)

**Probléma:** `app-store.jsx` (488KB) minden modult tartalmaz egyetlen fájlban.

**Megoldás:** Store slice-ok modulonként

```javascript
// app-store.jsx (core only)
import { crmStore } from './stores/crm-store.js';
import { salesStore } from './stores/sales-store.js';
import { warehouseStore } from './stores/warehouse-store.js';

window.sim = {
  ...crmStore.getInitialState(),
  ...salesStore.getInitialState(),
  ...warehouseStore.getInitialState(),
  // ...
};
```

**Store slice minta:**
```javascript
// stores/crm-store.js (csak CRM slice)
export const crmStore = {
  getInitialState: () => ({
    leads: [],
    opportunities: [],
    crmSeq: { lead: 1, opp: 1 }
  }),

  actions: {
    createLead: (state, payload) => { /* ... */ },
    convertLeadToOpp: (state, leadId) => { /* ... */ }
  }
};
```

**Target:** 488KB → 150-200KB core + lazy-loaded slices

---

#### 2. Lazy Loading by World (🔴 Kritikus - 1MB+ csökkentés)

**Probléma:** Minden `page-*.jsx` betöltődik app induláskor (4.2MB).

**Megoldás:** Dynamic import world-önként

```javascript
// App.jsx routing
const worlds = {
  sales: () => import('./page-sales.jsx'),
  crm: () => import('./page-crm.jsx'),
  warehouse: () => import('./page-warehouse.jsx'),
  // ...
};

// World váltáskor
async function loadWorld(worldId) {
  const module = await worlds[worldId]();
  return module.default;
}
```

**Target:** Initial bundle 4.2MB → 800KB-1MB (csak core + home)

---

#### 3. Image Optimization (🟡 Fontos - 300KB+ csökkentés)

**Probléma:** `image-slot.js` placeholder PNG-k beágyazva base64-ben.

**Megoldás:**
- WebP konverzió (50% méret csökkentés PNG-hez képest)
- Lazy load képek (`loading="lazy"`)
- Placeholder URL-ek CDN-ről (ne inline base64)

```javascript
// image-slot.js
export const IMAGE_PLACEHOLDER = {
  cabinet: 'https://cdn.joinerytech.hu/placeholders/cabinet.webp',
  door: 'https://cdn.joinerytech.hu/placeholders/door.webp',
  // ...
};
```

**Target:** 300KB+ base64 removal

---

#### 4. Build Optimization (🟡 Fontos)

**Probléma:** Babel transpile minden JSX-re, minification hiányzik.

**Megoldás:**
```javascript
// babel.config.js (új fájl)
{
  "presets": [
    ["@babel/preset-react", { "runtime": "automatic" }]
  ],
  "plugins": [
    ["@babel/plugin-transform-react-jsx", { "pragma": "React.createElement" }]
  ]
}

// Minification
// uglify-js vagy terser (build script-be)
```

**Target:** 10-15% build méret csökkentés

---

#### 5. localStorage Optimization (🟡 Fontos)

**Probléma:** Teljes store persist minden műveletkor (488KB write).

**Megoldás:**
- Debounce persist (500ms késleltetés)
- Csak változott slice-ok írása
- Compression (LZString)

```javascript
// app-store.jsx
import LZString from 'lz-string';

function persistStore(state) {
  const compressed = LZString.compress(JSON.stringify(state));
  localStorage.setItem(LS_KEY, compressed);
}

// Debounced persist
const debouncedPersist = debounce(persistStore, 500);
```

**Target:** 488KB → 150-200KB compressed storage

---

## Deliverables

### Phase 1-A: Store Splitting (Week 1)
- [ ] `stores/` folder létrehozása
- [ ] 5 store slice (crm, sales, warehouse, production, catalog)
- [ ] Core store refactor (slice imports)
- [ ] Build script frissítés (slice-ok buildelése)
- [ ] localStorage load/save refactor

### Phase 1-B: Lazy Loading (Week 1-2)
- [ ] Dynamic import routing (world-önként)
- [ ] Loading state komponens
- [ ] Preload stratégia (hover/idle)
- [ ] Error boundary (chunk load failure)

### Phase 1-C: Image & Build Optimization (Week 2)
- [ ] WebP konverzió (PNG → WebP)
- [ ] Image lazy load (`loading="lazy"`)
- [ ] Babel minification setup
- [ ] localStorage compression (LZString)

### Phase 1-D: Measurement & Validation (Week 2 vége)
- [ ] Bundle size report (before/after)
- [ ] Lighthouse performance audit
- [ ] Initial load time measurement
- [ ] localStorage write time measurement

## Acceptance Criteria

- [ ] Bundle size: 4.2MB → <2MB (50%+ csökkentés)
- [ ] Initial load: <3s (4G connection)
- [ ] localStorage write: <100ms (compressed)
- [ ] No breaking changes (minden funkció működik)
- [ ] Build script dokumentálva (README.md)
- [ ] DONE outbox üzenet (before/after metrics)

## Technical Stack

- **Current:** React 18 + Babel CDN + localStorage
- **New tools:**
  - LZString (compression)
  - Terser (minification)
  - WebP converter (imagemin)

## Reference

- Audit Report: `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md`
- Prototípus: `/opt/spaceos/docs/joinerytech/` (project root)
- Build guide: `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` (Section 7: Build)

## Timeline

**Becsült idő:** 2 hét (40 óra)
- Week 1: Store splitting + Lazy loading kezdés
- Week 2: Lazy loading befejezés + Image/Build opt + Validation

## Success Metrics (Before/After)

| Metric | Before | Target | Improvement |
|--------|--------|--------|-------------|
| Bundle size | 4.2 MB | <2 MB | 50%+ |
| app-store.js | 488 KB | <200 KB | 60%+ |
| Initial load | 5-6s | <3s | 40-50% |
| localStorage write | 200-300ms | <100ms | 60%+ |

## Notes

Ez Phase 1 - a **quick wins** (low-hanging fruit). Phase 2 (Dark mode + A11y) és Phase 3 (Long-term maintainability) később következnek.

**Haiku model** - straightforward refactor, established patterns (code splitting, lazy load).
