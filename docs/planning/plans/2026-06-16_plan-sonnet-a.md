# SpaceOS Manufacturing Stack — Implementációs Terv (Sonnet-A)

## Prioritás és sorrend

**1. Nesting Vizualizáció (001) → 2. Design→Cutting Workflow (003) → 3. Machine Scheduling (002)**

### Indoklás

A TOP 2-t (nesting viz) helyezem előre, mert:
- **Zero backend risk** — API kész, tesztelt, csak UI munka
- **Gyors win** — 3-4 nap FE, azonnali látható érték
- **Tanulási platform** — a nesting viewer szolgál tesztalanyként a TOP 1 workflow integráció UX-éhez

A TOP 1 (workflow) második, mert:
- Nesting viewer meglétével a design→cutting link azonnal gazdagabb UX (generálás után látod a waste %-ot)
- Workflow kapocs — megalapozza a TOP 3 schedulingot

A TOP 3 (scheduling) utolsó, mert:
- Új backend endpoint szükséges (kockázat)
- Drag-drop lib integrálás (komplexitás)
- A workflow link (TOP 1) nélkül kevésbé értelmes (nincs honnan jönnek a batch-ek)

---

## 1. NESTING VIZUALIZÁCIÓ (Prioritás: 1)

### Backend

✅ Kész: `GET /cutting/api/cutting/sheets/{id}/nesting`

**Response audit szükséges:**
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
      catalogType?: string; // CATALOG_LOOKUP mapping
    }>;
    wastePercent: number;
  }>;
  strategy: 'Guillotine' | 'FFDH';
}
```

**Kockázat:** Ha `catalogType` nincs a response-ban → FE-nek extra `GET /joinery/catalog/{partId}` hívások kellenek → N+1 query probléma.

**Mitigáció:** Backend enrichment kérése (1 nap BE) vagy FE batch fetch optimalizálás.

### Frontend

**Komponens struktúra:**
```
ProductionPage/
  NestingViewerPanel.tsx
    ├─ SheetSelector.tsx         // per-sheet toggle (ha >1)
    ├─ NestingCanvas.tsx          // SVG renderer
    │   └─ PlacedPart.tsx         // part rect + tooltip
    └─ NestingStats.tsx           // waste%, strategy, count
```

**State:**
```typescript
const [selectedPlan, setSelectedPlan] = useState<string | null>(null);
const { data, loading, error } = useApi<NestingResponse>(
  selectedPlan ? `/cutting/api/cutting/sheets/${selectedPlan}/nesting` : null
);
const [activeSheetIndex, setActiveSheetIndex] = useState(0);
```

**UX flow:**
1. ProductionPage plan sor → `View Nesting` gomb → sidebar slide-in
2. SVG canvas: 1200×800px viewport, `viewBox` = sheet size (mm), scale auto
3. Part színkódolás: `CATALOG_LOOKUP.type` → HSL color map
4. Tooltip hover: part ID, méretek, rotation, anyag típus
5. Stats badge: `Waste: 12.3% · Strategy: Guillotine · 2 sheets`

**Időbecslés:** 3 nap (SVG logic 1.5d, styling+polish 1d, API hook 0.5d)

---

## 2. DESIGN→CUTTING WORKFLOW (Prioritás: 2)

### Backend

✅ Kész: `POST /cutting/api/sheets` (SubmitCuttingSheet)

**DesignPage mock audit szükséges:**
```typescript
// JELENLEGI mock (feltételezett):
const handleSubmit = () => {
  // ... mock submit
  navigate('/dashboard'); // ❌ innen kell navigate('/production', { state: { ... }})
};
```

**Backend contract ellenőrzés:**
```csharp
// Elvárt request:
public record SubmitCuttingSheetRequest(
  string OrderReference,
  Guid TemplateId,
  List<CalculatedPart> Parts
);

// Elvárt response:
public record SubmitCuttingSheetResponse(
  Guid SheetId,
  Guid CuttingPlanId,
  DateTime CreatedAt
);
```

**Kockázat:** Ha `TemplateId` vagy `OrderReference` nincs a DesignPage state-ben → form módosítás kell.

### Frontend

**DesignPage Step 4 módosítás:**
```typescript
const handleGeneratePlan = async () => {
  const response = await api.post<SubmitCuttingSheetResponse>('/cutting/api/sheets', {
    orderReference: formState.orderRef, // form field kell?
    templateId: selectedTemplate.id,
    calculatedParts: materialList, // Step 3 output
  });
  
  navigate('/production', { 
    state: { 
      highlightPlanId: response.cuttingPlanId,
      fromDesign: true 
    } 
  });
};
```

**ProductionPage módosítás:**
```typescript
const { state } = useLocation<{ highlightPlanId?: string }>();

useEffect(() => {
  if (state?.highlightPlanId) {
    // scroll to row + highlight 2s
    const row = tableRef.current?.querySelector(`[data-plan-id="${state.highlightPlanId}"]`);
    row?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    row?.classList.add('highlight-row'); // CSS pulse animation
    setTimeout(() => row?.classList.remove('highlight-row'), 2000);
  }
}, [state]);
```

**Plan row enrichment:**
```typescript
// ProductionPage GET /cutting/api/plans szükséges response bővítés:
interface CuttingPlan {
  id: string;
  // ... meglévő mezők
  orderReference?: string;      // ← kell
  templateName?: string;         // ← kell
  customerName?: string;         // ← optional (ha van order link)
}
```

**Kockázat:** Ha backend nem adja vissza a `templateName`-t → külön `GET /design/templates/{id}` kell → N+1 query.

**Időbecslés:** 2.5 nap (DesignPage submit 0.5d, ProductionPage scroll+highlight 1d, backend response audit+BE kérés 1d)

---

## 3. MACHINE SCHEDULING (Prioritás: 3)

### Backend

❌ Hiányzik: `POST /cutting/api/plans/{date}/assign-batch`

**Javasolt DDD command:**
```csharp
public record AssignBatchCommand(
  Guid BatchId,
  string MachineId,      // enum: CNC1, CNC2, Edgebander, QC
  Guid? OperatorId,      // nullable (automat gépnél nincs)
  int Priority,          // 1-5
  DateTime ScheduledStart
);

// FSM trigger: CuttingPlanStatus.Planned → InProgress
```

**Repository szükséglet:**
```csharp
// spaceos-modules-cutting/Domain/CuttingExecution.cs
public class CuttingExecution : AggregateRoot {
  public void AssignToMachine(MachineId machine, UserId? operatorId) {
    // FSM guard: csak Planned state-ből
    // Event: BatchAssigned
  }
}
```

**Időbecslés (BE):** 1.5 nap (command handler 0.5d, FSM transition 0.5d, tests 0.5d)

### Frontend

**Komponens:**
```
ProductionPage/machining/
  UnassignedBatchList.tsx       // left column
  MachineColumn.tsx              // right 3 columns (CNC, Edge, QC)
    └─ BatchCard.tsx             // draggable card
  AssignmentDialog.tsx           // operator + time picker
```

**Drag-drop lib:** `@dnd-kit/core` (react-beautiful-dnd fork, maintained)

**State:**
```typescript
const [batches, setBatches] = useState<Batch[]>([]);
const { data: operators } = useApi<User[]>('/identity/api/users?role=machine-operator');

const handleDrop = async (batchId: string, machineId: string) => {
  setAssignmentDialog({ batchId, machineId, open: true });
};

const handleAssign = async (assignment: AssignBatchCommand) => {
  await api.post(`/cutting/api/plans/${selectedDate}/assign-batch`, assignment);
  refetchBatches();
};
```

**Kockázat:**
- Operator lista RBAC szűrés — ha `/identity` nem támogatja `?role=` filter → FE-ben szűrni kell
- Machine enum hardcoded vagy dynamic? (javasolt: hardcoded kezdetben, később admin UI)

**Időbecslés (FE):** 4 nap (drag-drop 2d, assignment dialog 1d, FSM action buttons 1d)

---

## Összesített időbecslés

| Funkció | FE | BE | Összesen |
|---|---|---|---|
| Nesting viz | 3d | 0d | 3d |
| Design→Cutting | 2.5d | 0d | 2.5d |
| Scheduling | 4d | 1.5d | 5.5d |
| **TOTAL** | **9.5d** | **1.5d** | **11d** |

**Sprint javaslat:** 3 hetes sprint, heti 1 dev (FE + BE együtt) → reális.

---

## Kritikus függőségek

1. **TOP 1 előtt:** Backend response audit (`POST /cutting/api/sheets` return mezők)
2. **TOP 2 előtt:** Nesting viewer meglétével a workflow UX teszt
3. **TOP 3 előtt:** BE endpoint delivery (blocking)

**Blokkolók:** Ha backend team 1.5 napos BE munkát nem tudja beütemezni → TOP 3 csúszik vagy elveszik.
