---
id: MSG-BACKEND-031
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: deda7e6e7e539820b893d9bc295f66126cc971b311db17051e5d1e4b14a54414
---

# Q3 Track B: Pricing Integration - Pricing Engine + Trade World

## Összefoglaló

Implementáld a **Pricing Engine**-t és integráld a **Trade World** (Kereskedelem világ) frontend-et, hogy árazási szabályokat lehessen kezelni és alkalmazni a vágási feladatokra.

## Scope

**Modul:** `spaceos-modules-cutting` + új `spaceos-modules-pricing` (külön schema)
**Időkeret:** 3 nap (Track B)
**Prioritás:** HIGH — Quote-to-Cash pipeline része

## Implementációs lépések

### 1. Pricing Module Scaffold (0.5 nap)

**Új modul:** `backend/spaceos-modules-pricing/`

```
SpaceOS.Modules.Pricing/
  Domain/
    Entities/
      PriceList.cs
      PriceRule.cs
      MaterialPrice.cs
    Events/
    ValueObjects/
  Application/
    Commands/
    Queries/
  Infrastructure/
    Persistence/
      PricingDbContext.cs
  Api/
    Controllers/
      PriceListsController.cs
```

**Port:** 5011
**Schema:** `spaceos_pricing`
**Systemd:** `spaceos-modules-pricing.service`

### 2. Domain Layer (1 nap)

**Aggregate:** `PriceList`

```csharp
public class PriceList : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } // "Standard 2026 Q2"
    public PriceListStatus Status { get; private set; } // Draft, Active, Archived
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public List<MaterialPrice> MaterialPrices { get; private set; }
    public List<PriceRule> Rules { get; private set; }

    // Factory
    public static PriceList Create(Guid tenantId, string name, DateTime validFrom);

    // FSM
    public void Activate();
    public void Archive();
    public void AddMaterialPrice(string materialCode, decimal pricePerSqm);
    public void AddRule(PriceRule rule);
}

public enum PriceListStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public class MaterialPrice : Entity<Guid>
{
    public string MaterialCode { get; private set; }
    public decimal PricePerSquareMeter { get; private set; }
    public decimal MinimumCharge { get; private set; } // Ha <0.5m² akkor fixed ár
}

public class PriceRule : Entity<Guid>
{
    public PriceRuleType Type { get; private set; } // Markup, Discount, EdgeBandingCost, WasteFee
    public string Condition { get; private set; } // JSON vagy expression
    public decimal Value { get; private set; }
}

public enum PriceRuleType
{
    Markup = 0,          // Pl. +20% ha <5db
    Discount = 1,        // Pl. -10% ha >100db
    EdgeBandingCost = 2, // Pl. +500 Ft/m
    WasteFee = 3         // Pl. +5% ha waste >15%
}
```

**Domain service:** `IPricingCalculator`

```csharp
public interface IPricingCalculator
{
    Task<QuotePriceResult> CalculatePriceAsync(
        Guid priceListId,
        List<CutPieceRequest> pieces,
        CancellationToken ct);
}

public record QuotePriceResult(
    decimal TotalPriceNet,
    decimal TotalPriceGross,
    decimal TaxAmount,
    List<PriceBreakdown> Breakdown
);

public record PriceBreakdown(
    string MaterialCode,
    int Quantity,
    decimal SquareMeters,
    decimal PricePerSqm,
    decimal Subtotal
);
```

### 3. Application Layer (0.5 nap)

**Commands:**
```csharp
public record CreatePriceListCommand(Guid TenantId, string Name, DateTime ValidFrom) : IRequest<Guid>;
public record ActivatePriceListCommand(Guid PriceListId) : IRequest;
public record AddMaterialPriceCommand(Guid PriceListId, string MaterialCode, decimal Price) : IRequest;
```

**Queries:**
```csharp
public record GetActivePriceListQuery(Guid TenantId) : IRequest<PriceListDto>;
public record GetPriceListsQuery(Guid TenantId, PriceListStatus? Status) : IRequest<List<PriceListDto>>;
public record CalculateQuotePriceQuery(Guid PriceListId, List<CutPieceRequestDto> Pieces) : IRequest<QuotePriceResult>;
```

### 4. Infrastructure Layer (0.5 nap)

**Database:**
```sql
CREATE SCHEMA spaceos_pricing;

CREATE TABLE spaceos_pricing.price_lists (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    status INT NOT NULL,
    valid_from TIMESTAMP NOT NULL,
    valid_until TIMESTAMP,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE TABLE spaceos_pricing.material_prices (
    id UUID PRIMARY KEY,
    price_list_id UUID NOT NULL REFERENCES spaceos_pricing.price_lists(id),
    material_code VARCHAR(100) NOT NULL,
    price_per_sqm NUMERIC(10,2) NOT NULL,
    minimum_charge NUMERIC(10,2) NOT NULL DEFAULT 0
);

CREATE TABLE spaceos_pricing.price_rules (
    id UUID PRIMARY KEY,
    price_list_id UUID NOT NULL REFERENCES spaceos_pricing.price_lists(id),
    type INT NOT NULL,
    condition TEXT,
    value NUMERIC(10,2) NOT NULL
);
```

**RLS:**
```sql
ALTER TABLE spaceos_pricing.price_lists ENABLE ROW LEVEL SECURITY;
CREATE POLICY price_lists_tenant_isolation ON spaceos_pricing.price_lists
    USING (tenant_id::text = current_setting('app.current_tenant_id', TRUE));
```

### 5. API Layer (0.5 nap)

**Endpoints:**

```http
GET /pricing/api/price-lists?status=Active
POST /pricing/api/price-lists
PATCH /pricing/api/price-lists/{id}/activate
POST /pricing/api/price-lists/{id}/materials
POST /pricing/api/price-lists/{id}/rules

POST /pricing/api/calculate
Content-Type: application/json
{
  "priceListId": "guid",
  "pieces": [
    {
      "materialCode": "PAL-18-WHITE",
      "length": 2000,
      "width": 600,
      "quantity": 10,
      "edgeBanding": "All"
    }
  ]
}

Response: 200 OK
{
  "totalPriceNet": 120000,
  "totalPriceGross": 152400,
  "taxAmount": 32400,
  "breakdown": [
    {
      "materialCode": "PAL-18-WHITE",
      "quantity": 10,
      "squareMeters": 12.0,
      "pricePerSqm": 10000,
      "subtotal": 120000
    }
  ]
}
```

### 6. Cutting Module Integration

**Modify `PublicQuoteRequest` aggregate:**

```csharp
public void GenerateQuote(Guid priceListId, QuotePriceResult priceResult)
{
    // Store price calculation result
    this.PriceListId = priceListId;
    this.TotalPriceNet = priceResult.TotalPriceNet;
    this.TotalPriceGross = priceResult.TotalPriceGross;
    this.Status = QuoteRequestStatus.Quoted;
    this.ProcessedAt = DateTime.UtcNow;

    AddDomainEvent(new QuoteGeneratedEvent(Id, priceResult));
}
```

**Update `GenerateQuoteCommandHandler`:**

```csharp
var priceResult = await _pricingCalculator.CalculatePriceAsync(
    activePriceListId,
    quoteRequest.Pieces,
    ct);

quoteRequest.GenerateQuote(activePriceListId, priceResult);
```

### 7. Tesztek

**Pricing Module:**
- 12 unit tests (domain FSM, price calculation logic)
- 8 integration tests (EF Core + RLS)
- 6 API tests

**Cutting Integration:**
- 4 integration tests (quote generation + pricing)

**Összesen:** 30+ teszt

## Definition of Done

✅ Pricing module scaffold + systemd service
✅ PriceList aggregate + MaterialPrice + PriceRule domain model
✅ IPricingCalculator service implementálva
✅ Database schema + migrations
✅ API endpoints (`/pricing/api/*`)
✅ Cutting module integration (GenerateQuote uses pricing)
✅ 30+ teszt pass
✅ nginx route: `/pricing/` → localhost:5011
✅ OpenAPI docs

## Blokkolók

**NONE** — Track A (MSG-BACKEND-030) párhuzamosan futhat.

## Kapcsolódó feladatok

- **Frontend:** MSG-FRONTEND-019 (Trade World - Price List Management UI)
- **Track A:** MSG-BACKEND-030 (Customer Portal API)

## Referenciák

- Kernel RLS pattern: `backend/spaceos-kernel/Infrastructure/Persistence/`
- Abstractions material catalog: `/abstractions/api/modules/materials`

---

**Határidő:** 2026-06-27 (Track B, 3 nap)
**Assigned to:** Backend terminal
**Model:** sonnet
