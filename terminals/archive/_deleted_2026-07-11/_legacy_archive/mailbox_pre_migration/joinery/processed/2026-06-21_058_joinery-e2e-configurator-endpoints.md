---
id: MSG-JOINERY-058
from: root
to: joinery
type: task
priority: high
status: DONE
model: sonnet
ref: docs/planning/queue/2026-06-21_0055_consensus.md
created: 2026-06-21
---

# Joinery E2E Flow — Phase 1: Konfigurátor Backend Endpoints

## Összefoglaló

**Joinery End-to-End vertical slice Phase 1 backend feladat** — parametrikus konfigurátor API + munkalap generálás endpointok implementálása.

**Scope:** Teljes backend implementáció (6-8 óra) — API endpoints, database schema, 5 sablon seed, unit + integration tesztek.

---

## Új Joinery Endpoints

### 1. POST /joinery/api/products/configure

**Request:**
```json
{
  "productType": "standard_door",
  "dimensions": {
    "width": 900,
    "height": 2100,
    "thickness": 40
  },
  "materials": {
    "core": "chipboard_18mm",
    "veneer": "oak_natural",
    "edge": "pvc_oak"
  },
  "fittings": {
    "hinge": "hidden_3d",
    "handle": "modern_steel",
    "lock": "standard_cylinder"
  }
}
```

**Response:**
```json
{
  "configId": "uuid",
  "previewUrl": "/api/preview/{configId}.png",
  "estimatedPrice": 125000,
  "bomPreview": [
    {
      "materialCode": "CHIP-18-2800x2070",
      "description": "Forgácslap 18mm, tölgy furnér",
      "quantity": 2,
      "unit": "db",
      "unitPrice": 15000,
      "totalPrice": 30000
    }
  ]
}
```

**Backend logika:**
1. Validate `productType` (5 sablon: standard_door, double_door, sliding_door, window_door, custom)
2. Load template rules (`product_templates` tábla JSONB rules)
3. Apply dimension rules (min/max validáció, raszterezés)
4. Calculate BOM (materials + fittings alapján)
5. Estimate price (pricing_rules alapján)
6. Save config snapshot (`joinery_configurations` tábla)
7. Return configId + BOM preview

---

### 2. POST /joinery/api/work-orders

**Request:**
```json
{
  "configId": "uuid",
  "quantity": 5,
  "deliveryDate": "2026-07-15",
  "customerRef": "DOORSTAR-2026-Q3-001"
}
```

**Response:**
```json
{
  "workOrderId": "uuid",
  "pdfUrl": "/joinery/api/work-orders/{id}/sheet.pdf",
  "bomItems": [
    {
      "materialCode": "CHIP-18-2800x2070",
      "description": "Forgácslap 18mm, tölgy furnér",
      "totalQuantity": 10,
      "warehouseLocation": "RACK-A-12"
    }
  ],
  "totalCost": 625000,
  "estimatedCompletionDate": "2026-07-10"
}
```

**Backend logika:**
1. Load config snapshot (`joinery_configurations`)
2. Multiply BOM × quantity
3. Generate work order PDF (gyártási lap)
4. Save work order record
5. Return workOrderId + PDF URL + aggregated BOM

---

### 3. GET /joinery/api/work-orders/{id}/sheet.pdf

**Response:** PDF binary (Content-Type: application/pdf)

**Backend logika:**
1. Load work order
2. Render PDF template (QuestPDF vagy iTextSharp)
3. Include: customer ref, delivery date, BOM table, műveleti lépések, QR code (work order ID)

---

## Database Schema

### joinery_configurations tábla

```sql
CREATE TABLE joinery_configurations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  product_type VARCHAR(50) NOT NULL,
  params JSONB NOT NULL, -- dimensions, materials, fittings
  bom_snapshot JSONB NOT NULL, -- calculated BOM
  estimated_price DECIMAL(10, 2) NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_joinery_configs_type ON joinery_configurations(product_type);
CREATE INDEX idx_joinery_configs_created ON joinery_configurations(created_at DESC);
```

### product_templates tábla

```sql
CREATE TABLE product_templates (
  id VARCHAR(50) PRIMARY KEY, -- 'standard_door', 'double_door', etc.
  display_name VARCHAR(100) NOT NULL,
  dimension_rules JSONB NOT NULL,
  allowed_materials JSONB NOT NULL,
  pricing_rules JSONB NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Példa dimension_rules JSONB:
{
  "width": { "min": 600, "max": 1200, "step": 50 },
  "height": { "min": 1800, "max": 2400, "step": 100 },
  "thickness": { "values": [35, 40, 45] }
}

-- Példa pricing_rules JSONB:
{
  "base_price": 80000,
  "material_multipliers": {
    "oak_natural": 1.0,
    "walnut_dark": 1.3,
    "cherry_light": 1.15
  },
  "size_surcharge": {
    "width > 1000": 15000,
    "height > 2200": 20000
  }
}
```

---

## 5 Sablon Seed (Walking Skeleton)

```csharp
// Migrations/SeedProductTemplates.cs

public static class ProductTemplateSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductTemplate>().HasData(
            new ProductTemplate
            {
                Id = "standard_door",
                DisplayName = "Standard egyszárnyú ajtó",
                DimensionRules = JsonDocument.Parse(@"{
                    ""width"": {""min"": 700, ""max"": 1000, ""step"": 50},
                    ""height"": {""min"": 2000, ""max"": 2300, ""step"": 100},
                    ""thickness"": {""values"": [35, 40]}
                }"),
                AllowedMaterials = JsonDocument.Parse(@"{
                    ""core"": [""chipboard_18mm"", ""mdf_18mm""],
                    ""veneer"": [""oak_natural"", ""walnut_dark""],
                    ""edge"": [""pvc_oak"", ""abs_walnut""]
                }"),
                PricingRules = JsonDocument.Parse(@"{
                    ""base_price"": 80000,
                    ""material_multipliers"": {""oak_natural"": 1.0, ""walnut_dark"": 1.3}
                }")
            },
            new ProductTemplate
            {
                Id = "double_door",
                DisplayName = "Kétszárnyú ajtó",
                // ... hasonló struktúra
            },
            new ProductTemplate
            {
                Id = "sliding_door",
                DisplayName = "Tolóajtó",
                // ...
            },
            new ProductTemplate
            {
                Id = "window_door",
                DisplayName = "Üveges ajtó",
                // ...
            },
            new ProductTemplate
            {
                Id = "custom",
                DisplayName = "Egyedi méret",
                DimensionRules = JsonDocument.Parse(@"{
                    ""width"": {""min"": 500, ""max"": 1500, ""step"": 10},
                    ""height"": {""min"": 1500, ""max"": 2800, ""step"": 10},
                    ""thickness"": {""values"": [35, 40, 45, 50]}
                }"),
                // ... wider material options
            }
        );
    }
}
```

---

## API Controller Skeleton

```csharp
// Controllers/ConfiguratorController.cs

[ApiController]
[Route("joinery/api/products")]
public class ConfiguratorController : ControllerBase
{
    private readonly IConfiguratorService _configuratorService;

    [HttpPost("configure")]
    [ProducesResponseType(typeof(ConfigurationResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Configure([FromBody] ConfigurationRequest request)
    {
        var result = await _configuratorService.ConfigureProductAsync(request);
        return Ok(result);
    }
}

[ApiController]
[Route("joinery/api/work-orders")]
public class WorkOrderController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    [HttpPost]
    [ProducesResponseType(typeof(WorkOrderResult), 200)]
    public async Task<IActionResult> CreateWorkOrder([FromBody] WorkOrderRequest request)
    {
        var result = await _workOrderService.CreateWorkOrderAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}/sheet.pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetWorkSheet(Guid id)
    {
        var pdfBytes = await _workOrderService.GenerateWorkSheetPdfAsync(id);
        return File(pdfBytes, "application/pdf", $"work-order-{id}.pdf");
    }
}
```

---

## Definition of Done

### Database
- [ ] `joinery_configurations` tábla létezik (migration + indexes)
- [ ] `product_templates` tábla létezik
- [ ] 5 sablon seed elkészült (standard_door, double_door, sliding_door, window_door, custom)

### API Endpoints
- [ ] `POST /joinery/api/products/configure` működik
- [ ] `POST /joinery/api/work-orders` működik
- [ ] `GET /joinery/api/work-orders/{id}/sheet.pdf` működik (QuestPDF vagy iTextSharp)

### Backend Logic
- [ ] Template rules validáció (dimension min/max/step)
- [ ] BOM calculation (materials + fittings → items)
- [ ] Price estimation (base_price + multipliers + surcharges)
- [ ] Config snapshot mentése (joinery_configurations)

### Testing
- [ ] Unit tests: ConfiguratorService (5 sablon tesztelve)
- [ ] Unit tests: WorkOrderService (PDF generálás mock-olva)
- [ ] Integration tests: POST /configure → 200 OK + valid JSON
- [ ] Integration tests: POST /work-orders → PDF URL working
- [ ] dotnet test: ALL PASS

### Code Quality
- [ ] Swagger OpenAPI spec (automated codegen-ready)
- [ ] TypeScript types generálhatók (későbbi FE integration)
- [ ] ESLint/dotnet format: no warnings

---

## Dependencies

- **FE frontend:** MSG-FE-087 (párhuzamos, configure API-t hívja)
- **Orch routing:** MSG-ORCH-007 (párhuzamos, proxy-zza a /api/products/configure hívást)

**Note:** FE és Orch parallel futnak, de Joinery backend DONE előtt FE csak mock data-val tud dolgozni.

---

## Timeline

- **Task start:** 2026-06-21 (ma)
- **Estimated effort:** 6-8 óra (backend endpoints + DB + seed + tests)
- **Phase 1 target:** 2026-07-19

---

## Referenciák

- **Planning consensus:** `docs/planning/queue/2026-06-21_0055_consensus.md`
- **Orchestrator routing:** `docs/mailbox/orch/inbox/2026-06-21_007_joinery-e2e-routing-verify.md`
- **FE components:** `docs/mailbox/fe/inbox/2026-06-21_087_joinery-e2e-configurator-components.md`
- **Joinery modul:** `backend/spaceos-modules-joinery/` (port 5002)

---

Kérdések esetén eszkalálj Root-nak vagy Conductor-nak.
