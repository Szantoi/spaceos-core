---
id: MSG-FRONTEND-017
from: conductor
to: frontend
type: task
priority: critical
status: DONE
model: sonnet
ref: MSG-CONDUCTOR-006, SPEC-002
created: 2026-06-22
content_hash: dc0003df98fec33b7354c47fa71b813df4f86cfb2ff830df92c47f5ae7675b1d
---

# Cutting UI — Feature 1+2: Nesting Visualization + Design→Cutting Workflow

## Kontextus

**Phase 2 Roadmap:** 2026 Q2-Q3 Szabászat modul + 2. ügyfél onboarding.

A Cutting modul backend **production-ready** (964 tests, ALL PASS), a hiányzó komponens a **Frontend UI implementáció**. Ez a feladat a Cutting UI Spec (2026-06-17) Feature 1+2-jét implementálja.

## Feladat

Implementáld a Cutting UI első két feature-ét:

### Feature 1: Nesting Visualization (3-4 nap)

**Cél:** Panel placement grid + drag-drop interaction a nesting result megjelenítéséhez.

**Komponensek:**
- `src/components/cutting/NestingSheet.tsx` — panel placement grid komponens
- `src/components/cutting/PanelPlacement.tsx` — egyedi panel drag-drop elem
- `src/hooks/useCuttingNesting.ts` — API hook (GET /cutting/api/plans/{date}/nesting)

**API integráció:**
- Backend endpoint: `GET /cutting/api/plans/{date}/nesting`
- Response DTO: `NestingResultResponse` (korrigált spec szerint)
  - `sheetId: string`
  - `sheetWidth: number`
  - `sheetHeight: number`
  - `panels: PanelPlacementDto[]`
    - `panelId: string`
    - `x: number`
    - `y: number`
    - `width: number`
    - `height: number`
    - `rotation: number` (0 | 90 | 180 | 270)

**Technikai követelmények:**
1. **NPM dependency:** Telepítsd a `@dnd-kit` library-t (drag-drop):
   ```bash
   cd /opt/spaceos/frontend/joinerytech-portal
   npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
   ```

2. **Grid rendering:** SVG vagy Canvas alapú panel placement grid (sheetWidth × sheetHeight méretű lap)

3. **Drag-drop:** `@dnd-kit` integráció panel mozgatásához (csak vizualizáció, nem mentés)

4. **Unit tests:** Minimum 3 test (`NestingSheet.test.tsx`)
   - Renders panel grid correctly
   - Displays all panels from API response
   - Drag-drop interaction works

**Mock data (development):**
```typescript
const mockNestingResult: NestingResultResponse = {
  sheetId: "sheet-001",
  sheetWidth: 2800,
  sheetHeight: 2070,
  panels: [
    { panelId: "panel-1", x: 0, y: 0, width: 600, height: 800, rotation: 0 },
    { panelId: "panel-2", x: 610, y: 0, width: 400, height: 600, rotation: 90 }
  ]
};
```

### Feature 2: Design→Cutting Workflow (2-3 nap)

**Cél:** "Send to Cutting" button a Design portálon + workflow FSM integráció.

**Komponensek:**
- `src/pages/DesignPage.tsx` — "Send to Cutting" button hozzáadása
- `src/components/workflow/WorkflowStatus.tsx` — workflow status badge komponens
- `src/hooks/useWorkflowTransition.ts` — API hook (POST /cutting/api/send-to-cutting)

**API integráció:**
- Backend endpoint: `POST /cutting/api/send-to-cutting`
- Request:
  ```json
  {
    "orderId": "uuid",
    "designId": "uuid"
  }
  ```
- Response:
  ```json
  {
    "cuttingPlanId": "uuid",
    "status": "cutting_assigned"
  }
  ```

**Workflow FSM:**
```
design_active → cutting_assigned → nesting_complete → execution_planned → completed
```

**Technikai követelmények:**
1. **Button placement:** DesignPage-en "Send to Cutting" button (csak `design_active` státuszban enabled)
2. **Status badge:** `WorkflowStatus` komponens megjelenítése az aktuális workflow state-tel
3. **Nesting result display:** Feature 1 `NestingSheet` komponens integrációja (read-only mode)
4. **Unit tests:** Minimum 3 test
   - "Send to Cutting" button disabled when status != design_active
   - API call triggers workflow transition
   - Nesting result displayed after transition

**Mock data (development):**
```typescript
const mockWorkflowStatus = {
  orderId: "order-001",
  currentState: "design_active",
  transitions: ["send_to_cutting"]
};
```

## Definition of Done (DoD)

- [x] NPM dependency `@dnd-kit` telepítve
- [x] Feature 1: `NestingSheet` + `PanelPlacement` komponensek implementálva
- [x] Feature 1: `useCuttingNesting` hook implementálva (API integráció)
- [x] Feature 1: Unit tests (minimum 3) pass
- [x] Feature 2: "Send to Cutting" button hozzáadva DesignPage-hez
- [x] Feature 2: `WorkflowStatus` badge komponens implementálva
- [x] Feature 2: `useWorkflowTransition` hook implementálva
- [x] Feature 2: Unit tests (minimum 3) pass
- [x] Frontend build sikeres (`npm run build`)
- [x] Nem törnek el létező tesztek (`npm run test`)
- [x] Kód review-ready (clean code, commented if necessary)

## Technikai Notes

### API Endpoint Verifikáció

**DTO Mismatch Risk:** Ha a backend API response NEM egyezik a `NestingResultResponse` spec-kel, azonnal jelezd az outbox-odban. Ellenőrizd:

```bash
# Backend API ellenőrzés (példa)
curl -H "Authorization: Bearer {jwt}" \
  http://localhost:5005/cutting/api/plans/2026-06-22/nesting
```

### Drag-Drop Library Setup

`@dnd-kit` alapú drag-drop példa:

```typescript
import { DndContext, closestCenter } from '@dnd-kit/core';
import { SortableContext, useSortable } from '@dnd-kit/sortable';

function NestingSheet({ panels }: { panels: PanelPlacementDto[] }) {
  return (
    <DndContext collisionDetection={closestCenter}>
      <svg width="2800" height="2070">
        {panels.map(panel => (
          <PanelPlacement key={panel.panelId} panel={panel} />
        ))}
      </svg>
    </DndContext>
  );
}
```

### Workflow State Machine

A backend FSM validációja:
- Csak `design_active` státuszban lehet `send_to_cutting` transition
- Ha invalid state → 400 BadRequest (backend validálja)

## Kapcsolódó Dokumentáció

- **Cutting UI Spec:** `/opt/spaceos/docs/planning/specs/2026-06-17_cuttinguispecdone.md`
- **Phase 2 Planning:** `terminals/conductor/outbox/2026-06-22_001_phase2-cutting-module-planning.md`
- **Backend API:** `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs`

## Következő Lépések

1. Implementáld Feature 1 (Nesting Visualization) — **3-4 nap**
2. Implementáld Feature 2 (Design→Cutting Workflow) — **2-3 nap**
3. Unit tesztek írása mindkét feature-höz — **párhuzamosan**
4. Outbox DONE üzenet Feature 1+2 completion után

---

**Becsült időtartam:** 5-7 nap (szekvenciális)
**Prioritás:** 🔴 CRITICAL — 2. ügyfél MVP blocker
**Dependencies:** Feature 2 függ Feature 1-től (`NestingSheet` komponens újrahasználása)
