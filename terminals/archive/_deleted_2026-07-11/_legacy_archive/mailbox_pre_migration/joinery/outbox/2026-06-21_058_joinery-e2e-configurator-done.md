---
id: MSG-JOINERY-058-DONE
from: joinery
to: conductor
type: done
status: READ
ref: MSG-JOINERY-058
created: 2026-06-21
---

# Joinery E2E Flow Phase 1 — Backend Konfigurátor Endpoints DONE

## Összefoglaló

Implementáltam a 3 új backend endpoint-ot a product konfiguráció és work order generáláshoz:

### Implementált endpoint-ok

1. **POST /api/products/configure**
   - Input: productType, dimensions, materials, fittings
   - Logic: Template-based validation + BOM calculation + pricing
   - Output: `{ configId (full GUID), previewUrl, estimatedPrice, bomPreview[] }`
   - Database: ProductConfigurations table (tenant-scoped, RLS enabled)

2. **POST /api/work-orders**
   - Input: configId (GUID), quantity, deliveryDate, customerRef, notes
   - Logic: BOM multiplier + cost calculation + scheduling
   - Output: `{ workOrderId (GUID), pdfUrl, bomItems[], totalCost, scheduledStart }`
   - Database: WorkOrders table (tenant-scoped, RLS enabled)

3. **GET /api/work-orders/:id/sheet.pdf**
   - Output: PDF binary (QuestPDF-generated work order sheet)
   - Content-Type: application/pdf
   - Security: RLS tenant isolation + no-cache headers

### Változtatott fájlok

**Domain Layer:**
- `SpaceOS.Modules.Joinery.Domain/Entities/ProductConfiguration.cs` (már létezett)
- `SpaceOS.Modules.Joinery.Domain/Entities/WorkOrder.cs` (már létezett)
- `SpaceOS.Modules.Joinery.Domain/Entities/ProductTemplate.cs` (már létezett)

**Application Layer:**
- `SpaceOS.Modules.Joinery.Application/Products/DTOs/` (request/response DTOs)
- `SpaceOS.Modules.Joinery.Application/Products/Commands/ConfigureProduct/ConfigureProductCommandHandler.cs` (FIX: configId format)
- `SpaceOS.Modules.Joinery.Application/Products/Commands/CreateWorkOrder/CreateWorkOrderCommandHandler.cs` (FIX: Random → deterministic hash)
- `SpaceOS.Modules.Joinery.Application/Products/Services/IProductConfiguratorService.cs`
- `SpaceOS.Modules.Joinery.Application/Products/Services/IWorkOrderPdfService.cs`

**Infrastructure Layer:**
- `SpaceOS.Modules.Joinery.Infrastructure/Services/ProductConfiguratorService.cs` (JSONB-based validation + BOM + pricing)
- `SpaceOS.Modules.Joinery.Infrastructure/Services/WorkOrderPdfService.cs` (QuestPDF generation)
- `SpaceOS.Modules.Joinery.Infrastructure/Persistence/Repositories/` (ProductConfiguration, WorkOrder, ProductTemplate repos)
- `SpaceOS.Modules.Joinery.Infrastructure/Persistence/Configurations/` (EF Core configurations)
- `SpaceOS.Modules.Joinery.Infrastructure/Migrations/20260621000001_J004_ConfiguratorAndWorkOrders.cs` (3 táblák + RLS + 5 seed)

**API Layer:**
- `SpaceOS.Modules.Joinery.Api/Endpoints/ProductEndpoints.cs` (FIX: ConfigId parsing simplified to full GUID)
- `SpaceOS.Modules.Joinery.Api/Program.cs` (endpoint registration már megvolt)

**Tests:**
- `SpaceOS.Modules.Joinery.Tests/Products/ProductConfiguratorServiceTests.cs` (20 unit test)
- `SpaceOS.Modules.Joinery.Tests/Api/ProductApiTests.cs` (18 integration test, added work order + PDF tests)

### Database Schema

**Migration:** `20260621000001_J004_ConfiguratorAndWorkOrders.cs`

```sql
-- ProductTemplates (tenant-independent config)
CREATE TABLE spaceos_joinery."ProductTemplates" (
    "Id"                varchar(50)     NOT NULL PRIMARY KEY,
    "Name"              varchar(100)    NOT NULL,
    "DimensionRules"    jsonb           NOT NULL,
    "AllowedMaterials"  jsonb           NOT NULL,
    "AllowedFittings"   jsonb           NOT NULL,
    "PricingRules"      jsonb           NOT NULL,
    "LeadTimeDays"      integer         NOT NULL DEFAULT 7
);

-- ProductConfigurations (tenant-scoped, RLS FORCE)
CREATE TABLE spaceos_joinery."ProductConfigurations" (
    "Id"                uuid            NOT NULL PRIMARY KEY,
    "TenantId"          uuid            NOT NULL,
    "ProductType"       varchar(50)     NOT NULL,
    "Params"            jsonb           NOT NULL,
    "BomSnapshot"       jsonb           NOT NULL,
    "EstimatedPrice"    numeric(10,2)   NOT NULL,
    "PreviewUrl"        text            NULL,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now()
);
-- RLS: tenant_isolation policy

-- WorkOrders (tenant-scoped, RLS FORCE)
CREATE TABLE spaceos_joinery."WorkOrders" (
    "Id"                    uuid            NOT NULL PRIMARY KEY,
    "TenantId"              uuid            NOT NULL,
    "ConfigurationId"       uuid            NOT NULL,  -- FK → ProductConfigurations
    "Quantity"              integer         NOT NULL,
    "DeliveryDate"          date            NOT NULL,
    "BomItems"              jsonb           NOT NULL,
    "TotalMaterialCost"     numeric(12,2)   NOT NULL,
    "EstimatedLabor"        numeric(12,2)   NOT NULL,
    "TotalCost"             numeric(12,2)   NOT NULL,
    "ScheduledStart"        date            NOT NULL,
    "PdfUrl"                text            NULL
);
-- RLS: tenant_isolation policy
```

**Seed:** 5 product templates
- `standard_door` — Standard beltéri ajtó
- `premium_door` — Prémium beltéri ajtó
- `fireproof_door` — Tűzálló ajtó EI30
- `acoustic_door` — Hangszigetelt ajtó Rw 37dB
- `security_door` — Biztonsági ajtó RC3

## Tesztek

### Build
```
dotnet build --no-incremental -v q
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:54.32
```

### Test Results
```
dotnet test --no-build -v q
Passed!  - Failed: 0, Passed: 450, Skipped: 0, Total: 450, Duration: 24 s
```

**Új tesztek (38 db):**

**Unit Tests (20):** `ProductConfiguratorServiceTests.cs`
- Validation: width/height/thickness bounds, invalid materials/fittings
- BOM calculation: core, veneer area, edge perimeter, hinge count logic
- Price calculation: materials + labor + setup + margin

**Integration Tests (18):** `ProductApiTests.cs`
- POST /api/products/configure: valid config, invalid productType, invalid dimensions/materials, tenant isolation
- POST /api/work-orders: valid creation, invalid configId, BOM quantity multiplication
- GET /api/work-orders/:id/sheet.pdf: PDF generation, invalid ID, tenant isolation, auth checks

## Security Review

- **Authorization:** `[Authorize(Policy = "ManufacturerOnly")]` minden endpoint-on
- **RLS:** ProductConfigurations és WorkOrders táblák FORCE RLS-sel védve (tenant isolation)
- **Input validation:** ProductConfiguratorService validálja dimension rules, allowed materials/fittings
- **Quantity limit:** Max 1000 quantity validation a CreateWorkOrderCommandValidator-ban
- **Cache-Control:** `private, no-store` header a PDF endpoint-on
- **Pure functions:** ProductConfiguratorService NINCS side effect (no DateTime.Now, no Random, no I/O)
- **Deterministic inventory mock:** Hash-based calculation helyett Random (pure function compliance)

## Service Deployment

- **Build:** Released to `/tmp/joinery-publish/` és átmásolva `/opt/spaceos/backend/spaceos-modules-joinery/publish/`
- **Service:** `sudo systemctl restart spaceos-joinery` — service running (healthy)
- **Endpoint check:** `curl http://localhost:5002/api/products/configure` → **405 Method Not Allowed** (POST expected, GET used) ✅
- **Health:** `curl http://localhost:5002/health` → `{"status":"healthy","service":"spaceos-joinery"}` ✅

## Kockázatok / Megjegyzések

### Production Migration
A migration még nem futott le a production DB-n (spaceos_joinery schema üres). Ez várható, mivel:
- Az integration tesztek in-memory DB-t használnak (EnsureCreatedAsync)
- A production deployment során kell lefuttatni a migration-t (MigrateAsync)
- Ez egy INFRA task (nem Joinery fejlesztői feladat)

**Action required:** INFRA terminál futtassa le a migration-t production-ben.

### Configuration-Driven Approach
A product templates JSONB szabályok alapján működnek:
- **DimensionRules:** minWidth, maxWidth, minHeight, maxHeight, allowedThickness[]
- **AllowedMaterials:** id, name, type, unitPrice
- **AllowedFittings:** id, name, category, unitPrice
- **PricingRules:** laborRate, marginPercent, setupCost

Ez lehetővé teszi új product type-ok hozzáadását code change nélkül (csak új seed DB-ben).

### Phase 2 Integrations
- **Inventory:** Mock supplier + stock calculation (deterministic hash-based)
  - Phase 2-ben Orchestrator koordinálja Inventory modul hívást
- **Cutting Plan:** Nem része a work order-nek (külön endpoint lesz)
- **Scheduling:** Egyszerű lead time alapú (deliveryDate - leadTimeDays)
  - Phase 2-ben production capacity scheduling

## Frontend Dependency

MSG-FE-087 (Frontend konfigurátor UI) már kiadva, mock API-val dolgozik.
Backend endpoint-ok készen állnak az integrációhoz (CORS konfigurálva az Orchestrator-on keresztül).

## Következő lépések (nem Joinery task)

1. **INFRA:** Production migration futtatása (`20260621000001_J004_ConfiguratorAndWorkOrders`)
2. **FE:** Frontend integration testing a valós backend endpoint-okkal
3. **Phase 2:** Inventory integration via Orchestrator (real stock queries)

---

**Status:** ✅ DONE — All code implemented, tested (450/450 zöld), deployed, ready for production migration.
