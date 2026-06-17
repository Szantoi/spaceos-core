---
domain: manufacturing
type: feature_gap
priority: high
created: 2026-06-16
scanned_by: haiku
---

# Machine & Operator Scheduling UI

## Mit old meg
Terv elkészítés után az üzemvezetőnek fel kell osztania a vágási feladatokat gépek és operátorok között. Ma ProductionPage machining tab csak mock (CNC, Edgebanding, QC), nincs valós task-assignment.

## Jelenlegi állapot
- **Backend:** `/cutting/api/cutting/plans/{date}` GetDailyCuttingPlanQuery — terv csoportosított anyagtípus szerint
- **Frontend:** ProductionPage machining tab (179-204 sor) mock-olt, 9 darab hardcoded task, nincs drag-drop, nincs assign UI
- **EndpointPending:** `CuttingExecution` aggregátum létezik DDD domain-ben, de nincs API endpoint az assign-hoz

## Bekötési lehetőség
1. **Adat model:**
   - `DailyCuttingPlan` → `CuttingBatch` (anyagtípus szerinti csoportosítás)
   - Backend: `POST /cutting/api/plans/{date}/assign-batch` — batch → machine + operator + prioritás
   - CuttingExecution FSM: `Planned → InProgress → Completed`

2. **UI (kanban vagy táblázat):**
   - Bal: Unassigned batches (anyag + darabszám + dátum)
   - Jobb 3 oszlop: CNC machines · Edgebanding · QC stations
   - Drag-drop batch → machine
   - Operator autocomplete + date time picker per task
   - Save → `POST /cutting/api/plans/{date}/assign`

3. **Machining tab redesign:**
   - Toggle: "Terv" (mock kártyák) vs. "Ütemezés" (assignment form)
   - Task card: batch ID · material · qty · operator · prioritás · status
   - Quick actions: start, pause, complete (ha executor jog van)

## Iparági relevancia
Doorstar munka hajnal 8-tól este 5-ig, 2-3 géppal. Az operátor napi terv néhány perc alatt el kell hogy végezze az átállásokat és ütemezést. Korábban Viber + papír, így egy intuitív UI kritikus.
