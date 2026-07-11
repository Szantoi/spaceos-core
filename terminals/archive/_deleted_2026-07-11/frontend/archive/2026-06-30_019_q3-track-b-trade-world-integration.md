---
id: MSG-FRONTEND-019
from: conductor
to: frontend
type: task
priority: high
status: UNREAD
model: sonnet
ref: TRACK-B-DISPATCH
created: 2026-06-30
---

# Track B Phase 1: Trade World Integration UI

## Context

**Track A Status:** 70% complete
- ✅ Frontend: Complete (MSG-061, all forms + hooks)
- ✅ Backend Phase 3-4-5: Complete (MSG-091, 64/64 tests)
- ⏳ Backend Phase 1-2: In progress (MSG-078)

**Root Decision:** "Proceed with Track B dispatch — don't wait for Track A Phase 1-2 completion"

**Parallel Work:** Backend is simultaneously implementing Pricing Rule Engine (MSG-031)

---

## Track B Overview

Track B introduces **supplier network pricing** to SpaceOS:
1. **Pricing Rule Engine** (Backend MSG-031) — Supplier pricing FSM
2. **Trade World Integration** (Frontend MSG-019 — THIS) — Supplier network UI
3. **Catalog Pricing** (Backend) — Product-to-supplier price mapping

---

## MSG-019: Trade World Integration UI

### Feature: Supplier Catalog & Quote Comparison

**Objective:** Build a React UI for SpaceOS suppliers to discover pricing, compare quotes, and integrate into their workflows.

### Pages & Components

#### 1. **Supplier Catalog Page** (`/trade-world/suppliers`)

**Purpose:** Browse suppliers by category (door, cabinet, panel) + pricing

**Components:**
- `SupplierFilterPanel` (left sidebar, 150px width)
  - Category filter: checkbox list (door, cabinet, panel, etc.)
  - Region filter: dropdown (regions supported by supplier)
  - Min/Max price range slider
  - In stock toggle
  - Apply filters button

- `SupplierCardGrid` (main content)
  - Grid layout: 3 columns (responsive: 1 col mobile, 2 col tablet, 3 col desktop)
  - Each card:
    - Supplier logo (100×100px)
    - Name (bold, clickable link to detail)
    - Region (small gray text)
    - Rating: 5-star (aggregated from supplier reviews table)
    - Base price range: "50–200 HUF / unit" (from Pricing Rule Engine)
    - Lead time: "7–14 days" (sourced from PricingRule.leadTimeAdjustments)
    - "View Details" button (→ detail page)
    - "Request Quote" button (→ quote form)

- `SupplierDetailPage` (`/trade-world/suppliers/:id`)
  - Supplier info: name, region, contact, description
  - Pricing table (from Pricing Rule Engine API):
    ```
    | Product Category | Base Price | Qty 5-10 | Qty 10+ | Lead Time |
    |------------------|-----------|----------|---------|-----------|
    | Door             | 100 HUF   | 95 HUF   | 90 HUF  | 7–14 days |
    | Cabinet          | 150 HUF   | 140 HUF  | 130 HUF | 7–14 days |
    ```
  - Request Quote form (embedded)
  - Supplier reviews (rating, comment, date)

#### 2. **Quote Request Form** (`/trade-world/request-quote`)

**Purpose:** Request a quote from one or multiple suppliers

**Form Fields:**
- Supplier selection: multi-select (autocomplete)
- Product category: dropdown (door, cabinet, panel)
- Quantity: integer input + slider (1–1000)
- Lead time required: date picker ("I need this by...")
- Special requirements: textarea (material type, finish, customization)
- Estimated budget: currency input (HUF)
- Contact info: prefilled from Portal user (editable)
- Submit button: "Send Quote Request" → POST `/api/quote-requests`

**Validation:**
- At least 1 supplier selected
- Quantity > 0
- Lead time > today

**Success Message:** "Quote request sent! You'll receive responses within 24 hours."

#### 3. **Quote Comparison Page** (`/trade-world/quote-comparison`)

**Purpose:** Compare multiple supplier quotes side-by-side

**Components:**
- `QuoteTable`
  - Columns: Original Request Info, Supplier 1, Supplier 2, Supplier 3, ...
  - Rows:
    - Unit price
    - Total price (unit × quantity)
    - Lead time offered
    - Special conditions
    - "Accept Quote" button (→ order)
    - "Request Revision" button (→ reply form)

- `LowestPriceHighlight` — automatically highlight lowest price in each row (green background)

- `AcceptQuoteModal`
  - Confirm: supplier, quantity, unit price, total, lead time
  - Delivery address selection (from Portal user addresses)
  - "Confirm Order" → POST `/api/orders` (creates binding order)

### Styling & UX

**Design System:**
- Reuse Portal v2 Tailwind config (`tailwind.config.ts`)
- Colors:
  - Primary: Portal blue (#3B82F6)
  - Success: green (#10B981)
  - Alert: red (#EF4444)
- Typography: Portal font stack (Inter, sans-serif)

**Responsive:**
- Mobile (375px–767px): 1-column grid, full-width forms
- Tablet (768px–1023px): 2-column grid, sidebar collapses to top menu
- Desktop (1024px+): 3-column grid, sidebar visible

**Accessibility:**
- Form labels with `<label htmlFor="...">` (WCAG 2.1 AA)
- Keyboard navigation: Tab through filters, supplier cards, buttons
- ARIA labels for star ratings, disabled buttons
- Alt text for supplier logos

### API Consumption

**Backend Endpoints to consume:**

1. **GET `/api/suppliers`** (filter params: category, region, minPrice, maxPrice)
   - Response: `{ suppliers: SupplierDto[] }` (from Kernel Supplier entity)
   - Used by: SupplierFilterPanel, SupplierCardGrid

2. **GET `/api/suppliers/{id}/pricing-rules`** (consumed from MSG-031)
   - Response: `{ rules: PricingRuleDto[] }`
   - Used by: SupplierDetailPage (pricing table)

3. **POST `/api/quote-requests`** (creates QuoteRequest aggregate)
   - Request: `CreateQuoteRequestCommand` (supplierId[], productCategory, quantity, leadTime, budget, contactInfo)
   - Response: `{ quoteRequestId, status: "sent" }`
   - Used by: Quote Request Form submit

4. **GET `/api/quote-requests/{id}`** — Fetch single request with responses
   - Response: QuoteRequest + attached Quotes (from suppliers)
   - Used by: Quote Comparison page

### React Hook Strategy

**Hooks to implement:**

1. **`useSupplierFilter()`** — State management for filters
   - Returns: { filters, setFilters, filteredSuppliers }
   - Uses: `useCallback` to memoize filter logic

2. **`useQuoteRequest()`** — Quote request form state
   - Returns: { formData, setFormData, isSubmitting, error }
   - Handles: form validation, API POST, error handling

3. **`useQuoteComparison(quoteRequestId)`** — Fetch + memoize quote comparison
   - Returns: { quotes, lowestPrice, isLoading, error }
   - Uses: `useMemo` to recalculate comparison on quotes change

4. **`useSupplierDetail(supplierId)`** — Fetch supplier + pricing rules
   - Returns: { supplier, pricingRules, isLoading }
   - Uses: React Query cache

### Component Tree

```
TradeWorldLayout
├── SupplierCatalog
│   ├── SupplierFilterPanel
│   │   ├── CategoryFilter
│   │   ├── RegionFilter
│   │   ├── PriceRangeFilter
│   │   └── ApplyButton
│   └── SupplierCardGrid
│       └── SupplierCard[] × N
├── SupplierDetail
│   ├── SupplierHeader
│   ├── PricingTable
│   └── ReviewsSection
├── QuoteRequestForm
│   ├── SupplierMultiSelect
│   ├── ProductCategorySelect
│   ├── QuantityInput
│   ├── LeadTimeInput
│   ├── RequirementsTextarea
│   ├── BudgetInput
│   └── SubmitButton
└── QuoteComparison
    ├── QuoteTable
    ├── LowestPriceHighlight
    └── AcceptQuoteModal
```

### File Structure

```
client/src/
├── pages/
│   └── TradeWorldPage.tsx
├── components/
│   └── TradeWorld/
│       ├── SupplierCatalog.tsx
│       ├── SupplierCard.tsx
│       ├── SupplierDetailPage.tsx
│       ├── SupplierFilterPanel.tsx
│       ├── QuoteRequestForm.tsx
│       ├── QuoteComparisonPage.tsx
│       └── QuoteTable.tsx
├── hooks/
│   ├── useSupplierFilter.ts
│   ├── useQuoteRequest.ts
│   ├── useQuoteComparison.ts
│   └── useSupplierDetail.ts
├── types/
│   └── tradeWorld.ts (TypeScript interfaces)
└── services/
    └── tradeWorldService.ts (API client)
```

### Testing Requirements

**Unit Tests:**
- Filter logic: verify category, region, price filtering works correctly
- Price comparison: "lowest price" highlight correct
- Form validation: required fields, quantity > 0, lead time > today

**E2E Tests (Playwright):**
- Flow 1: Browse suppliers → view pricing → request quote
- Flow 2: Request quote → receive quotes → compare → accept order
- Mobile responsiveness: filters collapse to top menu

**Test Coverage:** 80%+ for components and hooks

### Acceptance Criteria

- [ ] All 5 pages/components render without errors
- [ ] Filter logic works: filters persist in URL (use URLSearchParams)
- [ ] API calls to Backend succeed (mock if needed during development)
- [ ] Pricing table displays correctly (handle edge cases: no pricing rules)
- [ ] Quote form validates and submits
- [ ] Quote comparison shows lowest price highlighted
- [ ] Responsive design verified on mobile (375px), tablet (768px), desktop (1024px)
- [ ] E2E tests: 2 main flows passing
- [ ] Accessibility: WCAG 2.1 AA validated (use axe-core)
- [ ] 80%+ test coverage

### Implementation Phases

**Phase 1: Components & Styling** (Estimate: 2 days)
- [ ] Create all components (SupplierCard, FilterPanel, PricingTable, etc.)
- [ ] Apply Tailwind styling + responsive design
- [ ] Wire up routing (`/trade-world/*` paths)

**Phase 2: State Management & Hooks** (Estimate: 2 days)
- [ ] Implement `useSupplierFilter()`, `useQuoteRequest()`, etc.
- [ ] Add form validation logic
- [ ] Error boundary for API failures

**Phase 3: API Integration** (Estimate: 1–2 days)
- [ ] Connect to Backend endpoints (suppliers, pricing rules, quote requests)
- [ ] Handle loading/error states gracefully
- [ ] Add retry logic for failed API calls

**Phase 4: Testing & Polish** (Estimate: 2 days)
- [ ] Unit tests (80%+ coverage)
- [ ] E2E tests (Playwright)
- [ ] Accessibility audit (axe-core)
- [ ] Mobile/tablet testing

### Dependency Notes

- **Backend MSG-031:** Pricing Rule Engine API (required for pricing table)
- **Backend Supplier entity:** Must exist in Kernel (Track A Phase 1-2)
- **Portal Auth:** Must already work (MSG-061 complete)

---

## Start When

**Immediately** — Parallel with Backend MSG-031 (Pricing Rule Engine)

Both can develop independently:
- Frontend develops UI with mock API data
- Backend develops API endpoints
- Integration test when both ready

---

**Conductor**
2026-06-30 — Track B Phase 1 dispatch
