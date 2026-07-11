# SpaceOS Cutting Q3 Track B — Pricing Integration

**Version:** v1
**Created:** 2026-06-23
**Status:** Approved (Root MSG-CONDUCTOR-007)
**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 3 days (Week 2-3)
**Priority:** HIGH

---

## Executive Summary

Implement an automated pricing engine for panel cutting quotes that integrates with the Quote Request flow (Track A) and exposes pricing configuration in a new "Trade" world in the frontend portal.

**Current gap:** Manual pricing in `ApproveQuote` command (admin enters price manually)
**Q3 Track B adds:**
1. Pricing Engine (backend service)
2. Price calculation rules (material, complexity, quantity)
3. Trade World (frontend UI for pricing config)
4. Auto-pricing integration with Quote Request flow

---

## Business Context

**Target customer:** 2. ügyfél (Lapszabász KKV)
**Use case:** Automated quote pricing based on:
- Material type (MDF, plywood, chipboard, etc.)
- Panel dimensions (m²)
- Cutting complexity (number of cuts, shapes)
- Quantity discounts (bulk orders)

**Current state:** Admin manually enters price when approving quote
**Desired state:** System auto-calculates price, admin reviews and adjusts if needed

---

## Acceptance Criteria

**Backend (2 days):**
- [ ] Pricing Engine service (`IPricingEngine`, `PricingEngine`)
- [ ] Pricing Rules domain model (PriceList, MaterialPricing, ComplexityModifier)
- [ ] API endpoints:
  - `POST /api/cutting/pricing/calculate` — calculate price for quote
  - `GET /api/cutting/pricing/rules` — list pricing rules
  - `PUT /api/cutting/pricing/rules/{id}` — update pricing rule
  - `GET /api/cutting/pricing/preview` — preview price before submitting quote
- [ ] Database schema: `PriceLists`, `MaterialPricing`, `ComplexityModifiers`
- [ ] Unit tests: Pricing calculation (20 test cases), rules CRUD (95%)

**Frontend (1 day):**
- [ ] Trade World (`/w/trade` route)
  - Dashboard: Revenue metrics, quote conversion rate, avg quote price
  - Pricing Rules panel: Material price list, complexity modifiers
  - Edit pricing rule SlideOver
- [ ] Integrate pricing preview into PublicQuoteRequestForm (optional, show estimated price)
- [ ] Integration tests: Trade World (5 scenarios)

**Integration:**
- [ ] E2E test: Quote Request → Auto-pricing → Approve with adjusted price
- [ ] Pricing calculation accuracy: 100% (verified with 10 real-world scenarios)

**Documentation:**
- [ ] Pricing algorithm documentation
- [ ] API docs: Pricing endpoints

---

## Technical Design

### Architecture

```
Quote Request (Track A)
  ↓
Pricing Engine
  ↓ Calculate base price (material + dimensions)
  ↓ Apply complexity modifier (cut count)
  ↓ Apply quantity discount
  ↓ Return calculated price
  ↓
ApproveQuote Command (auto-fill price)
  ↓
Admin reviews price in Trade World
  ↓
Admin approves or adjusts price
```

### Pricing Engine Algorithm

**Base Price Calculation:**
```
BasePrice = (Material.PricePerM² × PanelArea) + BaseFee
```

**Complexity Modifier:**
```
ComplexityScore = CutCount × 0.1 + ShapeComplexity
ComplexityMultiplier = 1.0 + (ComplexityScore × ComplexityModifier)
```

**Quantity Discount:**
```
QuantityDiscount =
  if Quantity >= 50: 0.85 (15% discount)
  if Quantity >= 20: 0.90 (10% discount)
  if Quantity >= 10: 0.95 (5% discount)
  else: 1.0 (no discount)
```

**Final Price:**
```
FinalPrice = BasePrice × ComplexityMultiplier × QuantityDiscount
```

### Database Schema

**PriceLists table:**
```sql
CREATE TABLE "PriceLists" (
  "Id" uuid PRIMARY KEY,
  "TenantId" uuid NOT NULL,
  "Name" text NOT NULL,
  "EffectiveFrom" timestamptz NOT NULL,
  "EffectiveTo" timestamptz,
  "IsActive" boolean NOT NULL DEFAULT true,
  CONSTRAINT fk_tenant FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id")
);
```

**MaterialPricing table:**
```sql
CREATE TABLE "MaterialPricing" (
  "Id" uuid PRIMARY KEY,
  "PriceListId" uuid NOT NULL,
  "MaterialType" text NOT NULL, -- MDF, Plywood, Chipboard, etc.
  "PricePerSquareMeter" decimal(10,2) NOT NULL,
  "Currency" text NOT NULL DEFAULT 'HUF',
  CONSTRAINT fk_pricelist FOREIGN KEY ("PriceListId") REFERENCES "PriceLists"("Id")
);
```

**ComplexityModifiers table:**
```sql
CREATE TABLE "ComplexityModifiers" (
  "Id" uuid PRIMARY KEY,
  "PriceListId" uuid NOT NULL,
  "ModifierType" text NOT NULL, -- CutCount, ShapeComplexity, EdgeBanding
  "MultiplierValue" decimal(5,3) NOT NULL,
  CONSTRAINT fk_pricelist FOREIGN KEY ("PriceListId") REFERENCES "PriceLists"("Id")
);
```

### API Endpoints

**1. Calculate Price (Backend)**
```http
POST /api/cutting/pricing/calculate
Authorization: Bearer {jwt}

Request:
{
  "materialType": "MDF",
  "panelWidth": 2500,
  "panelHeight": 1250,
  "quantity": 10,
  "cutCount": 5,
  "shapeComplexity": "simple"
}

Response:
{
  "basePrice": 12500.00,
  "complexityMultiplier": 1.05,
  "quantityDiscount": 0.95,
  "finalPrice": 12468.75,
  "currency": "HUF",
  "breakdown": {
    "materialCost": 10000.00,
    "baseFee": 2500.00,
    "complexityAdjustment": 625.00,
    "discountAmount": -656.25
  }
}
```

**2. Get Pricing Rules**
```http
GET /api/cutting/pricing/rules
Authorization: Bearer {jwt}

Response:
{
  "priceLists": [
    {
      "id": "...",
      "name": "Default Price List 2026",
      "effectiveFrom": "2026-01-01T00:00:00Z",
      "isActive": true,
      "materials": [
        { "type": "MDF", "pricePerM2": 4000.00, "currency": "HUF" },
        { "type": "Plywood", "pricePerM2": 6500.00, "currency": "HUF" }
      ],
      "modifiers": [
        { "type": "CutCount", "multiplier": 0.10 },
        { "type": "ShapeComplexity", "multiplier": 0.15 }
      ]
    }
  ]
}
```

**3. Update Pricing Rule**
```http
PUT /api/cutting/pricing/rules/{priceListId}
Authorization: Bearer {jwt}

Request:
{
  "materials": [
    { "type": "MDF", "pricePerM2": 4200.00 }
  ]
}
```

### Frontend Components

**1. TradeWorld.tsx** (new world)
```tsx
// Route: /w/trade
<TradeWorld>
  <TradeDashboard /> {/* Revenue KPI-k, conversion rate */}
  <PricingRulesPanel /> {/* Material árlisták, modifiers */}
</TradeWorld>
```

**2. PricingRulesPanel.tsx**
```tsx
- Material pricing table (type, price/m², edit button)
- Complexity modifiers table (type, multiplier, edit button)
- Edit SlideOver: Update material price
```

**3. PricingPreview integration (optional)**
```tsx
// PublicQuoteRequestForm.tsx
// Show estimated price range before submitting
<PricingPreviewBadge estimatedPrice={calculatedPrice} />
```

---

## Implementation Tasks

### Backend (MSG-031)

**Sub-tasks:**
1. Domain layer: PriceList, MaterialPricing, ComplexityModifier aggregates
2. Application layer: CalculatePriceCommand, GetPricingRulesQuery, UpdatePricingRuleCommand
3. Infrastructure: PricingRepository, Database migrations
4. API: PricingEndpoints.cs (3 endpoints)
5. PricingEngine service implementation
6. Unit tests: Pricing calculation (20 scenarios), CRUD (95%)
7. Integration tests: E2E pricing flow (5 scenarios)

**Files to create:**
- `Domain/PriceList.cs`, `Domain/MaterialPricing.cs`, `Domain/ComplexityModifier.cs`
- `Application/Commands/CalculatePrice/CalculatePriceCommand.cs`
- `Application/Queries/GetPricingRules/GetPricingRulesQuery.cs`
- `Application/Commands/UpdatePricingRule/UpdatePricingRuleCommand.cs`
- `Services/IPricingEngine.cs`, `Services/PricingEngine.cs`
- `Endpoints/PricingEndpoints.cs`
- `Migrations/AddPricingTables.cs`
- `Tests/Domain/PriceListTests.cs`, `Tests/Services/PricingEngineTests.cs`

**Files to modify:**
- `ApproveQuoteCommand` — integrate auto-pricing

**Estimate:** 2 days (16 hours)

---

### Frontend (MSG-019)

**Sub-tasks:**
1. Create Trade World component structure
2. TradeDashboard: Revenue KPI-k, conversion rate chart
3. PricingRulesPanel: Material árlisták, modifiers
4. Edit pricing rule SlideOver
5. Integration tests: Trade World (5 scenarios)
6. Optional: PricingPreview in PublicQuoteRequestForm

**Files to create:**
- `src/pages/TradeWorld.tsx`
- `src/components/TradeDashboard.tsx`
- `src/components/PricingRulesPanel.tsx`
- `src/components/EditPricingRuleSlideOver.tsx`
- `src/hooks/usePricingRules.ts`
- `src/pages/TradeWorld.test.tsx`

**Files to modify:**
- `src/App.tsx` (add `/w/trade` route)

**Estimate:** 1 day (8 hours)

---

## Dependencies

**Blocks:**
- Track C (ShopFloor Integration) — pricing affects machine queue priority

**Blocked by:**
- Track A (Customer Portal) — pricing integrates with Quote Request flow

---

## Success Metrics

- Auto-pricing accuracy: 95%+ (admin adjusts <5% of quotes)
- Price calculation time: <100ms
- Trade World adoption: 80%+ admins use it weekly
- Quote approval time: 50% reduction (manual → auto)

---

## Risks & Mitigation

| Risk | Mitigation |
|---|---|
| Pricing algorithm inaccurate | Validate with 10 real-world scenarios from 2. ügyfél |
| Material price volatility | Admin can adjust prices in Trade World |
| Complex shapes hard to price | Start with simple shapes, iterate complexity modifier |
| Quantity discount edge cases | Test with 20+ quantity scenarios |

---

## References

- **ApproveQuote command:** `ApproveQuoteHandler.cs`
- **Codebase_Status.md:** Sales API pricing (lines 235-240)
- **ADR-002:** Modular Monolith (Kernel IParametricProduct)
