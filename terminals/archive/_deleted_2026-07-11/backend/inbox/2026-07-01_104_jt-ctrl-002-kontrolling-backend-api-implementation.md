---
id: MSG-BACKEND-104
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: sonnet
epic_id: EPIC-JT-CTRL
project_id: joinerytech-prod
created: 2026-07-01
content_hash: cd640352398865c4541a8a7262069d561b3663a35aaf3cdf4d49cd72dd693ebf
---

# JT-CTRL-002: Kontrolling Backend API Implementation

# Kontrolling Backend API Implementation

Implementáld a JoineryTech Kontrolling modul backend API-ját az ADR-055 alapján.

## ADR Forrás
`/opt/spaceos/docs/architecture/decisions/ADR-055-joinerytech-kontrolling-domain-model.md`

## Scope

### Domain Layer
- `ProjectCostCalculation` aggregate (calculated, not stored)
- `CostAdjustment` entity (manual corrections only)
- 6 cost categories: Material, Labor, Subcontracting, Logistics, Supplier, Overhead
- EAC (Estimate at Completion) calculation engine

### Application Layer (CQRS)
- **4 Commands:** CreateCostAdjustment, UpdateCostAdjustment, DeleteCostAdjustment, UpdateOverheadConfig
- **6 Queries:** GetProjectCost, GetProjectEAC, GetPortfolioCosts, GetCostBreakdown, GetCostAdjustments, GetOverheadConfig

### Infrastructure Layer
- Database schema: `controlling.cost_adjustments`, `controlling.overhead_config`
- RLS policies (tenant isolation)
- Caching: ProjectCostCalculation 5 min cache, Portfolio 10 min cache
- Repositories: ICostAdjustmentRepository, IOverheadConfigRepository

### API Layer (10 endpoints)
- Project Cost Queries (4 endpoints)
- Portfolio Queries (2 endpoints)
- Cost Adjustments (3 endpoints)
- Configuration (1 endpoint)
- Permissions: controlling.view, controlling.manage, controlling.admin

### Integration Contracts (5 integrations)
1. **Controlling → Production:** `IProductionCostService.GetMaterialCostAsync()` + `GetLaborCostAsync()`
2. **Controlling → HR:** `IHrCostService.GetTimeLogCostAsync()`
3. **Controlling → Finance:** `IFinanceCostService.GetInvoiceCostAsync()`
4. **Controlling → Warehouse:** `IWarehouseCostService.GetReceiptCostAsync()`
5. **Controlling → Logistics:** `ILogisticsCostService.GetShipmentCostAsync()`

## Calculation Logic

### EAC Formula
```csharp
FOR EACH category:
  projected[category] = MAX(planned[category], actual[category])

costEAC = SUM(projected[category]) + overhead
eacMargin = actualRevenue - costEAC
```

### Overhead Allocation
```csharp
overhead = directCosts * overheadRate
```

## Codegen Support

Használd az ADR-051 CQRS Handler Generator-t:
```bash
./scripts/codegen/generate-handler.sh GetProjectCost \
  --type query \
  --module Controlling \
  --repository ICostAdjustmentRepository \
  --aggregate ProjectCostCalculation \
  --with-response \
  --with-test
```

## Implementation Plan (5 weeks)
- **Week 1:** Domain Layer (Calculation engine, Cost categories)
- **Week 2:** Application Layer (CQRS handlers, integration contracts)
- **Week 3:** Infrastructure Layer (Database, Repositories, Caching)
- **Week 4:** API Layer (Controllers, OpenAPI, Authorization)
- **Week 5:** Integration testing (5 modules, E2E cost calculation)

## Build & Test
- 0 TypeScript/C# errors
- Unit tests (EAC calculation, overhead allocation)
- Integration tests (API endpoints, caching)
- E2E test (Project → Cost → EAC flow with mock integrations)

## Files to Create
- `src/Modules/Controlling/Domain/` (ProjectCostCalculation, CostAdjustment)
- `src/Modules/Controlling/Application/` (Commands, Queries, Handlers)
- `src/Modules/Controlling/Infrastructure/` (Database, Repositories, Caching)
- `src/Modules/Controlling/API/` (Controllers, DTOs)
- `tests/Controlling.Tests/` (Unit, Integration, E2E)

## Acceptance Criteria

- [ ] ProjectCostCalculation aggregate implemented (calculation-first, not stored)
- [ ] CostAdjustment entity implemented (manual corrections)
- [ ] EAC calculation engine implemented (MAX formula)
- [ ] Overhead allocation logic implemented (DirectCostPercentage)
- [ ] 4 command handlers + 6 query handlers implemented (CQRS)
- [ ] Database schema created (2 tables, RLS policies)
- [ ] 10 REST API endpoints implemented with OpenAPI
- [ ] 5 integration contract interfaces defined
- [ ] Caching implemented (5 min project, 10 min portfolio)
- [ ] Unit tests for EAC calculation and overhead allocation
- [ ] Integration tests for API endpoints and caching
- [ ] Build passes with 0 errors
- [ ] E2E test: Project cost → EAC → margin calculation (mock integrations)
