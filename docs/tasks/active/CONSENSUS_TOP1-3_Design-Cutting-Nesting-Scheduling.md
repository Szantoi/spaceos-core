---
created: 2026-06-16
status: ready_for_conductor
priority: high
domain: manufacturing
---

# SpaceOS Konsenzus Implementációs Terv

## Összefoglalás

A kiválasztott TOP 3 fejlesztés a Doorstar napi workflow törött pontjait oldja meg, backend infrastruktúra már rendelkezésre áll. Fókusz: Design’Cutting integráció (workflow kapocs), nesting vizualizáció (anyaghatékonyság láthatóvá tétele), machine scheduling UI (üzemvezetQ reggeli rituálé digitalizálása). Teljes implementáció: ~1012 nap FE, 1 nap BE.

## Elfogadott prioritás sorrend

1. **TOP 1: Design ’ Cutting Plan Workflow (003)**  DesignPage Step 4 submit ’ valódi `POST /cutting/api/sheets`, ProductionPage auto-navigation + highlight
2. **TOP 2: Nesting Vizualizáció (001)**  ProductionPage nesting viewer ’ `GET /cutting/api/cutting/sheets/{id}/nesting`, SVG canvas + waste % badge
3. **TOP 3: Machine & Operator Scheduling UI (002)**  ProductionPage machining tab redesign: drag-drop batch assignment, új BE endpoint `POST /cutting/api/plans/{date}/assign-batch`

**Indoklás:** TOP 1 létrehozza a workflow integráció kapocsot  amint a design’cutting link él, a TOP 2 nesting vizualizáció azonnal gazdagabb kontextusban jelenik meg (az adott plan-hoz látjuk a waste %-ot). TOP 3 a legnehezebb (új BE endpoint + drag-drop), ezért utolsó.

---

## TOP 1: Design ’ Cutting Plan Workflow

### Backend szükségletek

**Nincs új backend munka**  az endpoint már létezik és tesztelt:
- `POST /cutting/api/sheets` (SubmitCuttingSheet)  931/931 teszt, deployed
- Request body: `{ orderReference: string, templateId: string, calculatedParts: [...] }`
- Response: `{ sheetId: string, cuttingPlanId: string }`

### Frontend megközelítés

**Fájlok:**
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.tsx`
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.tsx`

**Implementáció:**

1. **DesignPage Step 4 submit logika (jelenleg mock):**
   - Lecserélni a mock `cuttingPlanId` generálást valódi API hívásra
   - `useApi()` hook: `POST ${API_BASE.cutting}/api/sheets`
   - Body: `{ orderReference: currentOrderRef, templateId: selectedTemplate.id, calculatedParts: cuttingList }`
   - Response: `{ sheetId, cuttingPlanId }` ’ state-be mentés
   - Success callback: `navigate('/w/production/cutting', { state: { highlightPlanId: cuttingPlanId } })`

2. **ProductionPage auto-scroll + highlight:**
   - `useLocation()` hook: `state.highlightPlanId` olvasás
   - Ha van `highlightPlanId`:
     - Auto-select: `setSelectedPlan(highlightPlanId)`
     - Scroll to row: `ref.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })`
     - Visual highlight: 3s-ig border-teal-500 animáció a kiválasztott sorban
   - Plan sorban: customer name + order context (design template back-link) hozzáadása (jelenleg "")

3. **Breadcrumb UI (opcionális, alacsony prioritás):**
   - DesignPage Step 4 button szöveg: "Terv létrehozása és tovább a Gyártásba" (jelenlegi: csak "Terv létrehozása")
   - ProductionPage toast notification (3s): "Új vágási terv: {planName}  {templateName}"

**Tesztlefedettség:**
- DesignPage.test.tsx: mock API válasz, navigate hívás ellenQrzése
- ProductionPage.test.tsx: location state alapú highlight, scroll ref ellenQrzése

**Implementációs komplexitás:** 23 nap FE (0 backend)

**Kockázatok:**
-   `cuttingList` formátum nem egyezik az API elvárással ’ validation réteg kell a submit elQtt
-   Ha a `POST /cutting/api/sheets` hiba (409 duplicate, 400 validation) ’ user-friendly error message kell, fallback mock-ra NINCS

---

## TOP 2: Nesting Vizualizáció

### Backend szükségletek

**Nincs új backend munka**  az endpoint már létezik és tesztelt:
- `GET /cutting/api/cutting/sheets/{id}/nesting`  931/931 teszt, deployed
- Response: `{ sheets: [{ id, width, height, placedParts: [{ x, y, width, height, materialType, partId }], wastePercentage }], strategy: "Guillotine" | "FFDH" }`

### Frontend megközelítés

**Fájlok:**
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- Új komponens: `/opt/spaceos/frontend/joinerytech-portal/src/components/nesting/NestingViewer.tsx`

**Implementáció:**

1. **ProductionPage nesting viewer (jelenleg "Nesting API nem elérhetQ" mock üzenet):**
   - `useApi()` hook: `GET ${API_BASE.cutting}/api/cutting/sheets/${selectedPlan}/nesting`
   - Ha API elérhetQ ’ `<NestingViewer data={nestingData} />` renderelés
   - Ha API hiba ’ fallback: jelenlegi mock üzenet megmarad

2. **NestingViewer komponens (új):**
   - Props: `{ data: NestingResultDto }`
   - SVG canvas:
     - Viewport: auto-scale a legnagyobb sheet méretére (pl. 2800×2070mm ’ 700×517px, 4:1 scale)
     - Background: light gray rectangle (panel)
     - Placed parts: colored rectangles (CATALOG_LOOKUP szerinti szín kódolás  már létezik a kódbázisban)
     - Hover tooltip: part ID, dimensions, material type
   - Stats badge (top-right corner):
     - Waste %: `{wastePercentage.toFixed(1)}%` (red if >15%, yellow if 1015%, green if <10%)
     - Strategy: `{strategy}` pill
     - Sheets count: `{sheets.length} lap`
   - Per-sheet toggle (ha `sheets.length > 1`): previous/next buttons, sheet indicator (pl. "2 / 3")
   - Material type filter dropdown (opcionális): CATALOG_LOOKUP unique values

3. **CATALOG_LOOKUP integráció:**
   - Check: `/opt/spaceos/frontend/joinerytech-portal/src/mocks/data.ts`  ha nincs `CATALOG_LOOKUP`, létrehozni mock színekkel (Bükk: amber, MDF: stone, Tölgy: brown, stb.)

**Tesztlefedettség:**
- NestingViewer.test.tsx: SVG rendering, scale calculation, color mapping, sheet navigation, stats badge
- ProductionPage.test.tsx: API hívás, fallback logic

**Implementációs komplexitás:** 34 nap FE (0 backend)  SVG rendering + scale logic a fQ munka

**Kockázatok:**
-   API response schema változás (pl. `placedParts` helyett `placements`) ’ strict TypeScript interface kell
-   SVG performance ha >100 part egy sheet-en ’ virtualizáció NEM szükséges Doorstar skálán (max 3040 part/sheet)

---

## TOP 3: Machine & Operator Scheduling UI

### Backend szükségletek

**1 új endpoint szükséges:**

**Endpoint:** `POST /cutting/api/plans/{date}/assign-batch`

**Request body:**
```typescript
{
  batchId: string         // Cutting plan ID
  machineId: string       // Workstation UUID (Kernel tools)
  operatorId: string      // User UUID (Keycloak tenantId attr)
  priority: number        // 110 scale
  startTime: string       // ISO8601 timestamp
}
```

**Response:**
```typescript
{
  executionId: string     // CuttingExecution aggregátum UUID
  status: "Planned"       // FSM státusz
}
```

**Backend implementációs terv:**
- Modul: `backend/spaceos-modules-cutting/`
- Handler path: `src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs`
- Domain: `CuttingExecution` aggregátum már létezik (ScheduleExecution command)  ezt kell kiterjeszteni `machineId` + `operatorId` paraméterekkel
- FSM transition: `Draft ’ Planned` (már létezik)
- RBAC: `machine_operator` role check (Keycloak)
- Idempotencia: `batchId` + `date` unique constraint (PostgreSQL)

**Tesztek:**
- Integration test: `CuttingPlanningEndpoints.AssignBatch_ValidRequest_ReturnsExecutionId`
- FSM test: `CuttingExecution.ScheduleWithAssignment_TransitionsToPlanndState`
- RBAC test: `AssignBatch_NonOperatorRole_Returns403`

**Implementációs komplexitás:** 1 nap BE (handler + domain logic + 3 teszt)

### Frontend megközelítés

**Fájlok:**
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- Új komponens: `/opt/spaceos/frontend/joinerytech-portal/src/components/machining/BatchAssignmentBoard.tsx`
- Új komponens: `/opt/spaceos/frontend/joinerytech-portal/src/components/machining/BatchCard.tsx`

**Implementáció:**

1. **ProductionPage machining tab redesign (jelenleg hardcoded 9 mock kártya):**
   - Lecserélni a jelenlegi 3 oszlopos mock grid-et ’ `<BatchAssignmentBoard />`

2. **BatchAssignmentBoard komponens (új):**
   - Layout: 4 oszlopos grid
     - Oszlop 1 (bal): **Unassigned batches** (drag source)
     - Oszlop 24 (jobb): **CNC machines · Edgebanding · QC stations** (drop targets)
   - Drag-drop library: `@dnd-kit/core` (már használt a kódbázisban? check package.json)
   - API integráció:
     - `GET /cutting/api/plans?status=Draft`  unassigned batches fetch
     - `GET /api/tools/workstations`  machine list (már létezik, FE-kód használja MachineParkPanel-ben)
     - `POST /cutting/api/plans/{date}/assign-batch`  drop callback

3. **BatchCard komponens (új):**
   - Props: `{ batch: CuttingPlan, mode: 'unassigned' | 'assigned' }`
   - UI:
     - Header: batch ID (pl. "CP-184-A"), material chip (amber badge: "Bükk"), qty badge (pl. "14 db")
     - Body (csak 'unassigned' mode-ban):
       - Operator autocomplete: `GET /identity/users?role=machine_operator` (Identity modul API  check létezik-e)
       - Time picker: `startTime` (default: now)
       - Priority slider: 110 (default: 5)
       - "Hozzárendelés" button ’ `POST /cutting/api/plans/{date}/assign-batch`
     - Body (csak 'assigned' mode-ban):
       - Assigned operator name, start time, priority chip
       - FSM quick actions (executor jogosultsággal): Start / Pause / Complete buttons
         - `POST /cutting/api/execution/{id}/start`
         - `POST /cutting/api/execution/{id}/complete`

4. **RBAC ellenQrzés:**
   - `useAuth()` hook: current user `role` check
   - Ha NEM `machine_operator` vagy `production_manager` ’ assign form disabled, csak read-only view

**Tesztlefedettség:**
- BatchAssignmentBoard.test.tsx: drag-drop flow, API hívások
- BatchCard.test.tsx: form validation, RBAC disabled state, FSM action buttons

**Implementációs komplexitás:** 45 nap FE (1 nap BE)  drag-drop lib integráció + RBAC logic a fQ munka

**Kockázatok:**
-   `GET /identity/users?role=machine_operator` endpoint NEM létezik ’ backend kiegészítés kell (Identity modul)
-   Drag-drop library nincs a package.json-ben ’ `@dnd-kit/core` telepítés kell
-   FSM transition jogosultság (start/complete) nem tisztázott ’ RBAC review szükséges backend oldalon

---

## Összesített backend szükségletek

| Modul | Endpoint | Metódus | Státusz | Munka |
|---|---|---|---|---|
| Cutting | `/api/sheets` | POST |  Létezik | 0 nap |
| Cutting | `/api/cutting/sheets/{id}/nesting` | GET |  Létezik | 0 nap |
| Cutting | `/api/plans/{date}/assign-batch` | POST | L Új | 1 nap |
| Identity | `/users?role={role}` | GET |   EllenQrizendQ | 0.5 nap (ha hiányzik) |

**Teljes backend munka:** 11.5 nap (csak TOP 3, opcionálisan Identity query endpoint)

---

## Frontend megközelítés összesítése

| Funkció | Érintett fájlok | API hívások | Új komponensek | Munka |
|---|---|---|---|---|
| TOP 1 | DesignPage.tsx, ProductionPage.tsx | POST /cutting/api/sheets | 0 | 23 nap |
| TOP 2 | ProductionPage.tsx | GET /cutting/api/cutting/sheets/{id}/nesting | NestingViewer.tsx | 34 nap |
| TOP 3 | ProductionPage.tsx | POST /cutting/api/plans/{date}/assign-batch, GET /identity/users | BatchAssignmentBoard.tsx, BatchCard.tsx | 45 nap |

**Teljes frontend munka:** 912 nap

**Teszt növekmény:**
- DesignPage.test.tsx: +3 teszt (submit flow)
- ProductionPage.test.tsx: +5 teszt (navigation, nesting API, assign board)
- NestingViewer.test.tsx: +8 teszt (új)
- BatchAssignmentBoard.test.tsx: +6 teszt (új)
- BatchCard.test.tsx: +4 teszt (új)
- **Összesen:** +26 FE teszt (~768 teszt ’ 794 teszt)

---

## Kockázatok és függQségek

### Kockázatok (prioritás szerint)

1. **=4 KRITIKUS: DesignPage `cuttingList` formátum nem egyezik a Cutting API elvárással**
   - EllenQrizni: `/opt/spaceos/backend/spaceos-modules-cutting/src/.../SubmitCuttingSheetCommand.cs` request DTO
   - EllenQrizni: `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.tsx` `cuttingList` state struktúra
   - Ha nem egyezik ’ validation/mapping réteg kell a FE submit elQtt

2. **=á KÖZEPES: Identity modul `GET /users?role={role}` endpoint hiánya**
   - EllenQrizni: `/opt/spaceos/backend/spaceos-modules-identity/src/.../Endpoints/`  van-e role-alapú user query
   - Ha nincs ’ TOP 3 késik 0.5 napot (Identity backend endpoint hozzáadása)

3. **=á KÖZEPES: Drag-drop library (`@dnd-kit/core`) nincs telepítve**
   - EllenQrizni: `/opt/spaceos/frontend/joinerytech-portal/package.json` dependencies
   - Ha nincs ’ `pnpm add @dnd-kit/core @dnd-kit/sortable` (5 perc)

4. **=â ALACSONY: Nesting API response schema változás**
   - API 931 teszttel protected, schema change alacsony valószínqségq
   - TypeScript interface strict typing + runtime validation (Zod?) lefedi

5. **=â ALACSONY: FSM transition jogosultság (start/complete) RBAC policy**
   - Cutting modul már használ RS256 JWT + RBAC
   - EllenQrizni: `CuttingExecutionEndpoints.cs`  `/start`, `/complete` endpoint-ok RBAC annotációja

### FüggQségek (implementációs sorrend)

```
TOP 1 elQfeltételek: NINCS (azonnal indítható)
TOP 2 elQfeltételek: NINCS (azonnal indítható, de TOP 1-gyel együtt nagyobb érték)
TOP 3 elQfeltételek:
  - Backend: POST /cutting/api/plans/{date}/assign-batch (1 nap)
  - Backend (opcionális): GET /identity/users?role={role} (0.5 nap)
  - Frontend: @dnd-kit/core telepítés (5 perc)
```

**Kritikus útvonal:** TOP 1 + TOP 2 párhuzamosan ’ TOP 3 (backend után)

---

## Nyitott kérdések (PRE-IMPLEMENTATION)

Ezeket a FE terminál session-start elQtt kell tisztázni:

1. **DesignPage `cuttingList` vs. Cutting API `calculatedParts` mapping:**
   - Q: Milyen formátumban tárolja a DesignPage a `cuttingList` state-et? (JSON schema)
   - Q: Milyen formátumot vár a `POST /cutting/api/sheets` endpoint? (DTO check)
   - Tisztázandó: `/opt/spaceos/backend/spaceos-modules-cutting/src/.../SubmitCuttingSheetCommand.cs` + `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.tsx` (lines 239, 279)

2. **Identity modul role-based user query:**
   - Q: Létezik-e `GET /identity/users?role={role}` endpoint?
   - Q: Ha nem, akkor hogyan szqrjünk role szerint (pl. Keycloak direct query, vagy Kernel proxy)?
   - Tisztázandó: `/opt/spaceos/backend/spaceos-modules-identity/src/.../Endpoints/` grep "MapGet.*users"

3. **Cutting API nesting response CATALOG_LOOKUP mapping:**
   - Q: A `GET /cutting/api/cutting/sheets/{id}/nesting` response tartalmaz-e `materialType` enumot?
   - Q: Ez az enum egyezik-e a frontend `CATALOG_LOOKUP` kulcsaival?
   - Tisztázandó: `/opt/spaceos/backend/spaceos-modules-cutting/src/.../GetNestingResultQuery.cs` response DTO

4. **Drag-drop library választás:**
   - Q: Van-e már `@dnd-kit` a package.json-ben (más komponens használja)?
   - Q: Ha nincs, akkor `@dnd-kit/core` vagy `react-beautiful-dnd` (deprecated)?
   - Tisztázandó: `/opt/spaceos/frontend/joinerytech-portal/package.json` grep "dnd"

5. **FSM transition RBAC policy:**
   - Q: Ki hívhatja a `POST /cutting/api/execution/{id}/start` endpoint-ot? (role: `machine_operator` + `production_manager`?)
   - Q: Ki hívhatja a `POST /cutting/api/execution/{id}/complete` endpoint-ot? (csak az assigned operator? vagy manager override?)
   - Tisztázandó: `/opt/spaceos/backend/spaceos-modules-cutting/src/.../CuttingExecutionEndpoints.cs` RBAC annotációk

---

## KövetkezQ lépések (conductor számára)

1. **PRE-IMPLEMENTATION ellenQrzések futtatása:**
   - Root terminál: 5 nyitott kérdés tisztázása (grep + file check)
   - Eredmény: frissített konsenzus vagy BLOCKED üzenet az érintett terminálnak

2. **Backend track kiosztása (CUTTING terminál):**
   - Task: `CUTTING-V2_assign-batch-endpoint.md` (new/)
   - Scope: `POST /cutting/api/plans/{date}/assign-batch` endpoint + teszt
   - DoD: 3 teszt (integration, FSM, RBAC), API deployed
   - Becsült idQ: 1 nap
   - Opcionális: Identity modul kiegészítés (ha `GET /users?role={role}` hiányzik)

3. **Frontend track kiosztása (FE terminál):**
   - Task 1: `FE-TOP1_design-cutting-workflow.md` (new/)
     - Scope: DesignPage submit + ProductionPage navigation
     - DoD: API integráció, highlight, +3 teszt
     - Becsült idQ: 23 nap
   - Task 2: `FE-TOP2_nesting-visualization.md` (new/)
     - Scope: NestingViewer komponens + ProductionPage integráció
     - DoD: SVG canvas, waste % badge, +13 teszt (8 viewer + 5 page)
     - Becsült idQ: 34 nap
   - Task 3: `FE-TOP3_machine-scheduling-ui.md` (new/)
     - Scope: BatchAssignmentBoard + BatchCard komponensek
     - DoD: Drag-drop, RBAC, FSM actions, +10 teszt
     - Becsült idQ: 45 nap
     - **FüggQség:** Backend `POST assign-batch` deployed

4. **Slice 2 (5 új backend modul) tervezés indítása (csak TOP 13 után):**
   - CRM / Finance / Project / Maintenance / HR modulok
   - Scope: ADR drafting (Architect terminál), domain modellezés
   - Timeline: 2026 Q3 (TOP 13 prioritás!)

---

## Sikerkritériumok (DoD)

**TOP 1:**
- [ ] DesignPage Step 4 submit hívja a valódi `POST /cutting/api/sheets` endpoint-ot (mock eltávolítva)
- [ ] ProductionPage auto-navigál a frissen létrehozott plan-ra (URL state + scroll)
- [ ] Plan sorban megjelenik a customer name + order context (jelenleg "")
- [ ] +3 FE teszt pass (submit flow, navigation, highlight)

**TOP 2:**
- [ ] ProductionPage nesting viewer megjeleníti a `GET /cutting/api/cutting/sheets/{id}/nesting` API adatait
- [ ] SVG canvas scale-zett panel + placed parts színkódolt rectangles (CATALOG_LOOKUP szerint)
- [ ] Stats badge: Waste % (color-coded), Strategy, Sheets count
- [ ] Per-sheet navigation (ha >1 sheet)
- [ ] +13 FE teszt pass (8 viewer + 5 page)

**TOP 3:**
- [ ] Backend: `POST /cutting/api/plans/{date}/assign-batch` endpoint deployed, +3 BE teszt pass
- [ ] Frontend: ProductionPage machining tab ’ drag-drop batch assignment board
- [ ] BatchCard: operator autocomplete, time picker, priority slider, assign button
- [ ] FSM quick actions: Start / Pause / Complete buttons (RBAC-based visibility)
- [ ] +10 FE teszt pass (6 board + 4 card)

**Teljes sikerkritérium:**
- [ ] ~26 új FE teszt pass (~768 ’ 794 teszt)
- [ ] +3 új BE teszt pass (Cutting modul ~931 ’ 934 teszt)
- [ ] 0 pnpm build error
- [ ] Doorstar workflow törött pontok (Design’Cutting, Waste láthatóság, Scheduling) megoldva
- [ ] Deploy smoke test: https://joinerytech.hu/w/design (Step 4 submit) ’ https://joinerytech.hu/w/production/cutting (új plan highlight)

---

## Elfogadási javaslat (conductor döntés)

**Javaslat:** APPROVE + PRE-IMPLEMENTATION ellenQrzések futtatása (5 nyitott kérdés).

**Indoklás:**
- Backend infrastruktúra 95%-ban kész (csak 1 endpoint hiányzik)
- Frontend munka jól definiált, komponens-szinten bontható
- Doorstar napi workflow törött pontjai priorizáltak (SpaceOS core promise)
- Iparági best practice-ek (Cyncly, Syspro, KanbanBOX) alátámasztják a funkcionalitást
- Teszt coverage növekszik (~26 FE teszt)

**KövetkezQ lépés:** Root terminál futtatja a PRE-IMPLEMENTATION ellenQrzéseket ’ frissített konsenzus VAGY inbox üzenetek a termináloknak.
