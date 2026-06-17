---
created: 2026-06-16
selected_by: sonnet
status: pending_debate
top_count: 3
domain: manufacturing
---

# SpaceOS Planning — Kiválasztott fejlesztési irányok

## Értékelési szempontok

| Szempont | Súly |
|---|---|
| Felhasználói érték (Doorstar napi workflow) | 40% |
| Megvalósíthatóság (backend kész, bekötési munka) | 35% |
| Iparági relevancia (ERP/MES standard?) | 25% |

---

## TOP 1: Design → Cutting Plan Workflow (003)

**Ötlet forrása:** `2026-06-16_003_cutting-list-to-plan.md`
**Típus:** UX gap — workflow integráció
**Backend állapot:** ✅ `POST /cutting/api/sheets` (SubmitCuttingSheet) már létezik

**Miért top:**
Ez a SpaceOS core promise-ának törése — a teljes Doorstar workflow (Design → Anyaglista →
Vágási terv → Gyártás) UI szinten szaggatott. Az operátor jelenleg manuálisan keresi meg
a ProductionPage-en az épp generált tervet. Ez pontosan az a Viber+papír fájdalompont
amit a SpaceOS megold. A backend kész, a feladat frontend integráció.

**Webes minták (kutatás alapján):**
- **Cyncly Insight** (woodworking ERP): "from initial opportunity to final installation" —
  closed-loop design-to-production, a CAD cutting list automatikusan kerül a production queue-ba
- **Syspro furniture ERP:** "engineering changes flowing automatically into bills of materials
  and production instructions" — nincs manuális átmásolás design-ból BOM-ba
- **Woodworking Network MES Best Practice:** "a cabinet shop can send cutting lists and CNC
  programs directly to the MES, which monitors each board cut in real time, sending
  completion data back to the ERP" — ez az iparági baseline elvárás

**Javasolt megközelítés:**
1. DesignPage Step 4 submit → valódi `POST /cutting/api/sheets` (mock helyett)
   - Body: `orderReference` + `templateId` + `calculatedParts`
   - Response: `sheetId` + `cuttingPlanId`
2. `navigate('/production', { state: { highlightPlanId } })` — scroll + highlight az új sorra
3. ProductionPage plan sorban: customer name + order context (design template back-link)
4. Breadcrumb UI: Design → Material List → Nesting → Execution

**Implementációs komplexitás:** Alacsony–Közepes (2–3 nap FE, 0 backend)

---

## TOP 2: Nesting Vizualizáció (001)

**Ötlet forrása:** `2026-06-16_001_nesting-visualization.md`
**Típus:** Endpoint gap — meglévő API nincs bekötve
**Backend állapot:** ✅ `GET /cutting/api/cutting/sheets/{id}/nesting` létezik, 32/32 teszt zöld

**Miért top:**
Az anyaghatékonyság (waste %) az asztalosok elsődleges döntési szempontja mielőtt a gép
elindul. Ma a ProductionPage nesting viewer `"Nesting API nem elérhető"` mock üzenettel áll —
holott az API ott van és tesztelt. Közvetlen bekötési lehetőség, magas látható értékkel.
Doorstar-nál 2-3 panel naponta — a waste % láthatóvá tétele közvetlen anyagköltség csökkentés.

**Webes minták (kutatás alapján):**
- **Solid Edge 2D Nesting:** "optimized layouts for two-dimensional cutting of wood, plastic,
  sheet metal" — SVG/DXF alapú vizualizáció iparági standard
- **Almacam Cut:** per-sheet placement toggle, material color coding, waste % badge —
  pontosan a SpaceOS-ből hiányzó vizuális elemek
- **ADH Machine Tool / CADTalk guide:** "nesting integrates with ERP/MES via two-way data
  exchange — material usage, cycle times, remnant details" — a remnant/waste tracking
  ERP standard; a SpaceOS backend ezt már returnálja, csak UI kell

**Javasolt megközelítés:**
1. `useApi()` hook → `GET /cutting/api/cutting/sheets/{selectedPlanId}/nesting`
2. SVG canvas: scale-zett panel (alap) + placed parts (CATALOG_LOOKUP szerinti szín kódolás)
3. Stats badge: Waste %, Strategy (Guillotine / FFDH), Sheets count
4. Per-sheet toggle ha >1 lap van
5. Material type filter (meglévő CATALOG_LOOKUP alapján)

**Implementációs komplexitás:** Közepes (3–4 nap FE, 0 backend — SVG rendering a fő munka)

---

## TOP 3: Machine & Operator Scheduling UI (002)

**Ötlet forrása:** `2026-06-16_002_machine-operator-scheduling.md`
**Típus:** Feature gap — UI + 1 új backend endpoint
**Backend állapot:** ⚠️ `CuttingExecution` DDD aggregátum létezik, de `POST assign-batch` hiányzik

**Miért top:**
Doorstar napi 8–17h között 2-3 géppel dolgozik. A machining tab ma 9 hardcoded mock
kártyát mutat — az operátor nem tud gépet vagy embert hozzárendelni. Ez a funkció az
üzemvezető reggeli 15 perces rituáléját digitalizálja (ma: Viber + papír + telefon).

**Webes minták (kutatás alapján):**
- **KanbanBOX Sequencer:** "digital board per resource — kanban cards in processing order,
  drag-drop reassignment, Production Manager reorganizes cards in real-time" — ez a
  pattern illeszkedik közvetlenül a ProductionPage machining tab-re
- **WinMan ERP:** "visual planning board — schedule workload by cell or machine, group
  similar work orders, analyze capacities and smooth bottlenecks"
- **Tulip MES:** "labor requirements specify roles/certifications — operator matched to
  jobs based on skill level" — RBAC-alapú operator assign, SpaceOS auth-csal kombinálható

**Javasolt megközelítés:**
1. Backend: `POST /cutting/api/plans/{date}/assign-batch` (1 nap BE)
   - Body: `batchId` + `machineId` + `operatorId` + `priority` + `startTime`
   - FSM: `Planned → InProgress` trigger
2. UI (machining tab redesign):
   - Bal oszlop: Unassigned batches (anyag + qty + dátum + prioritás chip)
   - Jobb 3 oszlop: CNC machines · Edgebanding · QC stations (drag-drop target)
3. Batch card: operator autocomplete (RBAC: machine operator szerepkör) + time picker
4. Submit → POST assign → UI refresh
5. FSM quick actions (executor jogosultsággal): Start / Pause / Complete

**Implementációs komplexitás:** Közepes–Magas (1 nap BE + 4–5 nap FE, drag-drop lib kell)

---

## Elvetett ötletek (és miért)

- **Valós idejű gyártócsarnok státusz (004):** `EndpointPending` — nincs backend, nincs
  WebSocket infrastruktúra. Értéke magas, de teljes BE build kellene. A TOP 3-as
  CuttingExecution FSM (assign + start/complete) lefekteti az alapot ehhez. Következő iteráció.

- **Anyagfelhasználás nyomon követés (005):** Medium priority a scanner szerint, és kisebb
  napi operatív hatás Doorstar jelenlegi fázisában. Logikus következő lépés a TOP 1
  (Design→Cutting link) életbe lépése után — akkor az inventory mozgás automatikusan
  köthetővé válik a cutting sheet-hez.

---

## Javasolt implementációs sorrend

```
TOP 1 (003) → TOP 2 (001) → TOP 3 (002)
```

**Indoklás:** TOP 1 a workflow integráció kapocs — amint a design→cutting link él, a TOP 2
nesting vizualizáció azonnal gazdagabb kontextusban jelenik meg (az adott plan-hoz látjuk
a waste %-ot). TOP 3 a legnehezebb (új BE endpoint + drag-drop), ezért utolsó.
