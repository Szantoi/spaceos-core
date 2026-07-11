---
id: MSG-FE-069
from: conductor
to: fe
type: task
priority: high
status: DONE
model: sonnet
ref: docs/tasks/new/SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md
created: 2026-06-18
---

# Production UI Enhancement — Nesting Visualization & Design→Cutting Workflow

## Összefoglaló

A ProductionPage és DesignPage fejlesztése három feature implementációjával:

1. **Nesting Vizualizáció** — ProductionPage SVG canvas
2. **Design→Cutting Workflow** — DesignPage→ProductionPage navigáció
3. **Machine & Operator Scheduling** — @dnd-kit board (BLOKKOLVA: BE endpoint hiányzik)

## Feature-ek prioritás szerint

### Feature 1: Nesting Vizualizáció (ProductionPage) — IMPLEMENTÁCIÓRA KÉSZ

**Cél:** A cutting plan részletek nézet kibővítése SVG nesting layout preview-val.

**Scope:**
- SVG canvas renderelés (1200×800px viewport)
- Lemez megjelenítés (PLYWOOD_BOARD: 2800×2070mm)
- Alkatrészek pozicionálása (`parts[]` `x,y,w,h`)
- Color coding (assigned/unassigned)
- Zoom controls + pan
- Part tooltip (ID, dimensions, material)

**Backend endpoint:** `GET /cutting/api/cutting/plans/{id}` — **KÉSZ** (5004 porton)

**API Contract:**
```typescript
{
  id: UUID,
  board: { width: number, height: number, thickness: number, materialCode: string },
  parts: [{ id: UUID, x: number, y: number, width: number, height: number, assigned: boolean }],
  efficiency: number,
  wasteArea: number
}
```

**DoD:**
- [ ] SVG canvas rendering (viewBox scaling)
- [ ] Board + parts vizualizáció
- [ ] Color coding (green: assigned, red: unassigned)
- [ ] Zoom + pan controls
- [ ] Part tooltip on hover
- [ ] Mock fallback eltávolítás
- [ ] Error state ha API fail

---

### Feature 2: Design→Cutting Workflow — IMPLEMENTÁCIÓRA KÉSZ

**Cél:** DesignPage paraméteres wizard után "Tovább gyártáshoz" gomb → ProductionPage navigáció a megfelelő cutting plan-nel.

**Scope:**
- DesignPage: "Tovább gyártáshoz" action gomb a wizard végén
- Navigation link: `/w/production?planId={generatedPlanId}`
- ProductionPage: query param alapján auto-scroll + highlight a megfelelő plan-hez
- Toast notification: "Cutting plan létrehozva: {planId}"

**Backend endpoint:** Nincs új endpoint szükséges — navigáció csak, a plan ID a wizard válaszából jön.

**DoD:**
- [ ] "Tovább gyártáshoz" gomb DesignPage-en
- [ ] Navigáció ProductionPage-re query param-mal
- [ ] ProductionPage query param alapján auto-scroll + highlight
- [ ] Toast notification

---

### Feature 3: Machine & Operator Scheduling — BLOKKOLVA ⚠️

**Cél:** Drag-and-drop scheduling board (machine × operator × shifts).

**Blocker:** Backend endpoint hiányzik.

**Szükséges endpoint:**
- `GET /cutting/api/machines` — gép lista
- `GET /cutting/api/operators` — operátor lista
- `POST /cutting/api/schedule` — schedule mentés
- `GET /cutting/api/schedule?date={date}` — napi schedule lekérdezés

**DEFER:** Feature 3 implementálása csak akkor, ha backend endpoint elkészül. Jelenlegi scope: Feature 1 + 2.

---

## Implementációs sorrend

1. **Feature 1** — Nesting Vizualizáció (ProductionPage)
   - SVG canvas component
   - API integration (`GET /cutting/api/cutting/plans/{id}`)
   - Mock fallback eltávolítás

2. **Feature 2** — Design→Cutting Workflow
   - DesignPage action gomb
   - Navigation logic
   - ProductionPage query param handling

3. **Feature 3** — SKIP (blokkolt backend endpoint miatt)

---

## Technikai jegyzetek

**Backend service:**
- Cutting modul: `http://127.0.0.1:5004` (systemd: `spaceos-modules-cutting.service`)
- Orchestrator proxy: `http://localhost:3000/cutting/*` (production setup)

**Frontend stack:**
- React 18 + Vite
- SVG.js vagy natív SVG
- React Router v6 (navigation)
- React Query (API calls)

**API base:** `/cutting/api/cutting/*` (via Orchestrator proxy)

**Mock fallback:** `CUTTING_PLANS` mock adatok törlése a `ProductionPage`-ről

---

## Definition of Done

- [ ] Feature 1: SVG nesting vizualizáció működik valódi API-ból
- [ ] Feature 2: Design→Production navigáció működik query param-mal
- [ ] Feature 3: SKIP (backend blocker miatt jelölve)
- [ ] Mock fallback eltávolítva (`CUTTING_PLANS`)
- [ ] Error state handling (API failure)
- [ ] Toast notifications
- [ ] 0 TypeScript error
- [ ] 0 ESLint warning
- [ ] Responsive (desktop minimum, mobile nem kritikus)

---

## Referenciák

- Spec dokumentum: `docs/tasks/new/SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md`
- Cutting API: `backend/spaceos-modules-cutting/` (port 5004)
- ProductionPage: `frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- DesignPage: `frontend/joinerytech-portal/src/pages/DesignPage.tsx`

---

**FE Terminal: Implementáld Feature 1 + 2-t. Feature 3-at skip-eld backend blocker miatt.**

Timestamp: 2026-06-18 05:02 UTC
