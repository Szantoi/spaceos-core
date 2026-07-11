---
id: MSG-FRONTEND-055
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: CONSENSUS-2026-06-23
created: 2026-06-29
content_hash: 552781f61f3eefd5d079516990053004e78bce587e0ab0751bace51f6c0cf68d
---

# Katalógus & Assembly Features Q3 - Frontend Implementáció

## Összefoglalás

3 high-value feature Frontend implementáció a 2026-06-23 konszenzus alapján:

1. **Assembly Szerelési Sorrend Drag-and-Drop** (Prioritás 1) — 5 nap
2. **Katalógus Filter Perzisztencia** (Prioritás 2) — 2 nap
3. **Katalógus Képoptimalizálás Phase 1** (Prioritás 3) — 3 nap

**Párhuzamos:** Backend `MSG-BACKEND-074` ugyanakkor az Assembly endpoint-ot implementálja.

---

## Feature 1: Assembly Drag-and-Drop UI (Prioritás 1)

### Komponens: `assembly-operations-list.jsx`

Szükséges:
- **Library:** `@dnd-kit/core` + `@dnd-kit/sortable` (npm install)
- **Komponensek:** Work order view-ban megjeleníteni az operations-t draggable lista formájában

### Implementációs vázlat

```jsx
// assembly-operations-list.jsx
import { DndContext, useSensor, PointerSensor, KeyboardSensor } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';

const sensors = [
  useSensor(PointerSensor, {
    activationConstraint: { distance: 8 }  // Touch scroll vs drag conflict fix
  }),
  useSensor(KeyboardSensor)  // Accessibility: keyboard navigation
];

export function AssemblyOperationsList({ workOrderId, operations: initialOps }) {
  const [operations, setOperations] = useState(initialOps);
  const [history, setHistory] = useState([]);  // Undo/redo support
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleDragEnd = async (event) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    // Optimistic UI: immediately update state
    const reorderedOps = reorderArray(operations, active.id, over.id);
    const prevOps = operations;
    setOperations(reorderedOps);

    // Command pattern: save action for undo
    const command = {
      execute: () => reorderedOps,
      undo: () => prevOps,
      timestamp: Date.now()
    };
    setHistory([...history, command]);

    // Haptic feedback (mobile)
    if ('vibrate' in navigator) navigator.vibrate([5, 50, 5]);

    // API call
    setLoading(true);
    try {
      const response = await api.patch(
        `/api/v1/work-orders/${workOrderId}/assembly-sequence`,
        {
          operations: reorderedOps.map((op, idx) => ({
            id: op.id,
            sequence: idx + 1
          })),
          timestamp: new Date().toISOString()
        }
      );

      // Update state with server response
      setOperations(response.updated_operations);
      // Show estimated duration change
      toast.success(`Sorrend mentve (+${response.estimated_duration_change})`);
    } catch (err) {
      if (err.status === 409) {
        // Conflict: rollback + notify user
        setOperations(prevOps);
        setHistory(history.slice(0, -1));
        toast.error('Mások módosították a műveletsort. Frissíts és próbáld újra.');
      } else {
        // Other error: rollback
        setOperations(prevOps);
        setHistory(history.slice(0, -1));
        toast.error('Hiba történt: ' + (err.message || 'Unknown error'));
      }
    } finally {
      setLoading(false);
    }
  };

  const handleUndo = () => {
    if (history.length === 0) return;
    const lastCommand = history[history.length - 1];
    setOperations(lastCommand.undo());
    setHistory(history.slice(0, -1));
  };

  return (
    <div className="assembly-operations">
      <div className="flex justify-between items-center mb-4">
        <h3>Szerelési Műveletek Sorrendje</h3>
        <button
          onClick={handleUndo}
          disabled={history.length === 0 || loading}
          className="px-3 py-1 text-sm bg-gray-200 rounded"
        >
          ↶ Visszavonás
        </button>
      </div>

      {error && <div className="bg-red-100 p-3 rounded mb-4">{error}</div>}

      <DndContext
        sensors={sensors}
        onDragEnd={handleDragEnd}
      >
        <SortableContext
          items={operations}
          strategy={verticalListSortingStrategy}
        >
          <ul className="space-y-2">
            {operations.map((op, idx) => (
              <SortableOperation
                key={op.id}
                operation={op}
                sequence={idx + 1}
                isLoading={loading}
              />
            ))}
          </ul>
        </SortableContext>
      </DndContext>
    </div>
  );
}

// Segédkomponens
function SortableOperation({ operation, sequence, isLoading }) {
  const { attributes, listeners, setNodeRef, transform } = useSortable({ id: operation.id });
  const style = transform ? {
    transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`
  } : undefined;

  return (
    <li
      ref={setNodeRef}
      style={style}
      className="p-3 bg-white border rounded flex items-center gap-3 cursor-grab"
      {...attributes}
      {...listeners}
    >
      <span className="font-bold text-gray-400 w-6">{sequence}</span>
      <span className="flex-1">{operation.description}</span>
      <span className="text-sm text-gray-600">{operation.estimated_duration}</span>
    </li>
  );
}
```

### Tesztek szükségesek

- ✅ Drag-drop funkció: elem mozgatása listán (unit + E2E)
- ✅ Optimistic UI: state frissül szerver válasz előtt
- ✅ Conflict handling (409): UI rollback
- ✅ Undo/redo: history command pattern
- ✅ Haptic feedback: navigator.vibrate hívódik mobilon
- ✅ Accessibility: keyboard navigation (ArrowUp/Down)

---

## Feature 2: Katalógus Filter Perzisztencia (Prioritás 2)

### Store: `useCatalogStore` - localStorage + BroadcastChannel sync

**Mit kell tárolni:**
- Kiválasztott szűrők (anyag, szín, ár, stb.)
- View mode (grid vs lista)
- Timestamp (24 óras expiry)

**Implementáció:**

```javascript
// hooks/useCatalogStore.js
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

const FILTER_VERSION = 2;  // Inkrementálj ha breaking change
const bc = new BroadcastChannel('spaceos_filters');
const EXPIRY_TIME = 24 * 60 * 60 * 1000;  // 24 óra

export const useCatalogStore = create(
  persist(
    (set, get) => ({
      filters: {},
      viewMode: 'grid',
      saveTimeout: null,

      setFilters: (filters) => {
        set({ filters });

        // Debounce: 300ms után save
        const { saveTimeout } = get();
        clearTimeout(saveTimeout);

        const timeout = setTimeout(() => {
          try {
            const data = {
              filters,
              viewMode: get().viewMode,
              timestamp: Date.now(),
              version: FILTER_VERSION
            };
            const jsonStr = JSON.stringify(data);

            // Plan-B: Compression if too large
            if (jsonStr.length > 50000) {
              // Fallback to sessionStorage if quota exceeded
              sessionStorage.setItem(
                `spaceos_catalog_v${FILTER_VERSION}`,
                jsonStr
              );
              console.warn('localStorage quota near limit, using sessionStorage');
            } else {
              localStorage.setItem(
                `spaceos_catalog_v${FILTER_VERSION}`,
                jsonStr
              );
            }

            // Multi-tab sync: notify other tabs
            bc.postMessage({
              type: 'FILTER_UPDATE',
              filters,
              viewMode: get().viewMode,
              timestamp: Date.now()
            });
          } catch (e) {
            if (e.name === 'QuotaExceededError') {
              sessionStorage.setItem(
                `spaceos_catalog_v${FILTER_VERSION}`,
                jsonStr
              );
              console.warn('localStorage quota exceeded, fallback to sessionStorage');
            }
          }
        }, 300);

        set({ saveTimeout: timeout });
      },

      setViewMode: (viewMode) => {
        set({ viewMode });
        // Save immediately
        get().setFilters(get().filters);
      },

      loadFilters: () => {
        // Try localStorage first
        let stored = localStorage.getItem(`spaceos_catalog_v${FILTER_VERSION}`);

        // Fallback to sessionStorage
        if (!stored) {
          stored = sessionStorage.getItem(`spaceos_catalog_v${FILTER_VERSION}`);
        }

        if (!stored) return null;

        try {
          const { filters, viewMode, timestamp } = JSON.parse(stored);

          // Expiry check: 24 óra
          if (Date.now() - timestamp > EXPIRY_TIME) {
            localStorage.removeItem(`spaceos_catalog_v${FILTER_VERSION}`);
            return null;
          }

          return { filters, viewMode };
        } catch (e) {
          console.error('Failed to load filters:', e);
          return null;
        }
      },

      clearFilters: () => {
        set({ filters: {}, viewMode: 'grid' });
        localStorage.removeItem(`spaceos_catalog_v${FILTER_VERSION}`);
        sessionStorage.removeItem(`spaceos_catalog_v${FILTER_VERSION}`);
      }
    }),
    {
      name: 'spaceos_catalog',
      version: FILTER_VERSION,
      // Persist config
      partialize: (state) => ({
        filters: state.filters,
        viewMode: state.viewMode
      })
    }
  )
);

// Listen to other tabs
bc.onmessage = (event) => {
  if (event.data.type === 'FILTER_UPDATE') {
    useCatalogStore.setState({
      filters: event.data.filters,
      viewMode: event.data.viewMode
    });
  }
};
```

### UI Integration

```jsx
// pages/CatalogPage.jsx
export function CatalogPage() {
  const { filters, setFilters, viewMode, setViewMode, loadFilters } = useCatalogStore();

  useEffect(() => {
    // Load saved filters on mount
    const saved = loadFilters();
    if (saved) {
      setFilters(saved.filters);
      setViewMode(saved.viewMode);
    }
  }, []);

  return (
    <div className="catalog-page">
      <FilterPanel
        filters={filters}
        onChange={setFilters}
      />

      <div className="flex gap-2 mb-4">
        <button
          onClick={() => setViewMode('grid')}
          className={viewMode === 'grid' ? 'active' : ''}
        >
          Grid View
        </button>
        <button
          onClick={() => setViewMode('list')}
          className={viewMode === 'list' ? 'active' : ''}
        >
          List View
        </button>
      </div>

      {viewMode === 'grid' ? (
        <ProductGrid products={filteredProducts} />
      ) : (
        <ProductList products={filteredProducts} />
      )}
    </div>
  );
}
```

### Tesztek

- ✅ localStorage save: filter change után 300ms-al
- ✅ localStorage load: page load-nál
- ✅ BroadcastChannel sync: multi-tab update
- ✅ Quota exceeded: sessionStorage fallback
- ✅ Expiry: 24 óra után clear
- ✅ versioning: filter_v2 kezelés

---

## Feature 3: Képoptimalizálás Phase 1 (Prioritás 3)

### Natív lazy-load megoldás

**Komponens:** ProductCard.jsx

```jsx
import { useEffect, useState } from 'react';
import { cn } from '@/lib/cn';

export function ProductCard({ product }) {
  const [imageState, setImageState] = useState('loading');

  return (
    <div className="product-card">
      <div className="relative aspect-[4/3] overflow-hidden rounded-lg bg-gray-100">
        {/* Skeleton loader */}
        {imageState === 'loading' && (
          <div className="absolute inset-0 bg-gradient-to-r from-gray-100 via-gray-50 to-gray-100 animate-shimmer" />
        )}

        {/* Main image */}
        <img
          loading="lazy"  // Native lazy-load
          src={product.imageUrl || '/assets/no-image.svg'}
          alt={product.name}
          onLoad={() => setImageState('loaded')}
          onError={() => setImageState('error')}
          className={cn(
            'w-full h-full object-cover transition-opacity duration-300',
            imageState === 'loaded' ? 'opacity-100' : 'opacity-0'
          )}
        />

        {/* Error state */}
        {imageState === 'error' && (
          <div className="absolute inset-0 flex items-center justify-center bg-gray-200">
            <span className="text-gray-500 text-sm">Kép nem elérhető</span>
          </div>
        )}
      </div>

      <div className="p-3">
        <h4 className="font-semibold">{product.name}</h4>
        <p className="text-gray-600">{product.description}</p>
      </div>
    </div>
  );
}
```

**CSS:**

```css
/* Shimmer animation */
@keyframes shimmer {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}

.animate-shimmer {
  animation: shimmer 2s infinite;
}

/* Aspect ratio fix */
.aspect-\[4\/3\] {
  aspect-ratio: 4 / 3;
}
```

### Phase 2 terv (később - NEM ebben a sprint-ben)

```jsx
// Later sprint: WebP support
<picture>
  <source
    srcSet={product.image_variants?.webp}
    type="image/webp"
  />
  <img
    loading="lazy"
    src={product.image_variants?.thumbnail || product.imageUrl}
    alt={product.name}
  />
</picture>
```

**Backend fog támogatni:** `image_variants` JSON field (Phase 2)

### Tesztek

- ✅ lazy-load: képek csak amikor visible-ek
- ✅ Shimmer: loading state animáció
- ✅ Error handling: fallback no-image.svg
- ✅ Aspect ratio: mobile 4:3 fix

---

## Elfogadási kritériumok (mind 3 feature-re)

### Feature 1 (Assembly DnD)
- ✅ @dnd-kit/core + @dnd-kit/sortable npm-ből
- ✅ DndContext + SortableContext beépítve
- ✅ Optimistic UI működik
- ✅ 409 Conflict error handling
- ✅ Undo/redo command pattern
- ✅ Haptic feedback mobilon
- ✅ Keyboard accessibility
- ✅ Unit test: drag-drop logika
- ✅ E2E test: full workflow

### Feature 2 (Filter Persistence)
- ✅ Zustand + persist middleware
- ✅ BroadcastChannel multi-tab sync
- ✅ 300ms debounce save
- ✅ localStorage + sessionStorage fallback
- ✅ 24h expiry check
- ✅ Versioned storage (v2)
- ✅ Test: save/load/sync scenarios

### Feature 3 (Image Lazy-load)
- ✅ Native HTML `loading="lazy"`
- ✅ Shimmer skeleton animation
- ✅ Error state fallback
- ✅ Aspect ratio fix (4:3)
- ✅ Test: lazy-load behavior

---

## Ütemezés

| Feature | Napok | Start | End |
|---------|-------|-------|-----|
| 1. Assembly DnD | 5 | 2026-06-29 | 2026-07-03 |
| 2. Filter Persistence | 2 | 2026-06-29 | 2026-07-01 |
| 3. Image Lazy-load | 3 | 2026-07-02 | 2026-07-04 |

**Párhuzamos:** Backend: MSG-BACKEND-074 (Assembly endpoint)

---

## Referencia

**Konsenzus:** `/opt/spaceos/docs/planning/consensus/2026-06-23_consensus.md`
**Backend task:** `MSG-BACKEND-074`
**Designer:** `MSG-DESIGNER-009` (ha szükséges UI review)
