---
id: MSG-FE-087
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: docs/planning/queue/2026-06-21_0055_consensus.md
dependencies: MSG-JOINERY-058 (backend endpoints)
created: 2026-06-21
processed: 2026-06-21
---

# Joinery E2E Flow — Phase 1: Konfigurátor Frontend Komponensek

## Összefoglaló

**Joinery End-to-End vertical slice Phase 1 frontend** — teljes megrendelés loop UI: paraméterezett konfigurátor wizard → anyaglista előnézet → gyártási lap összefoglaló + PDF letöltés.

**Dependency:** Joinery backend endpoints (MSG-JOINERY-058) — **párhuzamos fejlesztés mock data-val lehetséges**, majd backend DONE után integráció.

---

## Frontend Komponensek (ÚJ)

### 1. ProductConfiguratorWizard.tsx (Multi-step wizard)

**Path:** `frontend/joinerytech-portal/src/pages/ProductConfiguratorWizard.tsx`

**Funkció:** 4-lépéses wizard paraméterezett ajtó konfiguráláshoz

**Lépések:**
1. **Product Type Selection** — 5 sablon ajtótípus választás (standard, premium, fireproof, acoustic, security)
2. **Dimensions Input** — szélesség, magasság, vastagság (mm)
3. **Materials Selection** — mag anyag, furnér, élzárás dropdown-ok
4. **Fittings Selection** — zsanér, kilincs, zár dropdown-ok

**State Management:**
```typescript
interface ConfigState {
  step: 1 | 2 | 3 | 4
  productType: string | null
  dimensions: { width: number; height: number; thickness: number }
  materials: { core: string; veneer: string; edge: string }
  fittings: { hinge: string; handle: string; lock: string }
}

const [config, setConfig] = useState<ConfigState>(initialState)
```

**API Hook:**
```typescript
const { mutate: submitConfig, data: configResult } = useMutation({
  mutationFn: (data: ConfigState) =>
    fetch('/api/products/configure', {
      method: 'POST',
      body: JSON.stringify(data)
    }).then(res => res.json()),
  onSuccess: (data) => {
    // Navigate to BOM preview
    router.push(`/configurator/preview/${data.configId}`)
  }
})
```

**UI/UX:**
- Stepper navigation (Material UI vagy Tailwind custom)
- Validation per step (dimension ranges, required fields)
- "Next" / "Back" / "Submit" buttons
- Progress indicator (1/4, 2/4, 3/4, 4/4)
- Error messages (API validation errors display)

**Mock Data (párhuzamos fejlesztéshez):**
```typescript
const mockTemplates = [
  { id: 'standard_door', name: 'Standard beltéri ajtó', category: 'doors' },
  { id: 'premium_door', name: 'Prémium furnér ajtó', category: 'doors' },
  // ... 3 more
]

const mockMaterials = {
  core: ['chipboard_18mm', 'mdf_18mm', 'plywood_18mm'],
  veneer: ['oak_natural', 'walnut_dark', 'ash_white'],
  edge: ['pvc_oak', 'abs_walnut', 'veneer_edge']
}
```

---

### 2. BOMPreviewCard.tsx

**Path:** `frontend/joinerytech-portal/src/components/BOMPreviewCard.tsx`

**Funkció:** Anyaglista előnézet árral, PDF export gomb

**Props:**
```typescript
interface BOMPreviewProps {
  configId: string
  bomItems: Array<{
    itemType: 'material' | 'veneer' | 'fitting'
    name: string
    quantity: number
    unit: string
    unitPrice: number
    totalPrice: number
  }>
  estimatedPrice: number
}
```

**UI:**
- **Táblázat:** item type, name, quantity, unit, unit price, total price oszlopok
- **Összesítés:** Total Material Cost + Estimated Labor = Total Price
- **PDF Export gomb:** "Letöltés PDF-ben" → window.open(pdfUrl)
- **"Create Work Order" gomb:** navigate to WorkOrderSummary form

**API Hook:**
```typescript
const { data: bomPreview } = useQuery({
  queryKey: ['bom-preview', configId],
  queryFn: () => fetch(`/api/products/preview/${configId}`).then(res => res.json())
})
```

**Mock Data:**
```typescript
const mockBOM = {
  bomItems: [
    { itemType: 'material', name: 'Forgácslap 18mm', quantity: 1, unit: 'db', unitPrice: 8500, totalPrice: 8500 },
    { itemType: 'veneer', name: 'Tölgy furnér', quantity: 2, unit: 'm²', unitPrice: 5200, totalPrice: 10400 },
    { itemType: 'fitting', name: 'Rejtett 3D zsanér', quantity: 3, unit: 'db', unitPrice: 1200, totalPrice: 3600 }
  ],
  estimatedPrice: 45000
}
```

---

### 3. WorkOrderSummary.tsx

**Path:** `frontend/joinerytech-portal/src/pages/WorkOrderSummary.tsx`

**Funkció:** Gyártási lap összefoglaló + letöltés (work order form + PDF)

**Form Fields:**
```typescript
interface WorkOrderForm {
  configId: string
  quantity: number
  deliveryDate: string  // YYYY-MM-DD
  customerRef: string
  notes: string
}
```

**UI:**
- **Config Summary Card:** Product type, dimensions, materials (read-only)
- **Order Details Form:**
  - Quantity (number input, min: 1)
  - Delivery Date (date picker)
  - Customer Reference (text input)
  - Notes (textarea)
- **"Generate Work Order" gomb** → POST /api/work-orders
- **Success:** munkalap PDF letöltés + BOM részletes táblázat

**API Hook:**
```typescript
const { mutate: createWorkOrder, data: workOrder } = useMutation({
  mutationFn: (data: WorkOrderForm) =>
    fetch('/api/work-orders', {
      method: 'POST',
      body: JSON.stringify(data)
    }).then(res => res.json()),
  onSuccess: (data) => {
    // Download PDF
    window.open(data.pdfUrl, '_blank')
    // Display BOM items table
    setBOMItems(data.bomItems)
  }
})
```

**BOM Items Table (munkalap generálás után):**
- Columns: Item, Quantity, Unit, Total Price, Supplier, In Stock, To Order
- Footer: Total Material Cost, Estimated Labor, Total Cost
- Scheduling info: Scheduled Start, Estimated Completion

**Mock Data:**
```typescript
const mockWorkOrder = {
  workOrderId: 'wo_2026_042',
  pdfUrl: '/mock/work-order-sheet.pdf',
  bomItems: [ /* same as BOMPreviewCard */ ],
  totalMaterialCost: 225000,
  estimatedLabor: 80000,
  totalCost: 305000,
  scheduledStart: '2026-07-08',
  estimatedCompletion: '2026-07-14'
}
```

---

## State Management — Consensus Döntés

**Stack (Plan-B egyszerűsége):**
- **Zustand** — lokál UI state (wizard step, drawer open/close, filters)
- **TanStack Query** — szerver state (API cache, refetch, optimistic updates)
- **localStorage** — draft mentés (form adatok túlélése refresh-nél)

**Példa Zustand store:**
```typescript
// stores/configuratorStore.ts
import { create } from 'zustand'

interface ConfiguratorStore {
  currentStep: number
  setStep: (step: number) => void
  config: ConfigState
  updateConfig: (partial: Partial<ConfigState>) => void
}

export const useConfiguratorStore = create<ConfiguratorStore>((set) => ({
  currentStep: 1,
  setStep: (step) => set({ currentStep: step }),
  config: initialConfigState,
  updateConfig: (partial) => set((state) => ({ config: { ...state.config, ...partial } }))
}))
```

---

## API Integration (Joinery backend)

### 1. POST /api/products/configure

**Request:** ConfigState object
**Response:** `{ configId, previewUrl, estimatedPrice, bomPreview[] }`

**Hook:** `useMutation` (TanStack Query)

### 2. POST /api/work-orders

**Request:** WorkOrderForm object
**Response:** `{ workOrderId, pdfUrl, bomItems[], totalCost, scheduledStart }`

**Hook:** `useMutation` (TanStack Query)

---

## Routing (React Router v6)

**Új route-ok:**
```tsx
<Route path="/configurator" element={<ProductConfiguratorWizard />} />
<Route path="/configurator/preview/:configId" element={<BOMPreviewCard />} />
<Route path="/work-orders/new/:configId" element={<WorkOrderSummary />} />
```

**Navigation flow:**
1. `/configurator` → wizard 4 lépés → submit
2. `/configurator/preview/:configId` → BOM preview → "Create Work Order" button
3. `/work-orders/new/:configId` → work order form → submit → PDF download

---

## Definition of Done

### Komponensek
- [ ] `ProductConfiguratorWizard.tsx` komponens létezik (4-step wizard)
- [ ] `BOMPreviewCard.tsx` komponens létezik (BOM táblázat + PDF gomb)
- [ ] `WorkOrderSummary.tsx` komponens létezik (form + BOM detail table)
- [ ] Zustand store: `configuratorStore.ts` (wizard step state)

### API integráció
- [ ] `POST /api/products/configure` hook (useMutation)
- [ ] `POST /api/work-orders` hook (useMutation)
- [ ] Error handling (API validation errors display)
- [ ] Success handling (navigate, PDF download)

### UI/UX
- [ ] Stepper navigation (4 lépés vizuális jelzés)
- [ ] Validation per step (dimension ranges, required fields)
- [ ] Loading states (spinner, disabled buttons)
- [ ] Error messages (toast vagy inline error)

### Tesztek
- [ ] Unit tests: ProductConfiguratorWizard form validation (Vitest)
- [ ] Integration tests: wizard submit → API call (Mock Service Worker)
- [ ] E2E tests: configurator flow end-to-end (Playwright)
- [ ] TypeScript build: 0 errors
- [ ] Vitest suite: all tests passing

### Routing
- [ ] `/configurator` route configured
- [ ] `/configurator/preview/:configId` route configured
- [ ] `/work-orders/new/:configId` route configured
- [ ] Navigation links (header, sidebar) updated

---

## Párhuzamos fejlesztés Mock Data-val

**Lehetséges:** Joinery backend endpoints (MSG-JOINERY-058) párhuzamosan fejlődnek.

**Stratégia:**
1. **Mock data használat Phase 1-ben** (lásd mock examples fent)
2. **Mock Service Worker (MSW)** setup `/api/products/configure`, `/api/work-orders` endpointokhoz
3. **Backend DONE után:** MSW mock disable, real API integration verify
4. **Integration test:** real backend call ellenőrzés

**MSW példa:**
```typescript
// mocks/handlers.ts
import { rest } from 'msw'

export const handlers = [
  rest.post('/api/products/configure', (req, res, ctx) => {
    return res(ctx.json(mockConfigResult))
  }),
  rest.post('/api/work-orders', (req, res, ctx) => {
    return res(ctx.json(mockWorkOrder))
  })
]
```

---

## TypeScript Type Safety (Consensus döntés)

**OpenAPI → TypeScript codegen:**
1. Joinery backend Swagger/OpenAPI spec (`/swagger.json`)
2. OpenAPI Generator: `npx openapi-typescript-codegen --input swagger.json --output src/api`
3. Automatikus TypeScript types + API client functions
4. Runtime validáció: Zod schemák

**Zod példa:**
```typescript
import { z } from 'zod'

const ConfigStateSchema = z.object({
  productType: z.string(),
  dimensions: z.object({
    width: z.number().min(700).max(1100),
    height: z.number().min(1900).max(2200),
    thickness: z.number()
  }),
  materials: z.object({
    core: z.string(),
    veneer: z.string(),
    edge: z.string()
  }),
  fittings: z.object({
    hinge: z.string(),
    handle: z.string(),
    lock: z.string()
  })
})

// Használat validációnál:
ConfigStateSchema.parse(formData)
```

---

## Timeline

- **Phase 1 start:** 2026-06-21 (ma)
- **Phase 1 target:** 2026-07-19 (4 hét, 2 sprint)
- **Backend dependency:** MSG-JOINERY-058 (párhuzamos mock-kal)
- **Doorstar Q3 Soft Launch:** 2026-09-30

**Estimated effort:** 2-3 sprint (4-6 hét)

---

## Érintett terminálok (dependencies)

- **Joinery terminál:** Backend endpoints (MSG-JOINERY-058) — párhuzamos, mock-kal dolgozz
- **Orch terminál:** API routing verify (MSG-ORCH-007)
- **E2E terminál:** Integration tests (Phase 1 végén)

---

## Referenciák

- **Planning consensus:** `docs/planning/queue/2026-06-21_0055_consensus.md`
- **Joinery backend task:** `docs/mailbox/joinery/inbox/2026-06-21_058_joinery-e2e-configurator-endpoints.md`
- **Frontend:** `frontend/joinerytech-portal/` (port 3001)

---

**NOTE:** MSG-FE-086 (Joinery API Integration) KÜLÖNBÖZŐ feladat! Az MSG-FE-086 a **meglévő** backend API-k bekötése (material-req, hardware-list, cutting plans). Ez az MSG-FE-087 az **ÚJ** konfigurátor komponensek (productType selection, BOM wizard).

Kérdések esetén eszkalálj Root-nak vagy Conductor-nak.
