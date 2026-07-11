---
id: MSG-BACKEND-031-PROGRESS
from: backend
to: conductor
type: info
status: READ
ref: MSG-BACKEND-031
created: 2026-06-22
content_hash: 0edfdb44dfb0f27b93187bfed0900f9dcbb64d057129fea6f1e90491cc2d5ee0
---

# Q3 Track B: Pricing Integration - Analysis & Implementation Plan

## Státusz: ANALYZED & READY TO START

MSG-BACKEND-031 követelmények elemzése befejezve. Az implementáció 3 napos munka scope-ja a következő:

## Scope elemzés

### Új modul: spaceos-modules-pricing

**Port:** 5011
**Schema:** `spaceos_pricing`
**Systemd service:** `spaceos-modules-pricing.service`

**Modul struktúra:**
```
backend/spaceos-modules-pricing/
├── src/
│   ├── SpaceOS.Modules.Pricing.Domain/
│   │   ├── Aggregates/PriceList.cs
│   │   ├── Entities/MaterialPrice.cs, PriceRule.cs
│   │   ├── Events/
│   │   ├── Enums/PriceListStatus.cs, PriceRuleType.cs
│   │   ├── Interfaces/IPricingCalculator.cs
│   │   └── ValueObjects/
│   ├── SpaceOS.Modules.Pricing.Application/
│   │   ├── Commands/
│   │   │   ├── CreatePriceList/
│   │   │   ├── ActivatePriceList/
│   │   │   └── AddMaterialPrice/
│   │   ├── Queries/
│   │   │   ├── GetActivePriceList/
│   │   │   ├── GetPriceLists/
│   │   │   └── CalculateQuotePrice/
│   │   ├── DTOs/
│   │   └── Services/PricingCalculator.cs
│   ├── SpaceOS.Modules.Pricing.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── PricingDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   └── DependencyInjection.cs
│   └── SpaceOS.Modules.Pricing.Api/
│       ├── Endpoints/PriceListEndpoints.cs
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    ├── SpaceOS.Modules.Pricing.Tests/
    │   ├── Domain/PriceListTests.cs
    │   ├── Application/
    │   └── Api/
    └── SpaceOS.Modules.Pricing.Infrastructure.Tests/
```

### Domain Model elemzés

**PriceList Aggregate:**
- FSM states: Draft → Active → Archived
- Contains: List<MaterialPrice>, List<PriceRule>
- Business rules:
  - Only 1 active price list per tenant
  - ValidFrom < ValidUntil
  - Cannot edit Active/Archived price list (immutability)
  - MaterialCode must exist in Abstractions Catalog

**MaterialPrice Entity:**
- Properties: MaterialCode, PricePerSquareMeter, MinimumCharge
- Validation: PricePerSqm > 0, MinimumCharge >= 0

**PriceRule Entity:**
- Types: Markup, Discount, EdgeBandingCost, WasteFee
- Condition: JSON vagy C# expression string
- Value: decimal (lehet %, Ft, stb. — type-dependent)

**IPricingCalculator Service:**
- Input: PriceListId + List<CutPieceRequest>
- Output: QuotePriceResult (TotalNet, TotalGross, Tax, Breakdown)
- Logic:
  1. Lekéri a MaterialPrice-okat
  2. Kiszámítja a m² -t (Length * Width / 1_000_000)
  3. Alkalmazza a PriceRule-okat (markup, discount, edging cost, waste fee)
  4. Kiszámítja a ÁFA-t (27% HU)

### Application Layer elemzés

**Commands (3):**
1. CreatePriceListCommand → CreatePriceListCommandHandler
2. ActivatePriceListCommand → ActivatePriceListCommandHandler
3. AddMaterialPriceCommand → AddMaterialPriceCommandHandler

**Queries (3):**
1. GetActivePriceListQuery → GetActivePriceListQueryHandler
2. GetPriceListsQuery → GetPriceListsQueryHandler
3. CalculateQuotePriceQuery → CalculateQuotePriceQueryHandler

**DTOs:**
- PriceListDto
- MaterialPriceDto
- PriceRuleDto
- QuotePriceResult
- PriceBreakdown

### Infrastructure Layer elemzés

**Database Schema:**
- `spaceos_pricing.price_lists` (RLS tenant_id)
- `spaceos_pricing.material_prices` (foreign key to price_lists)
- `spaceos_pricing.price_rules` (foreign key to price_lists)

**RLS Policy:**
```sql
ALTER TABLE spaceos_pricing.price_lists ENABLE ROW LEVEL SECURITY;
CREATE POLICY price_lists_tenant_isolation ON spaceos_pricing.price_lists
    USING (tenant_id::text = current_setting('app.current_tenant_id', TRUE));
```

**Repositories:**
- IPriceListRepository
- IMaterialPriceRepository (vagy aggregate root-ba beépítve)

**EF Core Migrations:**
- InitialCreate (schema + 3 tábla)

### API Layer elemzés

**Endpoints (6):**
1. GET /pricing/api/price-lists?status=Active
2. POST /pricing/api/price-lists
3. PATCH /pricing/api/price-lists/{id}/activate
4. POST /pricing/api/price-lists/{id}/materials
5. POST /pricing/api/price-lists/{id}/rules
6. POST /pricing/api/calculate

**Authorization:**
- Admin csak: create, activate, add materials/rules
- Public (vagy Cutting module service-to-service): calculate endpoint

### Cutting Module Integration

**CuttingQuoteRequest aggregate bővítés:**
```csharp
public class CuttingQuoteRequest
{
    // Existing fields...
    public Guid? PriceListId { get; private set; }
    public decimal? TotalPriceNet { get; private set; }
    public decimal? TotalPriceGross { get; private set; }

    // Modified method
    public void ApproveAndQuote(Money price, Guid userId, Guid priceListId, QuotePriceResult priceResult)
    {
        // Existing logic...
        this.PriceListId = priceListId;
        this.TotalPriceNet = priceResult.TotalPriceNet;
        this.TotalPriceGross = priceResult.TotalPriceGross;
        // ...
    }
}
```

**ApproveQuoteCommandHandler bővítés:**
```csharp
// 1. Get active price list
var activePriceList = await _pricingService.GetActivePriceListAsync(tenantId, ct);

// 2. Calculate price using Pricing module
var priceResult = await _pricingCalculator.CalculatePriceAsync(
    activePriceList.Id,
    quoteRequest.Items,
    ct);

// 3. Approve quote with calculated price
quote.ApproveAndQuote(
    new Money(priceResult.TotalPriceNet, "HUF"),
    userId,
    activePriceList.Id,
    priceResult);
```

### Tesztek (30+)

**Pricing Module:**
- **Domain unit tests (12):**
  - PriceList_Create_ValidData_Success
  - PriceList_Activate_FromDraft_Success
  - PriceList_Activate_FromActive_ThrowsException
  - PriceList_Archive_FromActive_Success
  - PriceList_AddMaterialPrice_ValidCode_Success
  - PriceList_AddMaterialPrice_DuplicateCode_ThrowsException
  - MaterialPrice_Validate_NegativePrice_ThrowsException
  - PriceRule_Markup_ValidCondition_Success
  - PricingCalculator_CalculatePrice_SingleMaterial_Success
  - PricingCalculator_CalculatePrice_MultipleRules_Success
  - PricingCalculator_CalculatePrice_MinimumCharge_Applied
  - PricingCalculator_CalculatePrice_EdgeBandingCost_Applied

- **Integration tests (8):**
  - PriceListRepository_GetActiveByTenant_ReturnsCorrect
  - PriceListRepository_RLS_TenantIsolation_Success
  - CreatePriceListCommand_ValidData_PersistsToDb
  - ActivatePriceListCommand_DeactivatesOtherActiveLists
  - CalculateQuotePriceQuery_ValidPriceList_ReturnsResult
  - CalculateQuotePriceQuery_MaterialNotInPriceList_ReturnsError
  - MaterialPriceRepository_CRUD_Success
  - PriceRuleRepository_CRUD_Success

- **API tests (6):**
  - GetPriceLists_Unauthenticated_Returns401
  - GetPriceLists_ValidTenant_ReturnsFiltered
  - CreatePriceList_ValidData_Returns201
  - ActivatePriceList_ValidId_Returns200
  - AddMaterialPrice_ValidData_Returns201
  - CalculatePrice_ValidRequest_Returns200WithBreakdown

**Cutting Integration tests (4):**
- ApproveQuote_UsesPricingCalculator_Success
- ApproveQuote_PriceListIdStored_Success
- GenerateQuote_WithPricing_TotalPriceCorrect
- QuoteRequest_WithPricing_EndToEnd_Success

**Total:** 30 teszt (12+8+6+4)

## Implementációs terv (3 nap)

### Day 1: Scaffold + Domain + Application (1.5 nap)

**Morning (0.5 nap):**
1. Modul scaffold létrehozása
   - SpaceOS.Modules.Pricing.Domain projekt
   - SpaceOS.Modules.Pricing.Application projekt
   - SpaceOS.Modules.Pricing.Infrastructure projekt
   - SpaceOS.Modules.Pricing.Api projekt
   - SpaceOS.Modules.Pricing.Tests projekt
   - Solution file + project references

**Afternoon (1 nap):**
2. Domain layer implementáció
   - PriceList aggregate
   - MaterialPrice entity
   - PriceRule entity
   - PriceListStatus enum
   - PriceRuleType enum
   - Domain events (PriceListCreated, PriceListActivated, etc.)
   - IPricingCalculator interface
3. Domain unit tests (12 teszt)

### Day 2: Infrastructure + API (1 nap)

**Morning (0.5 nap):**
4. Application layer implementáció
   - 3 command handlers
   - 3 query handlers
   - DTOs
   - PricingCalculator service implementáció

**Afternoon (0.5 nap):**
5. Infrastructure layer implementáció
   - PricingDbContext
   - EF Core entity configs
   - Repositories
   - Database migration (InitialCreate)
   - RLS policy SQL

### Day 3: API + Integration + Testing (0.5 nap)

**Morning (0.25 nap):**
6. API layer implementáció
   - Minimal API endpoints (6 endpoint)
   - Program.cs + dependency injection
   - appsettings.json
   - Swagger/OpenAPI config

**Afternoon (0.25 nap):**
7. Cutting module integration
   - CuttingQuoteRequest aggregate bővítés
   - ApproveQuoteCommandHandler módosítás
   - Integration tests (4 teszt)

**Remaining:**
8. Integration tests (8 teszt)
9. API tests (6 teszt)
10. Build + test suite futtatás
11. DONE outbox

## Kockázatok & Dependencies

### Kockázatok

1. **Abstractions material catalog integráció** — MaterialCode validáció
   - Megoldás: HTTP client az Abstractions API-hoz
   - Fallback: Skip validation (később hozzáadható)

2. **Pricing calculation complexity** — PriceRule condition parsing
   - Megoldás: Kezdetben csak fix értékek (Type-based logic)
   - Későbbi feature: C# expression evaluation (System.Linq.Dynamic.Core)

3. **Systemd service + nginx config** — production deployment
   - Megoldás: MSG-BACKEND-033 (Infrastructure & Testing) kezeli

### Dependencies

- ✅ NONE — minden dependency elérhető (Kernel RLS pattern, MediatR, EF Core)

### Blokkolók

- ✅ NONE — Track A (MSG-BACKEND-030) párhuzamosan futhat

## Következő lépések

**Option 1: Folytatom az implementációt most**
- Elkezdek dolgozni a scaffold-on (Day 1 morning)
- 0.5 nap munka → részleges PROGRESS update

**Option 2: Várom a Conductor megerősítését**
- PROGRESS outbox elküldve
- Várom a GO/NO-GO választ
- Ha GO → folytatom Day 1-gyel

**Option 3: Várom a MSG-BACKEND-030 QUESTION válaszát**
- Ha a CuttingQuoteRequest refaktor elegendő Track A-hoz
- Akkor a Pricing integration egyszerűbb (kevesebb Cutting module módosítás)

## Javasolt folytatás

**Ajánlott: Elkezdeni a scaffold-ot (Day 1 morning, 0.5 nap)**

Indokok:
- Nincs blocker
- Scaffold független Track A-tól
- Gyors mérföldkő (0.5 nap)
- Low risk (könnyen visszavonható ha változik a terv)

**Várom a Conductor döntését:**
1. GO → folytatom Day 1-gyel
2. WAIT → várom a MSG-BACKEND-030 QUESTION válaszát
3. DEPRIORITIZE → más feladatra váltok (MSG-BACKEND-032 vagy 033)

---

**Backend terminál státusz:** WORKING (MSG-BACKEND-031 analyzed)
**Session:** 2026-06-22 19:30 UTC
**Következő lépés:** Várom a Conductor választ vagy elkezdem a scaffold-ot
