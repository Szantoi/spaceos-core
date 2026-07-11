---
id: MSG-FRONTEND-008
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md
created: 2026-06-23
content_hash: 937039e16c6e67c3d4a8751daf72d1aadf2b80814ce6c0ab6b63ceb8c7520545
---

# Catalog Filter Week 1 — State + Search + Chips (FE-CAT-001→003)

## Kontextus

Az Architect elkészítette a **Catalog + EHS Hybrid Architecture** teljes review pipeline-ját (v1→v4). Most a **Week 1 Frontend Track A** első 3 taskját kapod meg, amelyek a **Catalog Filter MVP alapjait** építik ki.

**Architektúra dokumentumok:** `/opt/spaceos/docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`

## 🟠 HIGH Security Fix

Ezek a taskok 1 HIGH security issue-t is kezelnek:

- **H1 (v3-Security):** XSS in catalog filter — HTML tag stripping a search inputból

## Taskok (3.5 óra becsült munka)

### FE-CAT-001: App Store Catalog Filter State (1h)

**Dependencies:** None

**Acceptance Criteria:**
- [ ] Add `catalogFilters` state (search, category[], priceRange, stockStatus)
- [ ] Implement `setFilter(key, value)` method
- [ ] Persist filters to localStorage
- [ ] URL sync (push search params to window.history)
- [ ] Implement `resetFilters()` method
- [ ] Write unit test: Filter state updates correctly

**Files to modify:**
- `design-portal/src/store/app-store.jsx`

**Code snippet:**
```javascript
// app-store.jsx
catalogFilters: {
  search: '',
  category: [],
  priceRange: [0, 1000000],
  stockStatus: 'all'
},
setFilter: (key, value) => {
  set(state => {
    const newFilters = { ...state.catalogFilters, [key]: value };
    localStorage.setItem('catalog_filters', JSON.stringify(newFilters));

    // URL sync
    const params = new URLSearchParams();
    if (newFilters.search) params.set('search', newFilters.search);
    if (newFilters.category.length) params.set('cat', newFilters.category.join(','));
    window.history.pushState({}, '', `?${params.toString()}`);

    return { catalogFilters: newFilters };
  });
},
resetFilters: () => {
  set({
    catalogFilters: {
      search: '',
      category: [],
      priceRange: [0, 1000000],
      stockStatus: 'all'
    }
  });
  localStorage.removeItem('catalog_filters');
  window.history.pushState({}, '', window.location.pathname);
}
```

**Unit Test:**
```javascript
test('setFilter updates state and persists to localStorage', () => {
  const store = useAppStore.getState();

  store.setFilter('search', 'wood panel');

  expect(store.catalogFilters.search).toBe('wood panel');
  expect(localStorage.getItem('catalog_filters')).toContain('wood panel');
});
```

---

### FE-CAT-002: SmartSearchBar Component (1.5h)

**Dependencies:** FE-CAT-001

**Acceptance Criteria:**
- [ ] Install `fuzzysort` npm package
- [ ] Create `SmartSearchBar.jsx` component
- [ ] Implement debounced search (300ms delay)
- [ ] Integrate with `catalogFilters.search` state
- [ ] **SECURITY FIX (v3-H1):** Strip HTML tags from input (XSS protection)
- [ ] Write E2E test: XSS payload `<script>alert(1)</script>` is escaped

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/SmartSearchBar.jsx`

**Code snippet (XSS fix):**
```jsx
import { useState, useEffect } from 'react';
import { useAppStore } from '../../../store/app-store';

export function SmartSearchBar() {
  const [localValue, setLocalValue] = useState('');
  const setFilter = useAppStore(state => state.setFilter);

  useEffect(() => {
    const timer = setTimeout(() => {
      // ✅ SECURITY FIX (v3-H1): Strip HTML tags
      const sanitized = localValue.replace(/<[^>]*>/g, '');
      setFilter('search', sanitized);
    }, 300);

    return () => clearTimeout(timer);
  }, [localValue]);

  return (
    <input
      type="text"
      value={localValue}
      onChange={(e) => setLocalValue(e.target.value)}
      placeholder="Keresés termékek között..."
      className="w-full px-4 py-2 border border-slate-300 rounded-md focus:ring-2 focus:ring-blue-500"
    />
  );
}
```

**E2E Test (XSS):**
```javascript
test('XSS payload in search is escaped', () => {
  const { container } = render(<SmartSearchBar />);
  const input = container.querySelector('input');

  fireEvent.change(input, { target: { value: '<script>alert(1)</script>' } });

  // Wait for debounce
  setTimeout(() => {
    const store = useAppStore.getState();
    expect(store.catalogFilters.search).toBe('alert(1)');  // HTML tags stripped
    expect(store.catalogFilters.search).not.toContain('<script>');
  }, 350);
});
```

---

### FE-CAT-003: Category Chips Component (1h)

**Dependencies:** FE-CAT-001

**Acceptance Criteria:**
- [ ] Create `CategoryChips.jsx` component
- [ ] Load categories from catalog data (distinct values)
- [ ] Multi-select toggle (click to add/remove)
- [ ] Integrate with `catalogFilters.category` state
- [ ] Active state styling (blue background for selected)
- [ ] Write unit test: Category toggle adds/removes from array

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/CategoryChips.jsx`

**Code snippet:**
```jsx
import { useAppStore } from '../../../store/app-store';

export function CategoryChips({ categories }) {
  const selectedCategories = useAppStore(state => state.catalogFilters.category);
  const setFilter = useAppStore(state => state.setFilter);

  const toggleCategory = (cat) => {
    const newCategories = selectedCategories.includes(cat)
      ? selectedCategories.filter(c => c !== cat)
      : [...selectedCategories, cat];

    setFilter('category', newCategories);
  };

  return (
    <div className="flex flex-wrap gap-2">
      {categories.map(cat => {
        const isActive = selectedCategories.includes(cat);
        return (
          <button
            key={cat}
            onClick={() => toggleCategory(cat)}
            className={`px-3 py-1 rounded-full text-sm transition-colors ${
              isActive
                ? 'bg-blue-500 text-white'
                : 'bg-slate-200 text-slate-700 hover:bg-slate-300'
            }`}
          >
            {cat}
          </button>
        );
      })}
    </div>
  );
}
```

**Unit Test:**
```javascript
test('Category chip toggles selection', () => {
  const { getByText } = render(<CategoryChips categories={['Wood', 'Metal']} />);

  const woodChip = getByText('Wood');
  fireEvent.click(woodChip);

  const store = useAppStore.getState();
  expect(store.catalogFilters.category).toContain('Wood');

  fireEvent.click(woodChip);
  expect(store.catalogFilters.category).not.toContain('Wood');
});
```

---

## Definition of Done

- [ ] FE-CAT-001: Filter state perzisztál localStorage-ba ÉS URL-be
- [ ] FE-CAT-002: XSS teszt PASS (`<script>` tag eltávolítva)
- [ ] FE-CAT-003: Multi-select kategória chip működik
- [ ] Minden komponens unit test PASS
- [ ] Build 0 hiba, 0 warning
- [ ] Code formázva (prettier)
- [ ] Storybook story minden komponenshez (opcionális)

## Tesztelési útmutató

### localStorage persistencia teszt:
1. Állíts be egy search filter-t: "wood panel"
2. Frissítsd az oldalt (F5)
3. Ellenőrizd hogy a search input értéke megmaradt

### URL sync teszt:
1. Állíts be search: "panel", category: "Wood", "Metal"
2. Nézd meg az URL-t → `?search=panel&cat=Wood,Metal`
3. Másold ki az URL-t új tab-ba → filterek betöltődnek

### XSS teszt (KRITIKUS):
1. DevTools Console nyitva
2. Search input-ba: `<script>alert('XSS')</script>`
3. **NE** legyen alert — csak `alert('XSS')` szöveg a filter state-ben

---

## Következő lépések

Ha ezek a taskok **DONE**:
1. Conductor kiadja a következő frontend bundle-t: **FE-CAT-004→007** (Price slider, fuzzy search, virtualized grid, voice search)
2. Backend párhuzamosan implementálja a **BE-EHS-001→005** (Security infrastructure)
3. Week 1 végére: **Catalog Filter MVP + Backend Endpoints DEPLOYED**

---

**Prioritás:** HIGH
**Becsült idő:** ~3.5 óra
**Review:** Automatikus (TypeScript reviewer pipeline)
**Deploy blocker:** Nem — de XSS fix KRITIKUS!

**Files changed:** (majd a DONE outbox-ban)
