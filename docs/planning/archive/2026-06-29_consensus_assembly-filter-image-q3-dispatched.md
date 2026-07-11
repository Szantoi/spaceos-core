```yaml
---
created: 2026-06-23
plan_a: /opt/spaceos/docs/planning/plans/2026-06-23_plan-a.md
plan_b: /opt/spaceos/docs/planning/plans/2026-06-23_plan-b.md
status: ready_for_conductor
---
```

# SpaceOS Konsenzus Implementációs Terv

## Összefoglalás

A konsenzus hibrid megközelítést javasol: **üzleti érték szerinti prioritás** (Assembly drag-drop először) **reális időbecslésekkel** (Plan-A), kombinálva **progresszív technológiai megoldásokkal** (Plan-B). A 3 feature párhuzamos track-ekben fejleszthető: Assembly backend+frontend (5 nap), Filter perzisztencia (2 nap), Képoptimalizálás CDN nélküli alap + jövőbeli WebP enhancement (3+2 nap).

## Elfogadott prioritás sorrend

1. **Assembly Drag-and-Drop** — legmagasabb üzleti érték (munkafolyamat optimalizálás), mindkét tervező egyetért a technológiában
2. **Katalógus Filter Perzisztencia** — gyors win, zero backend dependency, tanulási platform localStorage pattern-ekhez
3. **Katalógus Képoptimalizálás** — azonnali UX javulás natív lazy-load-dal, későbbi CDN/WebP enhancement

**Indoklás:** Plan-B üzleti prioritása + Plan-A kockázatkezelése. Assembly-t production-be kell tolni a leggyorsabban user feedback-ért, míg képoptimalizálás iteratívan bővíthető.

## Backend szükségletek (összesített)

### Assembly Drag-and-Drop (prioritás: 1)
```
PATCH /api/v1/work-orders/{id}/assembly-sequence
Body: { 
  operations: [
    { id: "op-1", sequence: 1 }, 
    { id: "op-2", sequence: 2 }
  ],
  timestamp: "2026-06-23T10:30:00Z"  // Conflict detection
}
Response: { 
  updated_operations: [...], 
  estimated_duration_change: "+15min" 
}
```

**Adatmodell:**
```csharp
public class WorkOrderOperation {
  public string Id { get; set; }
  public int Sequence { get; set; }          // ÚJ - migration szükséges
  public DateTime LastModified { get; set; }  // ÚJ - conflict detection
  public string Description { get; set; }
  public TimeSpan EstimatedDuration { get; set; }
}
```

**Validációk:**
- Minden operation.id létezik a work order-ben
- Sequence számok folytonosak (gap detection)
- Work order státusz != "completed"
- Timestamp-based optimistic locking (409 Conflict egyidejű szerkesztésnél)
- **Később:** Business rule validation (pl. "festés" nem lehet "alapozás" előtt)

### Filter Perzisztencia (prioritás: 2)
**Backend:** ❌ Nincs immediate szükség
**Jövőbeli endpoint (Phase 2):**
```
GET/PATCH /api/users/me/preferences
Body: { catalog_filters: {...}, view_mode: "grid" }
```

### Képoptimalizálás (prioritás: 3)
**Phase 1:** ❌ Backend módosítás nélkül (natív lazy-load)
**Phase 2 (later sprint):**
```
POST /api/catalog/items/{id}/image/upload
→ Automatikus WebP generálás + thumbnail
Response: { original, thumbnail, webp }
```

**Adatmodell kiegészítés (Phase 2):**
```sql
ALTER TABLE CatalogItems ADD COLUMN image_variants JSONB;
-- { "thumb": "cdn.url/thumb.jpg", "webp": "cdn.url/image.webp" }
```

## Frontend megközelítés (legjobb elemek)

### 1. Assembly Drag-and-Drop (5 nap)

**Library:** `@dnd-kit/core` + `@dnd-kit/sortable` (Plan-A + Plan-B konszenzus)

**Komponens:**
```jsx
// assembly-operations-list.jsx
import { DndContext, useSensor, PointerSensor, KeyboardSensor } from '@dnd-kit/core';

const sensors = [
  useSensor(PointerSensor, { 
    activationConstraint: { distance: 8 }  // Plan-B: touch scroll vs drag fix
  }),
  useSensor(KeyboardSensor)  // Plan-B: A11y support
];

function AssemblyOperationsList() {
  const [operations, setOperations] = useState([]);
  const [history, setHistory] = useState([]);  // Plan-B: undo support
  
  const handleDragEnd = async (event) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    
    // Plan-A: Optimistic UI
    const reorderedOps = reorderArray(operations, active.id, over.id);
    setOperations(reorderedOps);
    
    // Plan-B: Command pattern
    const command = {
      execute: () => reorderedOps,
      undo: () => operations,
      timestamp: Date.now()
    };
    setHistory([...history, command]);
    
    // API call
    try {
      await api.patch(`/work-orders/${id}/assembly-sequence`, {
        operations: reorderedOps.map((op, idx) => ({ id: op.id, sequence: idx + 1 })),
        timestamp: new Date().toISOString()
      });
      // Plan-B: Haptic feedback
      if ('vibrate' in navigator) navigator.vibrate([5, 50, 5]);
    } catch (error) {
      // Plan-A: Rollback on error
      setOperations(operations);
      setHistory(history.slice(0, -1));
      toast.error('Mentés sikertelen - változtatások visszavonva');
    }
  };
  
  return (
    <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
      <SortableContext items={operations} strategy={verticalListSortingStrategy}>
        {operations.map(op => <SortableOperation key={op.id} operation={op} />)}
      </SortableContext>
    </DndContext>
  );
}
```

**Plan-B gesture innovation + Plan-A error handling**

---

### 2. Filter Perzisztencia (2 nap)

**Store:** Plan-A versioned storage + Plan-B BroadcastChannel sync

```javascript
// app-store.jsx
const FILTER_VERSION = 2;
const bc = new BroadcastChannel('spaceos_filters');

const useCatalogStore = create(
  persist(
    (set, get) => ({
      filters: {},
      setFilters: (filters) => {
        set({ filters });
        // Plan-A: Debounced save
        clearTimeout(get().saveTimeout);
        const timeout = setTimeout(() => {
          try {
            // Plan-B: Compression ha nagy
            const data = JSON.stringify({ filters, timestamp: Date.now() });
            localStorage.setItem(`spaceos_catalog_v${FILTER_VERSION}`, data);
            // Plan-B: Multi-tab sync
            bc.postMessage({ type: 'FILTER_UPDATE', filters });
          } catch (e) {
            // Plan-A: Quota fallback
            sessionStorage.setItem(`spaceos_catalog_v${FILTER_VERSION}`, data);
          }
        }, 300);
        set({ saveTimeout: timeout });
      },
      loadFilters: () => {
        const stored = localStorage.getItem(`spaceos_catalog_v${FILTER_VERSION}`);
        if (!stored) return null;
        const { filters, timestamp } = JSON.parse(stored);
        // Plan-A: 24h expiry
        if (Date.now() - timestamp > 86400000) return null;
        return filters;
      }
    }),
    { name: 'spaceos_catalog', version: FILTER_VERSION }
  )
);

// Plan-B: Listen to other tabs
bc.onmessage = (event) => {
  if (event.data.type === 'FILTER_UPDATE') {
    useCatalogStore.setState({ filters: event.data.filters });
  }
};
```

---

### 3. Képoptimalizálás (3 nap Phase 1 + 2 nap Phase 2)

**Phase 1 - Natív megoldás (Plan-A alapon):**
```jsx
// ProductCard.jsx
const [imageState, setImageState] = useState('loading');

<div className="relative aspect-[4/3]">
  {imageState === 'loading' && <SkeletonLoader />}
  <img 
    loading="lazy"
    src={product.imageUrl || '/assets/no-image.svg'}
    onLoad={() => setImageState('loaded')}
    onError={() => setImageState('error')}
    className={cn(
      'transition-opacity duration-300',
      imageState === 'loaded' ? 'opacity-100' : 'opacity-0'
    )}
  />
  {imageState === 'error' && <NoImageBadge />}
</div>
```

**Phase 2 - WebP Enhancement (Plan-B progresszív megoldás):**
```jsx
// Later sprint - amikor CDN ready
<picture>
  <source srcSet={product.image_variants?.webp} type="image/webp" />
  <img 
    loading="lazy"
    src={product.image_variants?.thumbnail || product.imageUrl}
    alt={product.name}
  />
</picture>
```

## Amit Plan-A-tól veszünk át

- ✅ **Reális időbecslések:** 3-5 napos featureonként sprint buffer-rel
- ✅ **Explicit kockázatkezelés:** Safari polyfill, localStorage quota, multi-tab sync edge case-ek
- ✅ **Validáció részletesség:** Assembly sequence gap detection, work order status check
- ✅ **Debounce pattern:** 300ms filter save késleltetés
- ✅ **Aspect-ratio CSS:** Mobile layout bugok elkerülése
- ✅ **Versioned localStorage:** Cache invalidáció + timestamp expiry (24h)
- ✅ **Optimistic UI rollback:** API hiba esetén state visszaállítás + user feedback

## Amit Plan-B-től veszünk át

- ✅ **BroadcastChannel API:** Multi-tab real-time filter sync (kritikus multi-monitor setup-nál)
- ✅ **Command Pattern:** Undo/redo support assembly reordernél
- ✅ **Gesture-first DnD:** PointerSensor distance constraint + KeyboardSensor A11y
- ✅ **Haptic feedback:** Natív mobil vibráció drag-drop event-ekre
- ✅ **Progressive enhancement path:** WebP phase 2 terv (future-proof)
- ✅ **Estimated duration change response:** Backend kalkulálja az időbecslés változást átrendezéskor
- ✅ **Timestamp-based conflict detection:** 409 Conflict optimistic locking-hoz

## Nyitott kérdések a Conductor-nak

### 1. Infrastruktúra & DevOps
- **CD