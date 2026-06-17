---
created: 2026-06-16
plan_a: /opt/spaceos/docs/planning/plans/2026-06-16_plan-sonnet-a.md
plan_b: /opt/spaceos/docs/planning/plans/2026-06-16_plan-sonnet-b.md
status: ready_for_conductor
---

# SpaceOS Konsenzus Implementációs Terv — Manufacturing Stack

## Összefoglalás

A terv a Design→Cutting→Machining vertikum három funkcióját implementálja hibrid megközelítéssel: Sonnet-B capacity-driven backend alapozását kombináljuk Sonnet-A gyors iterációs UX stratégiájával. A nesting vizualizáció tanulási platformként szolgál, majd a scheduling backend (capacity API-val) megalapozza az operatív réteget, végül a design→cutting workflow köti össze a teljes láncot auto-assign innovációval. **Teljes időbecslés: 11-12 nap (4d BE + 7.5d FE).**

## Elfogadott prioritás sorrend

1. **Nesting Vizualizáció + Cycle Time** (TOP 2 hibrid) — Zero backend risk, tanulási platform a workflow UX-hez, PLUSZ Sonnet-B estimated cycle time innovációja
2. **Machine Scheduling Backend + Capacity API** (TOP 3 alap) — FSM transition, assign endpoint, capacity constraint API (DACH scaling prep)
3. **Design→Cutting Workflow + Auto-Assign** (TOP 1 finálé) — Navigate + highlight, PLUSZ Sonnet-B smart assign flagje
4. **Drag-Drop Scheduler UI** (TOP 3 UI) — Optimistic UI, conflict detection, capacity gauge

**Indoklás:** Nesting viewer gyors win + UX teszt platform → Backend alapozás (capacity-ready) → Workflow integráció (automatizálással) → Operatív UI finalizálás.

---

## Backend szükségletek (összesített)

### 1. Nesting API audit + Cycle Time (0.5 nap BE)

**Endpoint:** `GET /cutting/api/cutting/sheets/{id}/nesting`

**Response bővítés:**
```typescript
interface NestingResponse {
  sheetId: string;
  layouts: Array<{
    sheetIndex: number;
    material: { width: number; height: number; type: string };
    placements: Array<{
      partId: string;
      x: number; y: number;
      width: number; height: number;
      rotation: 0 | 90 | 180 | 270;
      catalogType: string; // ← ENRICHMENT kell (N+1 query elkerülés)
    }>;
    wastePercent: number;
  }>;
  strategy: 'Guillotine' | 'FFDH';
  estimatedCycleMinutes: number; // ← ÚJ (Sonnet-B innováció)
}
```

**Backend logic:**
```csharp
// CuttingService.cs
public int EstimateCycleTime(int partCount, int sheetCount) {
    return partCount * 2 + sheetCount * 5; // 2 min/part + 5 min setup/sheet
}
```

---

### 2. Machine Scheduling Backend (2 nap BE)

**Endpoint 1:** `POST /cutting/api/execution/assign-batch`
```json
{
  "batchId": "guid",
  "machineId": "CNC-01",
  "operatorId": "user-guid",
  "scheduledStart": "2026-06-17T08:00:00Z",
  "priority": 1
}
```

**FSM transition:** `CuttingExecution.Assign()` → `Planned → Scheduled`

**Endpoint 2:** `GET /cutting/api/machines/capacity?date=2026-06-17` (Sonnet-B capacity API)
```json
{
  "machines": [
    {"id": "CNC-01", "availableMinutes": 480, "assignedMinutes": 210},
    {"id": "EDGE-02", "availableMinutes": 480, "assignedMinutes": 450}
  ]
}
```

**Adatmodell:**
```csharp
public class MachineScheduleSlot {
    public string MachineId { get; init; }
    public DateOnly Date { get; init; }
    public int TotalMinutes { get; init; } = 480; // 8h shift
    public List<ScheduledBatch> Batches { get; init; }
}
```

**Conflict handling:** Pessimistic locking + 409 Conflict response ha ugyanazon slot-ra concurrent assign.

---

### 3. Design→Cutting Auto-Assign (1.5 nap BE)

**Endpoint bővítés:** `POST /cutting/api/sheets`
```json
{
  "orderReference": "string",
  "templateId": "guid",
  "calculatedParts": [...],
  "autoAssign": true,              // ← ÚJ flag
  "preferredMachine": "CNC-01"     // ← ÚJ
}
```

**Backend logic:**
```csharp
if (request.AutoAssign) {
    var machine = await _scheduler.FindNextAvailableSlot(
        request.EstimatedMinutes, 
        request.PreferredMachine
    );
    await _execution.Assign(sheetId, machine.Id, currentUser.Id);
}
```

**Response bővítés:**
```json
{
  "sheetId": "guid",
  "cuttingPlanId": "guid",
  "createdAt": "timestamp",
  "assignedMachine": "CNC-01"  // ← ÚJ (ha autoAssign=true)
}
```

---

## Frontend megközelítés (legjobb elemek)

### 1. Nesting Vizualizáció (3 nap FE)

**Komponens struktúra:** (Sonnet-A)
```
ProductionPage/
  NestingViewerPanel.tsx
    ├─ SheetSelector.tsx         
    ├─ NestingCanvas.tsx          // SVG renderer
    │   └─ PlacedPart.tsx         
    └─ NestingStats.tsx           // waste% + cycle time badge
```

**State:** (Sonnet-A snippet)
```typescript
const [selectedPlan, setSelectedPlan] = useState<string | null>(null);
const { data, loading, error } = useApi<NestingResponse>(
  selectedPlan ? `/cutting/api/cutting/sheets/${selectedPlan}/nesting` : null
);
```

**Stats badge:** (Sonnet-B bővítés)
```tsx
<Badge>
  Waste: {data.wastePercent}% · {data.strategy} · Est. {data.estimatedCycleMinutes} min
</Badge>
```

---

### 2. Design→Cutting Workflow (2 nap FE)

**DesignPage Step 4:** (Sonnet-A base + Sonnet-B auto-assign)
```typescript
const [autoSchedule, setAutoSchedule] = useState(false);
const [preferredMachine, setPreferredMachine] = useState("CNC-01");

const handleGeneratePlan = async () => {
  const response = await api.post<SubmitCuttingSheetResponse>('/cutting/api/sheets', {
    orderReference: formState.orderRef,
    templateId: selectedTemplate.id,
    calculatedParts: materialList,
    autoAssign: autoSchedule,
    preferredMachine: preferredMachine
  });
  
  navigate('/production', { 
    state: { 
      highlightPlanId: response.cuttingPlanId,
      fromDesign: true,
      tab: autoSchedule ? 'machining' : 'plans' // ← auto-assign esetén machining tab
    } 
  });
};
```

**UI:**
```tsx
<Checkbox checked={autoSchedule} onChange={setAutoSchedule}>
  Schedule immediately
</Checkbox>
{autoSchedule && (
  <Select value={preferredMachine} onChange={setPreferredMachine}>
    <option value="CNC-01">CNC-01</option>
    <option value="EDGE-02">Edge Bander</option>
  </Select>
)}
```

**ProductionPage highlight:** (Sonnet-A snippet)
```typescript
const { state } = useLocation<{ highlightPlanId?: string; tab?: string }>();

useEffect(() => {
  if (state?.tab) setActiveTab(state.tab);
  
  if (state?.highlightPlanId) {
    const row = tableRef.current?.querySelector(`[data-plan-id="${state.highlightPlanId}"]`);
    row?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    row?.classList.add('highlight-row'); // CSS pulse animation
    setTimeout(() => row?.classList.remove('highlight-row'), 2000);
  }
}, [state]);
```

---

### 3. Drag-Drop Scheduler UI (4.5 nap FE)

**Komponens:** (Sonnet-B base + Sonnet-A capacity bar)
```
ProductionPage/machining/
  UnassignedBatchList.tsx       
  MachineColumn.tsx (×3)        
    ├─ CapacityBar.tsx          // ← Sonnet-B capacity gauge (210/480 min)
    └─ BatchCard.tsx (draggable)
  AssignmentDialog.tsx
```

**State:** (Sonnet-B optimistic UI)
```typescript
const assignMutation = useMutation({
  mutationFn: (assign: AssignBatch) => 
    api.post('/cutting/api/execution/assign-batch', assign),
  onMutate: async (newAssign) => {
    queryClient.setQueryData(['batches'], old => moveCard(old, newAssign))
  },
  onError: (error, variables, context) => {
    // Rollback + toast on 409 Conflict
    queryClient.setQueryData(['batches'], context.previousBatches);
    toast.error('Machine already assigned at this time');
  }
})
```

**Capacity bar:**
```tsx
const { data: capacity } = useApi<MachineCapacity[]>(
  `/cutting/api/machines/capacity?date=${selectedDate}`
);

<CapacityBar 
  used={capacity.assignedMinutes} 
  total={capacity.availableMinutes}
  warning={capacity.assignedMinutes > capacity.availableMinutes} 
/>
```

---

## Amit Sonnet-A-tól veszünk át

1. **Nesting-first sorrend** — Zero backend risk, tanulási platform indoklás
2. **Frontend state snippet-ek** — `useLocation` + scroll-to-row kód (implementálható as-is)
3. **N+1 query kockázat audit** — `catalogType` enrichment explicit kérése
4. **Időbecslés granularitása** — FE/BE bontás, napi szinten

---

## Amit Sonnet-B-től veszünk át

1. **Capacity API** — `GET /machines/capacity` (DACH scaling prep)
2. **Estimated cycle time** — Nesting result bővítése operatív kontextussal
3. **Auto-assign flag** — Design Step 4 checkbox + gép preferencia (killer feature)
4. **Optimistic UI + conflict detection** — 409 Conflict kezelés előre lefedve
5. **Alulról építés indoklása** — Backend alapozás (capacity constraint) kritikus

---

## Nyitott kérdések a Conductor-nak

1. **Backend resource allokáció:** 4 nap BE munka (0.5d audit + 2d scheduling + 1.5d auto-assign) — melyik terminál (CUTTING vagy KERNEL) kapja a capacity API-t?
2. **Operator lista RBAC:** `/identity/api/users?role=machine-operator` filter támogatott? Ha nem, FE-ből szűrés vagy BE endpoint kell?
3. **Machine enum source of truth:** Hardcoded kezdetben (`CNC-01`, `EDGE-02`, `QC-01`) vagy admin UI-ból jön? Javasolt: hardcoded v1, későbbi feature.
4. **ProductionPage GET /cutting/api/plans response enrichment:** `orderReference`, `templateName`, `customerName` mezők elérhetők? Ha nem, N+1 query risk.
5. **Sprint planning:** 11-12 napos munka → 1 sprint (párhuzamos FE+BE) vagy 2 sprint (szekvenciális)?
6. **Doorstar soft launch deadline impact:** Ez a feature kritikus-e a Q2 soft launch-hoz, vagy post-launch iteration?

---

## Összesített időbecslés

| Funkció | FE | BE | Összesen |
|---|---|---|---|
| Nesting viz + cycle time | 3d | 0.5d | 3.5d |
| Scheduling backend + capacity | 0d | 2d | 2d |
| Design→Cutting + auto-assign | 2d | 1.5d | 3.5d |
| Drag-drop scheduler UI | 4.5d | 0d | 4.5d |
| **TOTAL** | **9.5d** | **4d** | **13.5d** |

**Korrigált realitás:** 11-12 nap (párhuzamos FE+BE munka, átfedések figyelembevételével).
