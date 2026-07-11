---
id: MSG-BACKEND-031
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_B_Pricing_Integration_v1.md
created: 2026-06-23
---

# Q3 Track B — Pricing Integration (Backend)

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 2 days
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Implement an automated pricing engine for panel cutting quotes. Integrates with Quote Request flow (Track A) and exposes pricing configuration API for the Trade World frontend.

**Current gap:** Manual pricing in `ApproveQuote` command (admin enters price manually)

**Track B Backend adds:**
1. Pricing Engine service with algorithm
2. Pricing Rules domain model (PriceList, MaterialPricing, ComplexityModifier)
3. API endpoints for price calculation and rules management
4. Auto-pricing integration with Quote Request flow

---

## Acceptance Criteria

- [ ] **Pricing Engine service** (`IPricingEngine`, `PricingEngine`)
  - Calculate base price (material × area)
  - Apply complexity modifier (cut count, shapes)
  - Apply quantity discounts
  - Return price breakdown
- [ ] **Domain models**
  - `PriceList` aggregate
  - `MaterialPricing` value object
  - `ComplexityModifier` value object
- [ ] **API endpoints** (4 endpoints)
  - `POST /api/cutting/pricing/calculate` — calculate price
  - `GET /api/cutting/pricing/rules` — list pricing rules
  - `PUT /api/cutting/pricing/rules/{id}` — update rule
  - `GET /api/cutting/pricing/preview` — preview price
- [ ] **Database schema**
  - `PriceLists`, `MaterialPricing`, `ComplexityModifiers` tables
- [ ] **Unit tests**
  - Pricing calculation: 20 test cases (edge cases, discounts, complexity)
  - Rules CRUD: 95% coverage
- [ ] **Integration tests**
  - E2E: Quote Request → Auto-pricing → Approve with price

---

## Pricing Algorithm

### Base Price
```
BasePrice = (Material.PricePerM² × PanelArea) + BaseFee
PanelArea = (Width × Height) / 1,000,000  // mm² to m²
```

### Complexity Modifier
```
ComplexityScore = CutCount × 0.1 + ShapeComplexity
ComplexityMultiplier = 1.0 + (ComplexityScore × ComplexityModifier)

ShapeComplexity:
  - Simple (rectangle): 0.0
  - Medium (L-shape, U-shape): 0.5
  - Complex (curves, angles): 1.0
```

### Quantity Discount
```
QuantityDiscount =
  if Quantity >= 50: 0.85 (15% discount)
  if Quantity >= 20: 0.90 (10% discount)
  if Quantity >= 10: 0.95 (5% discount)
  else: 1.0 (no discount)
```

### Final Price
```
FinalPrice = BasePrice × ComplexityMultiplier × QuantityDiscount
```

---

## Technical Implementation

### 1. Database Migrations

**File:** `Migrations/AddPricingTables.cs`

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

CREATE TABLE "MaterialPricing" (
  "Id" uuid PRIMARY KEY,
  "PriceListId" uuid NOT NULL,
  "MaterialType" text NOT NULL,
  "PricePerSquareMeter" decimal(10,2) NOT NULL,
  "Currency" text NOT NULL DEFAULT 'HUF',
  CONSTRAINT fk_pricelist FOREIGN KEY ("PriceListId") REFERENCES "PriceLists"("Id")
);

CREATE TABLE "ComplexityModifiers" (
  "Id" uuid PRIMARY KEY,
  "PriceListId" uuid NOT NULL,
  "ModifierType" text NOT NULL,
  "MultiplierValue" decimal(5,3) NOT NULL,
  CONSTRAINT fk_pricelist FOREIGN KEY ("PriceListId") REFERENCES "PriceLists"("Id")
);

CREATE INDEX idx_pricelists_tenant ON "PriceLists"("TenantId");
CREATE INDEX idx_materialpricing_pricelist ON "MaterialPricing"("PriceListId");
CREATE INDEX idx_complexitymodifiers_pricelist ON "ComplexityModifiers"("PriceListId");
```

**Seed data:**
```csharp
// Default price list for Doorstar
migrationBuilder.Sql(@"
  INSERT INTO ""PriceLists"" (""Id"", ""TenantId"", ""Name"", ""EffectiveFrom"", ""IsActive"")
  VALUES (gen_random_uuid(), (SELECT ""Id"" FROM ""Tenants"" WHERE ""Subdomain"" = 'doorstar'), 'Default 2026', '2026-01-01', true);

  INSERT INTO ""MaterialPricing"" (""Id"", ""PriceListId"", ""MaterialType"", ""PricePerSquareMeter"", ""Currency"")
  VALUES
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'MDF', 4000.00, 'HUF'),
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'Plywood', 6500.00, 'HUF'),
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'Chipboard', 3500.00, 'HUF');

  INSERT INTO ""ComplexityModifiers"" (""Id"", ""PriceListId"", ""ModifierType"", ""MultiplierValue"")
  VALUES
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'CutCount', 0.10),
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'ShapeComplexity', 0.15);
");
```

### 2. Domain Models

**File:** `Domain/PriceList.cs`

```csharp
public class PriceList
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    public List<MaterialPricing> Materials { get; private set; } = new();
    public List<ComplexityModifier> Modifiers { get; private set; } = new();

    // Factory method
    public static PriceList Create(Guid tenantId, string name, DateTime effectiveFrom)
    {
        // Validation + domain rules
    }
}
```

**File:** `Domain/MaterialPricing.cs`

**File:** `Domain/ComplexityModifier.cs`

### 3. Pricing Engine Service

**File:** `Services/IPricingEngine.cs`

```csharp
public interface IPricingEngine
{
    Task<PriceCalculationResult> CalculatePrice(
        Guid tenantId,
        string materialType,
        int panelWidth,
        int panelHeight,
        int quantity,
        int cutCount,
        string shapeComplexity,
        CancellationToken ct);
}
```

**File:** `Services/PricingEngine.cs`

```csharp
public class PricingEngine : IPricingEngine
{
    public async Task<PriceCalculationResult> CalculatePrice(...)
    {
        // 1. Get active price list for tenant
        var priceList = await GetActivePriceList(tenantId, ct);

        // 2. Calculate base price
        var panelAreaM2 = (panelWidth * panelHeight) / 1_000_000.0m;
        var materialPrice = priceList.Materials.First(m => m.Type == materialType).PricePerM2;
        var baseFee = 2500m; // TODO: configurable
        var basePrice = (materialPrice * panelAreaM2) + baseFee;

        // 3. Apply complexity modifier
        var complexityScore = (cutCount * 0.1m) + GetShapeComplexityScore(shapeComplexity);
        var complexityModifier = priceList.Modifiers.First(m => m.Type == "ShapeComplexity").Multiplier;
        var complexityMultiplier = 1.0m + (complexityScore * complexityModifier);

        // 4. Apply quantity discount
        var quantityDiscount = GetQuantityDiscount(quantity);

        // 5. Calculate final price
        var finalPrice = basePrice * complexityMultiplier * quantityDiscount;

        return new PriceCalculationResult
        {
            BasePrice = basePrice,
            ComplexityMultiplier = complexityMultiplier,
            QuantityDiscount = quantityDiscount,
            FinalPrice = finalPrice,
            Currency = "HUF",
            Breakdown = new PriceBreakdown
            {
                MaterialCost = materialPrice * panelAreaM2,
                BaseFee = baseFee,
                ComplexityAdjustment = basePrice * (complexityMultiplier - 1.0m),
                DiscountAmount = basePrice * complexityMultiplier * (1.0m - quantityDiscount)
            }
        };
    }

    private decimal GetShapeComplexityScore(string complexity) => complexity switch
    {
        "simple" => 0.0m,
        "medium" => 0.5m,
        "complex" => 1.0m,
        _ => 0.0m
    };

    private decimal GetQuantityDiscount(int quantity) => quantity switch
    {
        >= 50 => 0.85m,
        >= 20 => 0.90m,
        >= 10 => 0.95m,
        _ => 1.0m
    };
}
```

### 4. Application Layer

**File:** `Application/Commands/CalculatePrice/CalculatePriceCommand.cs`

**File:** `Application/Queries/GetPricingRules/GetPricingRulesQuery.cs`

**File:** `Application/Commands/UpdatePricingRule/UpdatePricingRuleCommand.cs`

### 5. API Endpoints

**File:** `Endpoints/PricingEndpoints.cs`

```csharp
public static class PricingEndpoints
{
    public static void MapPricingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cutting/pricing")
            .RequireAuthorization();

        group.MapPost("/calculate", CalculatePrice);
        group.MapGet("/rules", GetPricingRules);
        group.MapPut("/rules/{priceListId}", UpdatePricingRule);
        group.MapGet("/preview", PreviewPrice);
    }

    private static async Task<IResult> CalculatePrice(
        [FromBody] CalculatePriceRequest req,
        IPricingEngine pricingEngine,
        HttpContext context,
        CancellationToken ct)
    {
        var tenantId = context.User.GetTenantId();
        var result = await pricingEngine.CalculatePrice(
            tenantId, req.MaterialType, req.PanelWidth, req.PanelHeight,
            req.Quantity, req.CutCount, req.ShapeComplexity, ct);
        return Results.Ok(result);
    }

    // Implement other endpoints...
}
```

### 6. Update ApproveQuote Command

**File:** `Application/Commands/ApproveQuote/ApproveQuoteHandler.cs`

**Add auto-pricing integration:**
```csharp
// Before approving, auto-calculate price if not provided
if (command.Price == null)
{
    var priceResult = await _pricingEngine.CalculatePrice(...);
    command.Price = priceResult.FinalPrice;
}
```

---

## Files to Create

1. `Migrations/YYYYMMDDHHMMSS_AddPricingTables.cs`
2. `Domain/PriceList.cs`
3. `Domain/MaterialPricing.cs`
4. `Domain/ComplexityModifier.cs`
5. `Application/Commands/CalculatePrice/CalculatePriceCommand.cs`
6. `Application/Queries/GetPricingRules/GetPricingRulesQuery.cs`
7. `Application/Commands/UpdatePricingRule/UpdatePricingRuleCommand.cs`
8. `Services/IPricingEngine.cs`
9. `Services/PricingEngine.cs`
10. `Endpoints/PricingEndpoints.cs`
11. `Tests/Domain/PriceListTests.cs`
12. `Tests/Services/PricingEngineTests.cs`
13. `Tests/Integration/PricingE2ETests.cs`

---

## Files to Modify

1. `Program.cs`
   - Add DI: `builder.Services.AddScoped<IPricingEngine, PricingEngine>();`
   - Register `PricingEndpoints`
2. `Application/Commands/ApproveQuote/ApproveQuoteHandler.cs`
   - Add auto-pricing integration

---

## Testing Requirements

### Unit Tests (PricingEngineTests.cs) — 20 scenarios

1. Simple rectangle, MDF, 1 piece → correct base price
2. Complex shape, Plywood, 50 pieces → discount applied
3. Edge case: 0 cuts → no complexity modifier
4. Edge case: 9 pieces → no discount
5. Edge case: 10 pieces → 5% discount applied
6. Edge case: 19 pieces → 5% discount
7. Edge case: 20 pieces → 10% discount
8. Edge case: 49 pieces → 10% discount
9. Edge case: 50 pieces → 15% discount
10. Quantity discount + complexity → both applied
11. Unknown material → throws MaterialNotFoundException
12. Inactive price list → uses fallback
13. Missing complexity modifier → throws ConfigurationException
14. Negative quantity → throws ArgumentException
15. Zero panel dimensions → throws ArgumentException
16. Price calculation < 100ms (performance)
17. Multiple materials in same calculation
18. Currency conversion (future-proof)
19. Price rounding (2 decimal places)
20. Breakdown sum equals final price

### Integration Tests
1. E2E: CalculatePrice → valid result
2. E2E: GetPricingRules → returns active price list
3. E2E: UpdatePricingRule → price list updated
4. E2E: ApproveQuote with auto-pricing → price calculated
5. E2E: PreviewPrice (public endpoint) → no auth required

---

## Dependencies

**Blocks:**
- Track C (ShopFloor Integration) — pricing affects queue priority

**Blocked by:**
- Track A (Customer Portal) — pricing integrates with Quote Request flow

**References:**
- Track B spec: `/opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_B_Pricing_Integration_v1.md`
- ApproveQuote command: `Application/Commands/ApproveQuote/ApproveQuoteHandler.cs`

---

## Build & Test Gate

```bash
cd /opt/spaceos/backend/spaceos-modules-cutting

# Build
dotnet build --configuration Release

# Run tests
dotnet test --filter "Category=Unit&FullyQualifiedName~PricingEngine" --logger "console;verbosity=detailed"
dotnet test --filter "Category=Integration&FullyQualifiedName~Pricing" --logger "console;verbosity=detailed"

# Migration
dotnet ef migrations add AddPricingTables
dotnet ef database update
```

**Expected:** All 20+ tests pass, pricing accuracy 100%.

---

**Estimated effort:** 2 days (16 hours)
**Model:** sonnet
**Priority:** HIGH
