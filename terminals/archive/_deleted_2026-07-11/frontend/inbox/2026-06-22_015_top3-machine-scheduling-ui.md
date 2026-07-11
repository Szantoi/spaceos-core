---
id: MSG-FRONTEND-015
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md
created: 2026-06-22
content_hash: cbb1a1fc7d955c4c038f696523cd620eaa6080c081b5dd5f8efff7fecccd9bc1
---

# TOP 3: Machine & Operator Scheduling UI (Drag-and-Drop)

## Kontextus

**Doorstar use-case:** Művezetők reggel drag-and-drop interfésszel rendelik a vágási terveket a gépekhez és operátorokhoz. Jelenleg hardcoded 9 mock kártya van.

**Cél:** Valódi drag-and-drop batch assignment board, API integráció, RBAC.

## Feladat

Implementálj egy drag-and-drop alapú batch assignment board-ot, amely lehetővé teszi vágási tervek gépekhez rendelését.

### ⚠️ FÜGGŐSÉG

**Backend endpoint:** `POST /cutting/api/plans/{date}/assign-batch` (MSG-BACKEND-022)
**Status:** ⏳ PENDING — ezt a frontend előtt implementálni kell!

**Backend endpoint:** `GET /identity/users?role={role}` (MSG-BACKEND-023)
**Status:** ⏳ PENDING — opcionális, fallback mock működik nélkül is

### Implementációs scope

#### 1. Dependency: @dnd-kit Library Telepítés

**⚠️ ELŐFELTÉTEL:**
```bash
pnpm add @dnd-kit/core @dnd-kit/sortable
```

Ellenőrizd: `package.json` dependencies — jelenleg nincs telepítve.

#### 2. ProductionPage Machining Tab Redesign

**Fájl:** `src/pages/ProductionPage.tsx`

**Változtatások:**
- Lecserélni a jelenlegi 3 oszlopos mock grid-et → `<BatchAssignmentBoard />`

#### 3. BatchAssignmentBoard Komponens (ÚJ)

**Fájl:** `src/components/machining/BatchAssignmentBoard.tsx`

**Layout:**
- 4 oszlopos grid:
  - Oszlop 1 (bal): **Unassigned batches** (drag source)
  - Oszlop 2-4 (jobb): **CNC machines • Edgebanding • QC stations** (drop targets)

**API integráció:**
- `GET /cutting/api/plans?status=Draft` — unassigned batches fetch
- `GET /api/tools/workstations` — machine list (már létezik, FE-kód használja MachineParkPanel-ben)
- `POST /cutting/api/plans/{date}/assign-batch` — drop callback

**Drag-Drop Flow:**
1. User drag-eli a batch card-ot az Unassigned oszlopból
2. Drop-olja egy machine oszlopba
3. Megjelenik a BatchCard assign form (operator, time, priority)
4. "Hozzárendelés" gomb → `POST /cutting/api/plans/{date}/assign-batch`
5. Success → batch átkerül az Assigned állapotba

#### 4. BatchCard Komponens (ÚJ)

**Fájl:** `src/components/machining/BatchCard.tsx`

**Props:**
```typescript
interface BatchCardProps {
  batch: {
    id: string;
    materialType: string;
    quantity: number;
    assignedOperator?: string;
    assignedMachine?: string;
    startTime?: string;
    priority?: number;
    status: "Draft" | "Planned" | "InProgress" | "Completed";
  };
  mode: "unassigned" | "assigned";
}
```

**UI (unassigned mode):**
- Header: batch ID (pl. "CP-184-A"), material chip (amber badge: "Bükk"), qty badge (pl. "14 db")
- Body:
  - Operator autocomplete: `GET /identity/users?role=machine_operator` (fallback mock ha endpoint nincs)
  - Time picker: `startTime` (default: now)
  - Priority slider: 1-10 (default: 5)
  - "Hozzárendelés" button → `POST /cutting/api/plans/{date}/assign-batch`

**UI (assigned mode):**
- Header: ugyanaz
- Body:
  - Assigned operator name, start time, priority chip
  - FSM quick actions (executor jogosultsággal):
    - "Indítás" button → `POST /cutting/api/execution/{id}/start`
    - "Befejezés" button → `POST /cutting/api/execution/{id}/complete`

#### 5. RBAC Ellenőrzés

**Fájl:** `src/hooks/useAuth.ts`

**Változtatások:**
- `useAuth()` hook: current user `role` check
- Ha NEM `machine_operator` vagy `production_manager` → assign form disabled, csak read-only view
- FSM action buttons: csak assigned operator vagy manager látja

### DoD

- [ ] `@dnd-kit/core` telepítve (package.json)
- [ ] Backend `POST /cutting/api/plans/{date}/assign-batch` deployed ✅ (MSG-BACKEND-022)
- [ ] ProductionPage machining tab → drag-drop batch assignment board
- [ ] BatchCard: operator autocomplete, time picker, priority slider, assign button
- [ ] FSM quick actions: Start / Complete buttons (RBAC-based visibility)
- [ ] +10 FE teszt pass:
  - `BatchAssignmentBoard.test.tsx`: 6 teszt (drag-drop flow, API hívások)
  - `BatchCard.test.tsx`: 4 teszt (form validation, RBAC disabled state, FSM action buttons)
- [ ] `pnpm test` pass (742+ tests)
- [ ] `pnpm build` 0 error

### Backend API ellenőrzés

**Endpoint 1:** ⏳ PENDING — `POST /cutting/api/plans/{date}/assign-batch` (MSG-BACKEND-022)
**Endpoint 2:** ⏳ PENDING — `GET /identity/users?role={role}` (MSG-BACKEND-023, opcionális)
**Endpoint 3:** ✅ READY — `GET /api/tools/workstations` (már létezik)

### Implementációs irányelvek

**@dnd-kit Best Practices:**
- `DndContext` provider wrapper a board körül
- `useDraggable` hook: unassigned batch cards
- `useDroppable` hook: machine columns
- `onDragEnd` callback: API call + state update

**RBAC Pattern:**
- `useAuth()` hook: `hasRole('machine_operator')` utility
- Conditional rendering: `{canAssign && <AssignButton />}`

**Kockázatok:**
- ⚠️ `GET /identity/users?role=machine_operator` endpoint NEM létezik → backend kiegészítés kell (MSG-BACKEND-023)
- ⚠️ Drag-drop library nincs a package.json-ben → `@dnd-kit/core` telepítés kell
- ⚠️ FSM transition jogosultság (start/complete) nem tisztázott → RBAC review szükséges backend oldalon

### Kapcsolódó fájlok

- `frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- `frontend/joinerytech-portal/src/components/machining/BatchAssignmentBoard.tsx` (ÚJ)
- `frontend/joinerytech-portal/src/components/machining/BatchCard.tsx` (ÚJ)
- `frontend/joinerytech-portal/src/hooks/useAuth.ts`
- `frontend/joinerytech-portal/package.json` (dependencies)

### Becsült idő

**4-5 nap** (1 nap backend előfeltétel + drag-drop lib integráció + RBAC logic + 10 teszt)

---

**Priority:** HIGH — Doorstar workflow törött pont #3, üzemvezetői reggeli rituálé digitalizálása
**Ref:** `/opt/spaceos/docs/tasks/archive/CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md`
**Blocking:** MSG-BACKEND-022 (assign-batch endpoint)
