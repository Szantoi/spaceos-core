# SpaceOS Implementációs Terv — Sonnet-B (Merész megközelítés)

## Prioritás sorrend: Fordított logika

**TOP 3 → TOP 1 → TOP 2** — Alapozz alulról felfelé.

### Miért?

A TOP 1 (Design→Cutting workflow) látványos, de **üres alapokon** épül. Ha az operátor beküldi a cutting sheet-et, de nem tudja kijelölni gépet/embert, akkor csak új mock helyett régi mockot látsz. **Kezdd a fizikai műveleti réteggel.**

---

## 1. Machine Scheduling Backend (TOP 3 — 1. fázis)

### Endpoint: `POST /cutting/api/execution/assign-batch`

```json
{
  "batchId": "guid",
  "machineId": "CNC-01",
  "operatorId": "user-guid",
  "scheduledStart": "2026-06-17T08:00:00Z",
  "priority": 1
}
```

**FSM trigger:** `CuttingExecution.Assign()` → `Planned → Scheduled`

### Merész rész: Capacity constraint API

**`GET /cutting/api/machines/capacity?date=2026-06-17`**

```json
{
  "machines": [
    {"id": "CNC-01", "availableMinutes": 480, "assignedMinutes": 210},
    {"id": "EDGE-02", "availableMinutes": 480, "assignedMinutes": 450}
  ]
}
```

Ez a Doorstar 2-3 gépes setup-nál triviális, de **scaling készség** — DACH belépésnél 15 géppel ez kritikus lesz. Építsd be MOST.

**Adatmodell:**

```csharp
public class MachineScheduleSlot {
    public string MachineId { get; init; }
    public DateOnly Date { get; init; }
    public int TotalMinutes { get; init; } = 480; // 8h shift
    public List<ScheduledBatch> Batches { get; init; }
}
```

**Complexity:** 2 nap BE (1 új endpoint + FSM transition + capacity aggregation)

---

## 2. Frontend: Drag-Drop Scheduler (TOP 3 — 2. fázis)

### Komponens struktúra

```
ProductionPage/
  MachiningTab/
    UnassignedBatchList.tsx         ← bal oszlop
    MachineColumn.tsx (×3)          ← CNC / Edge / QC
      ├─ CapacityBar.tsx            ← 210/480 min gauge
      └─ BatchCard.tsx (draggable)
```

### State: React Query + optimistic UI

```typescript
const assignMutation = useMutation({
  mutationFn: (assign: AssignBatch) => 
    api.post('/cutting/api/execution/assign-batch', assign),
  onMutate: async (newAssign) => {
    // Optimistic: mozgasd a kártyát azonnal
    queryClient.setQueryData(['batches'], old => moveCard(old, newAssign))
  }
})
```

**Merész rész:** **Conflict detection** — ha két operátor ugyanarra a gépre próbál assignolni, akkor 409 Conflict + UI toast + auto-refresh. Ez a concurrency kezelés **előre** le van fedve.

**Lib:** `@dnd-kit/core` (lightweight, accessibility-first)

**Complexity:** 4 nap FE

---

## 3. Nesting Viz KIEGÉSZÍTVE capacity hatással (TOP 2)

### Standard bekötés

`GET /cutting/api/cutting/sheets/{id}/nesting` → SVG canvas (ez a baseline).

### Merész kiegészítés: **Estimated cycle time**

```typescript
interface NestingResult {
  sheets: Sheet[]
  wastePercent: number
  strategy: "Guillotine" | "FFDH"
  estimatedCycleMinutes: number  // ← ÚJ
}
```

**Backend:** `CuttingService.EstimateCycleTime(partCount, sheetCount)` — egyszerű heurisztika:

```csharp
return partCount * 2 + sheetCount * 5; // 2 min/part + 5 min setup/sheet
```

**Frontend hatás:** A nesting viewer badge mutatja: **"Est. 47 min"** — ez azonnal láthatóvá teszi, hogy a gép capacity-jébe belefér-e még ma.

**Complexity:** 2 nap FE + 0.5 nap BE

---

## 4. Design→Cutting Link AUTO-ASSIGN-nal (TOP 1 — finálé)

### Standard flow

DesignPage Step 4 → `POST /cutting/api/sheets` → `navigate('/production')`

### Merész kiegészítés: **Smart assign**

```json
POST /cutting/api/sheets {
  "autoAssign": true,  // ← ÚJ flag
  "preferredMachine": "CNC-01"
}
```

**Backend logika:**

```csharp
if (request.AutoAssign) {
    var machine = await _scheduler.FindNextAvailableSlot(
        request.EstimatedMinutes, 
        request.PreferredMachine
    );
    await _execution.Assign(sheetId, machine.Id, currentUser.Id);
}
```

**UX:** DesignPage Step 4-ben checkbox: **"Schedule immediately"** → gép kiválasztó dropdown (alapértelmezett: CNC-01). Ha be van pipálva, akkor a submit után **azonnal a machining tab-ra ugrik, ahol a kártya már assigned** és kijelölt.

Ez **eliminálja** az operátor manuális navigálását. A Doorstar workflow 4 kattintásról 1-re csökken.

**Complexity:** 1 nap BE + 1 nap FE

---

## Összegzett sorrend

```
1. Machine Scheduling Backend (2 nap BE)
2. Drag-Drop Scheduler UI (4 nap FE)
3. Nesting Viz + Cycle Time (2 nap FE + 0.5 nap BE)
4. Design→Cutting Auto-Assign (1 nap BE + 1 nap FE)
```

**Total:** 3.5 nap BE + 7 nap FE = **~2 sprint** (10.5 nap)

---

## Kockázatok

| Kockázat | Hatás | Mitigáció |
|---|---|---|
| Drag-drop accessibility | Screen reader inkompatibilitás | `@dnd-kit` built-in ARIA support |
| Capacity constraint edge case | Túlfoglalás azonos időpontra | Pessimistic locking + 409 Conflict |
| Auto-assign rossz gépet választ | Operátor átállítja manuálisan | Default preference user settings-ben |

---

## Miért merész ez a terv?

1. **Capacity API** — túlmutat a mostani 2-3 gépes setup-on, DACH scaling-re készít
2. **Estimated cycle time** — konkurencia (Cyncly, Syspro) ezt high-tier feature-ként adja, mi baseline-ba építjük
3. **Auto-assign** — eliminál manuális lépéseket, nem csak digitalizál hanem **automatizál**

A konzervatív út: TOP 1→2→3. Az innovátor út: **építsd az alapot erősre, a UI látványosság magától jön**.
