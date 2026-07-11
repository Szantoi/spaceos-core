---
id: MSG-BACKEND-031
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: TRACK-B-DISPATCH
created: 2026-06-30
---

# Track B Phase 1: Pricing Rule Engine Specification

## Context

**Track A Status:** 70% complete
- ✅ Frontend: Complete (MSG-061)
- ✅ Backend Phase 3-4-5: Complete (MSG-091, 64/64 tests)
- ⏳ Backend Phase 1-2: In progress (MSG-078, estimated ~4 hrs)

**Root Decision:** "Proceed with Track B dispatch — don't wait for Track A Phase 1-2 completion"

---

## Track B Overview

Track B introduces **pricing intelligence** to SpaceOS:
1. **Pricing Rule Engine** (Backend) — FSM-driven pricing logic
2. **Trade World Integration** (Frontend) — Supplier network UI
3. **Catalog Pricing** (Backend) — Product → supplier → price mapping

---

## MSG-031: Pricing Rule Engine Specification

### Feature: Dynamic Pricing FSM

**Objective:** Build a finite state machine for pricing rules that supports:
- Supplier-specific pricing strategies
- Quantity breakpoints (1-5, 5-10, 10+ units)
- Lead time adjustments (rush fee, standard, bulk discount)
- Material surcharges (oak, walnut, custom finishes)

### Requirements

**Domain Model:**
```
Aggregate: PricingRule
  - id: Guid
  - supplierId: Guid
  - productCategory: string (e.g., "door", "cabinet", "panel")
  - basePricePerUnit: decimal
  - quantityBreakpoints: QuantityBreakpoint[] (FSM states)
  - leadTimeAdjustments: LeadTimeAdjustment[]
  - materialSurcharges: MaterialSurcharge[]
  - status: PricingRuleStatus (draft | active | archived)
  - createdAt: DateTime
  - updatedAt: DateTime
```

**Finite State Machine (PricingRule lifecycle):**
```
draft → active → archived
  ↓
  └─ validation_error (→ draft)
```

**Quantity Breakpoint FSM:**
```
Quantity ∈ [1, 5) → price = basePricePerUnit (no discount)
Quantity ∈ [5, 10) → price = basePricePerUnit × 0.95 (5% discount)
Quantity ∈ [10, ∞) → price = basePricePerUnit × 0.90 (10% discount)
```

### API Endpoints (to implement)

1. **POST /api/pricing-rules** — Create new pricing rule
   - Request: `CreatePricingRuleCommand` (supplierId, productCategory, basePricePerUnit, breakpoints)
   - Response: `PricingRuleDto` (id, status: draft)
   - Validation: Must specify at least 1 quantity breakpoint

2. **GET /api/pricing-rules/{id}** — Fetch pricing rule detail
   - Response: Full `PricingRuleDto` with all breakpoints

3. **PUT /api/pricing-rules/{id}/activate** — Transition to active state
   - Request: `ActivatePricingRuleCommand`
   - Validation: basePricePerUnit must be > 0; all breakpoints must have valid ranges
   - Response: Updated `PricingRuleDto` (status: active)

4. **POST /api/pricing-rules/{id}/calculate-price** — Calculate price for a specific quantity/leadtime/material combo
   - Request: `CalculatePriceRequest` (quantity: int, leadDays: int, materialId: Guid)
   - Response: `PriceCalculationResult` (price: decimal, breakdown: string)
   - Example breakdown: "Base: 100 HUF × Qty Breakpoint (0.95) × LeadTime Adj (1.1) = 104.5 HUF"

### Implementation Phases

**Phase 1: Domain Model & FSM** (Estimate: 1 day)
- [ ] Create `PricingRule` aggregate root
- [ ] Create `QuantityBreakpoint`, `LeadTimeAdjustment`, `MaterialSurcharge` value objects
- [ ] Implement FSM state transitions (draft → active → archived)
- [ ] Add validation: no overlapping breakpoint ranges

**Phase 2: Repository & Persistence** (Estimate: 1 day)
- [ ] Create `IPricingRuleRepository` interface (Contracts.NuGet)
- [ ] Implement `PricingRuleRepository` (Cutting.Infrastructure)
- [ ] Create migration: `AddPricingRulesTable`
- [ ] Database schema: normalized breakpoints table (FK to PricingRule)

**Phase 3: Command Handlers & API** (Estimate: 2 days)
- [ ] `CreatePricingRuleCommandHandler`
- [ ] `ActivatePricingRuleCommandHandler`
- [ ] `CalculatePriceCommandHandler` (core pricing logic)
- [ ] API endpoints (OpenAPI spec in Swagger)
- [ ] Unit tests: state transitions, quantity breakpoint logic, price calculation

**Phase 4: Integration Tests** (Estimate: 1 day)
- [ ] E2E: Create rule → activate → calculate price
- [ ] Boundary cases: edge breakpoints (4, 5, 9, 10, 11 units)
- [ ] Error cases: invalid quantities, archived rules

### Testing Requirements

**Unit Tests (Phase 3):**
- Quantity breakpoint selection: [1, 5, 9, 10, 11] units
- Lead time adjustments: [7, 14, 30] days
- Price calculation with multiple adjustments: base × qty discount × leadtime adj × material adj
- FSM transitions: valid and invalid state changes

**Integration Tests (Phase 4):**
- Full E2E workflow: create → activate → use in price calc
- Database persistence: rule survives commit + reload
- Concurrent updates: 2 processes modify same rule simultaneously (expect optimistic concurrency error)

### Database Schema

```sql
CREATE TABLE spaceos_cutting.pricing_rules (
  id UNIQUEIDENTIFIER PRIMARY KEY,
  supplier_id UNIQUEIDENTIFIER NOT NULL,
  product_category VARCHAR(50) NOT NULL,
  base_price_per_unit DECIMAL(10,2) NOT NULL,
  status VARCHAR(20) NOT NULL CHECK (status IN ('draft', 'active', 'archived')),
  created_at DATETIME2 NOT NULL,
  updated_at DATETIME2 NOT NULL,
  version INT NOT NULL DEFAULT 1,
  FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
);

CREATE TABLE spaceos_cutting.quantity_breakpoints (
  id UNIQUEIDENTIFIER PRIMARY KEY,
  pricing_rule_id UNIQUEIDENTIFIER NOT NULL,
  min_quantity INT NOT NULL,
  max_quantity INT NOT NULL,
  discount_percent DECIMAL(5,2) NOT NULL,
  FOREIGN KEY (pricing_rule_id) REFERENCES pricing_rules(id) ON DELETE CASCADE,
  CHECK (min_quantity < max_quantity),
  CHECK (discount_percent >= 0 AND discount_percent <= 100)
);

CREATE INDEX idx_pricing_rules_supplier_status ON spaceos_cutting.pricing_rules(supplier_id, status);
```

### Error Handling

- **ValidationException:** basePricePerUnit ≤ 0, overlapping breakpoint ranges, invalid lead time
- **RuleNotFoundException:** pricing rule doesn't exist
- **InvalidStateTransitionException:** Can't activate archived rule
- **OptimisticConcurrencyException:** Version mismatch (expect and handle)

### Acceptance Criteria

- [ ] Domain model compiles with no warnings
- [ ] 4 API endpoints implemented (Create, Get, Activate, CalculatePrice)
- [ ] 100% test coverage (unit + integration) for pricing calculation logic
- [ ] All 64 unit tests passing (including new pricing tests)
- [ ] Migration runs successfully (test DB)
- [ ] OpenAPI spec auto-generated from attributes

### Dependency Notes

- **Kernel:** Requires Kernel v1.2 supplier entity (in Track A Phase 1-2)
- **Portal:** Pricing UI depends on this API (Track B Frontend — MSG-019)

---

## Next Steps

1. ✅ **This message:** Specification review
2. ⏳ **Your implementation:** Build Phase 1-4 as outlined
3. ⏳ **Frontend:** MSG-019 (Trade World UI) will consume this API
4. ⏳ **Integration:** Catalog pricing module will tie supplier rules to products

---

**Start when:** Immediately (parallel with Track A Phase 1-2)

**Conductor**
2026-06-30 — Track B Phase 1 dispatch
