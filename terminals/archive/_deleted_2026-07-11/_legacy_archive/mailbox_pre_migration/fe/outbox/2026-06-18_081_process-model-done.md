---
id: MSG-FE-081
from: fe
to: root
type: done
priority: high
status: READ
model: sonnet
ref: MSG-FE-081
created: 2026-06-18
---

# FE-081 — Process Model (folyamat-motor) DONE

## Összefoglaló

Implementálva a folyamat-motor modell és konstansok a `page-process-model.jsx` terv alapján.

## DoD teljesítés

✅ **PROC_PALETTE konstans (8 szín)**
- teal-500, amber-500, violet-500, pink-500, blue-500, green-500, orange-500, indigo-500

✅ **PROC_ACTORS és procActor helper**
- 6 szereplő: manufacturer, supplier, installer, designer, dealer, client
- `procActor(key)` → `{ label, icon, tint }` meta

✅ **SEG_META szegmens típus metaadatok**
- 4 típus: step, branch, parallel, loop
- Minden típushoz: label, icon, color, description

✅ **segId(prefix) egyedi ID generátor**
- Format: `${prefix}-${Date.now()}-${counter}`

✅ **newStep/newBranch/newParallel/newLoop factory-k**
- `newStep(name, actor?)` → StepSegment
- `newBranch(name)` → BranchSegment (2 alapértelmezett út)
- `newParallel(name)` → ParallelSegment (2 alapértelmezett sáv)
- `newLoop(name, targetStepId?)` → LoopSegment

✅ **mapFlow(flow, fn) rekurzív helper**
- Minden szegmentre alkalmazza a függvényt (beleértve nested paths/lanes)

✅ **findSeg(flow, id) keresés**
- Rekurzívan megtalálja a szegmenst ID alapján

✅ **updateSeg/removeSeg id-alapú mutátorok**
- `updateSeg(flow, id, updates)` → módosított flow
- `removeSeg(flow, id)` → szegmens nélküli flow

✅ **insertSeg(flow, newSeg, afterId) beszúró**
- Ha `afterId === null` → elejére beszúr
- Rekurzívan működik nested struktúrákban

✅ **moveSeg(flow, id, dir) fel/le mozgató**
- `direction: 'up' | 'down'`
- Rekurzívan működik nested struktúrákban

✅ **addPath/removePath branch kezelők**
- `addPath(flow, branchId)` → új út hozzáadása
- `removePath(flow, branchId, pathIndex)` → út eltávolítása (min. 2 marad)

✅ **addLane/removeLane parallel kezelők**
- `addLane(flow, parallelId)` → új sáv hozzáadása
- `removeLane(flow, parallelId, laneIndex)` → sáv eltávolítása (min. 2 marad)

✅ **allSteps(flow) gyűjtő**
- Összegyűjti az összes StepSegment-et a flow-ból

✅ **countSegments(flow) statisztika**
- `{ total, byType: { step, branch, parallel, loop } }`

✅ **Minden exportálva window-ra**
- `window.processModel` debug objektum minden exporttal

✅ **npm run build sikeres**

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/models/processModel.ts` | Folyamat-motor modell és helperek (464 sor) |
| `src/models/index.ts` | Re-export barrel fájl |

## Implementáció részletei

### Típusok

```typescript
// Színpaletta
export const PROC_PALETTE = [
  '#14b8a6', '#f59e0b', '#8b5cf6', '#ec4899',
  '#3b82f6', '#22c55e', '#f97316', '#6366f1',
] as const

// Szereplők
export const PROC_ACTORS = [
  'manufacturer', 'supplier', 'installer',
  'designer', 'dealer', 'client',
] as const

// Szegmens típusok
export const SEG_TYPES = ['step', 'branch', 'parallel', 'loop'] as const

// Szegmens interfészek
interface StepSegment { type: 'step'; name; actor?; color?; duration?; instructions? }
interface BranchSegment { type: 'branch'; name; condition?; paths: FlowSegment[][] }
interface ParallelSegment { type: 'parallel'; name; lanes: FlowSegment[][] }
interface LoopSegment { type: 'loop'; name; targetStepId?; maxIterations?; condition? }

type FlowSegment = StepSegment | BranchSegment | ParallelSegment | LoopSegment
type Flow = FlowSegment[]
```

### Actor Meta

| Actor | Label | Icon | Tint |
|---|---|---|---|
| manufacturer | Gyártó | factory | teal |
| supplier | Beszállító | truck | amber |
| installer | Beszerelő | wrench | violet |
| designer | Tervező | ruler | pink |
| dealer | Kereskedő | storefront | blue |
| client | Ügyfél | user | green |

### Segment Meta

| Type | Label | Icon | Color | Description |
|---|---|---|---|---|
| step | Lépés | chevron | stone | Egyszerű művelet |
| branch | Elágazás | route | amber | Feltételes útvonalak |
| parallel | Párhuzam | workflow | blue | Egyidejű sávok |
| loop | Ciklus | bolt | purple | Ismétlődő szakasz |

## Build

```bash
npm run build
```

**Eredmény:**
```
✓ 144 modules transformed.
dist/index.html                     0.45 kB │ gzip:   0.29 kB
dist/assets/index-CO4ms72V.css     93.35 kB │ gzip:  14.87 kB
dist/assets/index-CfTqGpqd.js   1,060.30 kB │ gzip: 240.03 kB
✓ built in 1.19s
```

## Megjegyzések

### Használat

A modell a `ProcessesPanel` komponensben és a jövőbeli ProcessEditor-ban használható:

```typescript
import {
  PROC_PALETTE,
  PROC_ACTORS,
  procActor,
  SEG_META,
  newStep,
  newBranch,
  newParallel,
  newLoop,
  mapFlow,
  updateSeg,
  removeSeg,
  insertSeg,
  moveSeg,
  addPath,
  removePath,
  addLane,
  removeLane,
  allSteps,
  countSegments,
} from '@/models'
```

### Debug

Browser console-ban:
```javascript
window.processModel.newStep('Test lépés', 'manufacturer')
window.processModel.newBranch('Döntés')
window.processModel.allSteps(myFlow)
```

---

**Állapot:** ✅ KÉSZ
**Blocking:** Nincs
