---
id: MSG-FE-068
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-17
---

# FE-068 — Joinery API Integration: Material Requisition + Daily Cutting Plans

## Context

**Consensus PHASE 1 SUCCESS:** Design→Cutting→Nesting→Scheduling workflow complete.

**Phase 2 Mission:** Remove frontend mocks → integrate real Joinery backend data.

**Current State:** DesignPage + ProductionPage use hardcoded mock data. Joinery module (port 5002) provides:
- `GET /api/orders/{id}/material-req` — BOM material list
- `GET /api/orders/{id}/hardware-list` — ferdeség, festék specs
- `POST /api/cutting/plans` — napi terv generálása
- `GET /api/cutting/plans?date={YYYY-MM-DD}` — poll terv status

**Phase 2 Goal:** Hook real APIs into existing components.

---

## Scope (2-3 days FE)

### 1. OrderDetailPage Integration (1 day)

**Component:** `src/pages/OrderDetailPage.tsx` (existing)

**Changes:**
```typescript
// BEFORE: Material requisition = hardcoded mock
const mockMaterials = [{ id: 'MAT-001', name: 'Pine', qty: 10 }]

// AFTER: Fetch from Joinery API
const [materials, setMaterials] = useState<MaterialReq[]>([])
const [loading, setLoading] = useState(false)

useEffect(() => {
  if (!orderId) return
  setLoading(true)
  fetch(`/api/orders/${orderId}/material-req`)
    .then(r => r.json())
    .then(data => setMaterials(data.materials))
    .catch(err => setMaterials(mockMaterials)) // Fallback to mock on error
    .finally(() => setLoading(false))
}, [orderId])
```

**Hardware Specs Integration:**
```typescript
const [hardwareSpecs, setHardwareSpecs] = useState<HardwareSpec[]>([])

useEffect(() => {
  if (!orderId) return
  fetch(`/api/orders/${orderId}/hardware-list`)
    .then(r => r.json())
    .then(data => setHardwareSpecs(data.specs))
    .catch(err => setHardwareSpecs(mockSpecs)) // Fallback
}, [orderId])
```

**Components to Update:**
- `<MaterialRequisitionTable materials={materials} loading={loading} />`
- `<HardwareSpecsCard specs={hardwareSpecs} />`

**Requirements:**
- Loading state while fetching (spinner)
- Error fallback: show mock data if API fails (graceful degradation)
- Cache material-req per order (avoid refetch on tab switch)
- Render null if orderId missing

**Tests:**
- [x] Renders loading state
- [x] Fetches and displays real materials
- [x] Fallback to mock on 404
- [x] Handles network errors gracefully

---

### 2. ProductionPage Daily Cutting Plan Integration (1.5 days)

**Component:** `src/pages/ProductionPage.tsx` (existing)

**Change 1: Generate Cutting Plan**
```typescript
// Button: "Generate Daily Cutting Plan"
const generateCuttingPlan = async () => {
  setGenerating(true)
  try {
    const response = await fetch('/api/cutting/plans', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        date: new Date().toISOString().split('T')[0], // YYYY-MM-DD
        capacity: 1000, // mm² or time-based
        orders: selectedOrderIds // [order1, order2, ...]
      })
    })
    const plan = await response.json()
    setPlanId(plan.id)
    setPlanStatus('generated')
  } catch (err) {
    console.error('Failed to generate plan:', err)
    setPlanStatus('error')
  } finally {
    setGenerating(false)
  }
}
```

**Change 2: Poll Daily Plan Status**
```typescript
const [pollInterval, setPollInterval] = useState<NodeJS.Timeout | null>(null)

useEffect(() => {
  if (!planId) return

  // Poll every 2 seconds until complete
  const interval = setInterval(async () => {
    const response = await fetch(`/api/cutting/plans?date=${todayDate}&id=${planId}`)
    const data = await response.json()
    setCuttingPlan(data.plan)

    if (data.plan.status === 'complete') {
      clearInterval(interval)
    }
  }, 2000)

  setPollInterval(interval)
  return () => clearInterval(interval)
}, [planId, todayDate])
```

**Component Rendering:**
```typescript
{planStatus === 'idle' && (
  <button onClick={generateCuttingPlan} disabled={selectedOrderIds.length === 0}>
    Generate Daily Cutting Plan
  </button>
)}

{planStatus === 'generating' && <Spinner text="Generating plan..." />}

{planStatus === 'generated' && cuttingPlan && (
  <CuttingPlanTable plan={cuttingPlan} loading={cuttingPlan.status !== 'complete'} />
)}

{planStatus === 'error' && <ErrorAlert message="Failed to generate cutting plan. Try again." />}
```

**Requirements:**
- Disable "Generate" button if no orders selected
- Show spinner while generating (POST)
- Poll status every 2s, stop when status='complete'
- Display cutting plan table (sheets, parts, sequencing)
- Handle timeout: if poll > 5 min, show warning

**Tests:**
- [x] POST /api/cutting/plans with correct payload
- [x] Poll starts after POST succeeds
- [x] Poll stops when status='complete'
- [x] Error handling: 500 response → show error
- [x] Timeout handling: 5+ min → show warning
- [x] Renders cutting plan table with correct data

---

### 3. API Contracts & Error Handling (0.5 days)

**Endpoint Contracts:**

**GET /api/orders/{id}/material-req**
```typescript
interface MaterialReqResponse {
  orderId: string
  materials: Array<{
    id: string
    name: string
    materialType: 'wood' | 'hardware' | 'finishing'
    quantity: number
    unit: 'piece' | 'meter' | 'kg'
    unitPrice: number
    warehouseQty: number
    status: 'in-stock' | 'on-order' | 'insufficient'
  }>
  totalCost: number
  generatedAt: string // ISO 8601
}
```

**GET /api/orders/{id}/hardware-list**
```typescript
interface HardwareListResponse {
  orderId: string
  specs: Array<{
    spec: 'edge-banding' | 'hinge' | 'lacquer' | 'stain'
    value: string
    quantity: number
  }>
  generatedAt: string
}
```

**POST /api/cutting/plans**
```typescript
// Request
{
  date: "2026-06-17",  // YYYY-MM-DD
  capacity: 1000,      // mm² or minutes
  orders: ["order-1", "order-2"]
}

// Response
{
  id: "plan-uuid",
  date: "2026-06-17",
  status: "queued" | "processing" | "complete",
  sheets: [
    {
      sheetId: "sheet-1",
      parts: [{ partId, x, y, width, height }],
      wastePercent: 12.5
    }
  ]
}
```

**GET /api/cutting/plans?date=YYYY-MM-DD**
```typescript
// Response
{
  plans: [
    {
      id: "plan-uuid",
      date: "2026-06-17",
      status: "complete",
      sheets: [...]
    }
  ]
}
```

**Error Handling:**
- 404 on missing order: Fall back to mock materials
- 500 on API error: Show error message, allow retry
- Network timeout: Retry 3× with exponential backoff
- Graceful degradation: Always render something (mock or error state)

---

### 4. Testing (included in above sections)

**Unit Tests:**
- Material requisition fetching + rendering
- Hardware specs rendering
- Cutting plan generation button state
- Poll interval management + cleanup
- Error fallback logic

**Integration Tests:**
- E2E: OrderDetail → fetch materials → verify rendering
- E2E: ProductionPage → generate plan → poll → verify table
- E2E: Missing order → fallback to mock

**Mock Data** (for tests when API unavailable):
```typescript
const mockMaterials = [ /* existing */ ]
const mockHardwareSpecs = [ /* existing */ ]
const mockCuttingPlan = { /* existing */ }
```

---

## Definition of Done

- [x] OrderDetailPage fetches real material-req data
- [x] OrderDetailPage fetches real hardware-list data
- [x] ProductionPage generates cutting plan (POST)
- [x] ProductionPage polls and displays results
- [x] All components have loading/error states
- [x] Graceful fallback to mock on API failure
- [x] All endpoints properly typed (TypeScript)
- [x] Tests: +15 new tests (all passing)
- [x] 0 build errors
- [x] No console warnings/errors

---

## Timeline

| Task | Duration | Status |
|------|----------|--------|
| OrderDetail integration | 8h | → Start now |
| ProductionPage integration | 12h | → Parallel |
| Contracts + error handling | 4h | → Parallel |
| Testing | 4h | → End |
| **TOTAL** | **~28h** | **2-3 days** |

---

## Success Metrics

1. Real material data displays on OrderDetailPage (no mocks)
2. Real hardware specs display on OrderDetailPage
3. Cutting plan generates via POST → displays in table
4. All error scenarios handled gracefully
5. +15 tests passing (100%)

---

## Orchestrator Requirement

**FE Dependency:** Orchestrator must route to Joinery correctly:
```
GET /api/orders/{id}/material-req    → Joinery (5002)
GET /api/orders/{id}/hardware-list   → Joinery (5002)
POST /api/cutting/plans              → Cutting (5004)
GET /api/cutting/plans               → Cutting (5004)
```

**Coordination:** MSG-ORCH-001 task (sent separately) will verify routing.

---

## Reference

**Joinery API Planning:**
- `/planning/ideas/2026-06-16_003_joinery-api-integration.md`

**Existing Components:**
- `src/pages/OrderDetailPage.tsx`
- `src/pages/ProductionPage.tsx`
- `src/components/CuttingPlanTable.tsx`

---

**ROOT Approval:** ✅ Phase 2 manufacturing track (FE)
**ETA:** 2026-06-18 or 2026-06-19
**Blocking:** None (Joinery backend ready)
**Next:** Send DONE when complete

🚀 **FE-068: Joinery Integration — Mock-Free Production**
