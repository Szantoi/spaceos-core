---
id: SPEC-2026-06-23-DND
title: "Assembly Szerelési Sorrend Drag-and-Drop Átrendezés UI"
type: spec
priority: high
status: IMPLEMENTÁCIÓRA KÉSZ
source_idea: IDEA-20260623-002
assignee: frontend
model: sonnet
created: 2026-06-23
---

# Assembly Szerelési Sorrend Drag-and-Drop Átrendezés UI

## 1. Cél és Scope

**Probléma:** Az `assembly.jsx` szerelési műveletek (BOM machining lines) listája statikus. A dolgozók nem tudják átrendezni a javasolt sorrendet a tényleges munkamenet szerint.

**Megoldás:** Drag-and-drop UI a megmunkálási műveletek (`machLines`) átrendezéséhez, mentés az `app-store.jsx` state-be.

**Scope:**
- `assembly.jsx` — `AssemblyComposer` komponens módosítása
- `app-store.jsx` — opcionális: `machiningOrder` perzisztálása
- dnd-kit library VAGY natív HTML5 Drag API

**Döntés library vs natív:**

| Opció | Előny | Hátrány |
|-------|-------|---------|
| **dnd-kit** | Touch-support, accessibility, smooth animations | +30KB bundle, új dependency |
| **HTML5 Drag API** | Nincs dependency, natív | Mobil touch gyengébb, kevesebb animáció |

**Javaslat:** `dnd-kit` — a JoineryTech prototípus erős mobil fókusszal rendelkezik, a touch-support kritikus.

## 2. Architektúra

### Érintett komponensek

```
/docs/joinerytech/assembly.jsx
  └── AssemblyComposer() komponens (120-250. sor)
        ├── machLines array (136-137. sor) — megmunkálási műveletek
        └── BOM panel JSX (193-235. sor) — itt jelenik meg a lista

/docs/joinerytech/app-store.jsx
  └── (opcionális) machiningOrder state perzisztálás
```

### Jelenlegi adatstruktúra (machLines)

```javascript
// Származtatott a resolveBOM()-ból (117. sor)
const machLines = Object.entries(bom.mach).map(([name, m]) => ({
  ...m,
  mode: machMode[name] || "in"
}));

// Példa eredmény:
[
  { name: "Élzárás + fúrás", unit: "óra", rate: 8200, qty: 1.6, mode: "in" },
  { name: "Szabás", unit: "óra", rate: 7600, qty: 0.6, mode: "in" },
  { name: "Korpusz CNC", unit: "óra", rate: 9500, qty: 2.4, mode: "ext" },
  { name: "Fiók szerelés", unit: "óra", rate: 7800, qty: 4.0, mode: "in" }
]
```

### Javasolt state kiegészítés

```javascript
// AssemblyComposer komponensben
const [machOrder, setMachOrder] = useStateAS(null); // null = alapértelmezett sorrend

// Rendezett machLines
const orderedMachLines = useMemoAS(() => {
  if (!machOrder) return machLines;
  const orderMap = new Map(machOrder.map((name, idx) => [name, idx]));
  return [...machLines].sort((a, b) =>
    (orderMap.get(a.name) ?? 999) - (orderMap.get(b.name) ?? 999)
  );
}, [machLines, machOrder]);
```

## 3. UI/UX Terv

### Drag-and-drop interakció

```jsx
import { DndContext, closestCenter, useSensor, useSensors, PointerSensor, TouchSensor } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

function SortableMachLine({ line }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: line.name,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 50 : 'auto',
  };

  return (
    <div ref={setNodeRef} style={style}
      className={`rounded-lg border p-2.5 flex items-center justify-between gap-2 ${
        isDragging ? 'border-teal-400 bg-teal-50 shadow-lg' : 'border-stone-200'
      }`}>

      {/* Drag handle */}
      <button {...attributes} {...listeners}
        className="w-6 h-6 flex items-center justify-center text-stone-300 hover:text-stone-500 cursor-grab active:cursor-grabbing shrink-0">
        <Icon name="grip" size={14} />
      </button>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="text-[12px] font-medium text-stone-900 truncate">{line.name}</div>
        <div className="text-[10px] text-stone-400">{line.qty.toFixed(1)} óra · {asHuf(line.rate)}/óra</div>
      </div>

      {/* Mode toggle (existing) */}
      <div className="flex items-center gap-1 bg-stone-100 rounded-lg p-0.5 shrink-0">
        {[{ k: "in", l: "Saját" }, { k: "ext", l: "Külső" }].map((it) => (
          <button key={it.k} onClick={() => setMachMode((m) => ({ ...m, [line.name]: it.k }))}
            className={`px-2 h-6 rounded-md text-[10.5px] font-medium ${line.mode === it.k ? "bg-white text-stone-900 shadow-sm" : "text-stone-500"}`}>{it.l}</button>
        ))}
      </div>
    </div>
  );
}

// A BOM panel JSX-ben:
<DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
  <SortableContext items={orderedMachLines.map(l => l.name)} strategy={verticalListSortingStrategy}>
    {orderedMachLines.map((l) => (
      <SortableMachLine key={l.name} line={l} />
    ))}
  </SortableContext>
</DndContext>
```

### Drag-end handler

```javascript
const handleDragEnd = (event) => {
  const { active, over } = event;
  if (!over || active.id === over.id) return;

  const oldIndex = orderedMachLines.findIndex(l => l.name === active.id);
  const newIndex = orderedMachLines.findIndex(l => l.name === over.id);

  const newOrder = arrayMove(orderedMachLines.map(l => l.name), oldIndex, newIndex);
  setMachOrder(newOrder);
};
```

### Tailwind stílusok

**Drag handle:**
```css
w-6 h-6 flex items-center justify-center text-stone-300 hover:text-stone-500
cursor-grab active:cursor-grabbing
```

**Dragging state:**
```css
border-teal-400 bg-teal-50 shadow-lg opacity-50
```

**Drop indicator:**
dnd-kit natívan kezeli a drop pozíció jelzését.

### Mobil support

```javascript
const sensors = useSensors(
  useSensor(PointerSensor, {
    activationConstraint: { distance: 5 }, // kis mozgatás kell az aktiváláshoz
  }),
  useSensor(TouchSensor, {
    activationConstraint: { delay: 150, tolerance: 5 }, // 150ms hold aktiválja
  })
);
```

## 4. State Management

### Lokális state (alapértelmezett)

A sorrend a komponens életciklusában él. Oldal frissítéskor visszaáll az alapértelmezettre.

```javascript
const [machOrder, setMachOrder] = useStateAS(null);
```

### Opcionális: app-store perzisztálás

Ha a sorrend megmaradjon munkamenetek között:

```javascript
// app-store.jsx - új mező a state-ben
machiningOrderOverrides: {} // { "assemblyId": ["művelet1", "művelet2", ...] }

// Akciók
setMachiningOrder(assemblyId, order) → state.machiningOrderOverrides[assemblyId] = order
getMachiningOrder(assemblyId) → state.machiningOrderOverrides[assemblyId] || null
```

**Javaslat:** Első verzióban lokális state elegendő. Perzisztálás later feature.

## 5. Szükséges dependency

```bash
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

**Bundle impact:** ~30KB (gzipped: ~10KB)

## 6. Definition of Done (DoD)

- [ ] dnd-kit library telepítve és importálva
- [ ] `SortableMachLine` komponens létrehozva drag handle-lel
- [ ] `DndContext` wrapper a megmunkálás lista körül
- [ ] Drag-and-drop működik desktop-on (mouse)
- [ ] Drag-and-drop működik mobilon (touch)
- [ ] Átrendezett lista az új sorrendben renderel
- [ ] Meglévő "Saját/Külső" toggle továbbra is működik
- [ ] Nincs vizuális regresszió a BOM panel többi részében
- [ ] Console error-mentes

## 7. Becsült idő

**2-3 óra**

Részletezés:
- dnd-kit setup + import: 20 perc
- SortableMachLine komponens: 45 perc
- DndContext integráció: 30 perc
- Sensors config (touch support): 20 perc
- Stílusok finomhangolás: 30 perc
- Tesztelés (desktop + mobil): 30 perc

## 8. Terminál hozzárendelés

**frontend** — React komponens + external library integráció

## 9. Kockázatok és megjegyzések

1. **CDN vs npm:** A JoineryTech prototípus jelenleg CDN-ről tölti be a React-et. dnd-kit telepítéséhez npm/bundle setup kellhet, vagy CDN fallback (unpkg.com).

2. **Konfiguráció interakció:** A BOM újraszámítódik ha a felhasználó változtat a config-on (korpusz, front, stb.). Ilyenkor a sorrend resetelhető az alapértelmezettre, vagy megtartható ha a műveletek neve egyezik.

3. **Accessibility:** dnd-kit beépített keyboard support-ot ad (Tab + Space/Enter). Ez fontos a11y szempontból.

4. **Alternatíva (kisebb scope):** Ha dnd-kit túl nagy dependency, natív HTML5 Drag API használható, de mobil touch-ra külön implementáció kell (touchstart/touchmove/touchend).

---

*Generálva: 2026-06-23 | Forrás idea: IDEA-20260623-002 | Architect terminál*
