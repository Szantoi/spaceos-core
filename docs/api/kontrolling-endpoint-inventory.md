# JoineryTech Kontrolling API — Endpoint Inventory Matrix

**Version:** 1.0.0
**Date:** 2026-07-04
**Epic:** EPIC-JT-CTRL
**OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`

---

## Endpoint Overview

**Total Endpoints:** 10
**Query Endpoints (GET):** 6
**Command Endpoints (POST/PUT/DELETE):** 4

---

## Query Endpoints (Read-Heavy)

| # | Endpoint | Method | Purpose | Request DTO | Response DTO | Dependencies | Auth |
|---|----------|--------|---------|-------------|--------------|--------------|------|
| 1 | `/api/kontrolling/projects/{projectId}/cost-summary` | GET | Real-time cost summary (planned, actual, EAC) | - | `CostSummaryDto` | Production, HR, Finance, Warehouse, Logistics | `controlling.view` |
| 2 | `/api/kontrolling/projects/{projectId}/eac-calculation` | GET | EAC projection with category breakdown | - | `EACCalculationDto` | All 5 modules | `controlling.view` |
| 3 | `/api/kontrolling/projects/{projectId}/cost-breakdown` | GET | Cost breakdown by category and source module | - | `CostBreakdownDto` | All 5 modules | `controlling.view` |
| 4 | `/api/kontrolling/projects/{projectId}/variance-analysis` | GET | Planned vs. Actual variance report | - | `VarianceAnalysisDto` | All 5 modules | `controlling.view` |
| 5 | `/api/kontrolling/portfolio/summary` | GET | Portfolio-level aggregation (all projects) | - | `PortfolioSummaryDto` | All 5 modules | `controlling.view` |
| 6 | `/api/kontrolling/overhead-config` | GET | Get overhead allocation configuration | - | `OverheadConfigDto` | - | `controlling.view` |

---

## Command Endpoints (Write)

| # | Endpoint | Method | Purpose | Request DTO | Response DTO | Dependencies | Auth |
|---|----------|--------|---------|-------------|--------------|--------------|------|
| 7 | `/api/kontrolling/overhead-config` | POST | Create overhead configuration (initial setup) | `SetOverheadConfigCommand` | `OverheadConfigDto` | - | `controlling.admin` |
| 8 | `/api/kontrolling/overhead-config/{tenantId}` | PUT | Update overhead configuration | `SetOverheadConfigCommand` | `OverheadConfigDto` | - | `controlling.admin` |
| 9 | `/api/kontrolling/adjustments` | POST | Create manual cost adjustment | `CreateCostAdjustmentCommand` | `CostAdjustmentDto` | - | `controlling.manage` |
| 10 | `/api/kontrolling/adjustments/{adjustmentId}` | DELETE | Delete cost adjustment | - | - (204 No Content) | - | `controlling.manage` |

---

## Data Model Summary

### Core Response DTOs

| DTO | Fields | Description |
|-----|--------|-------------|
| **CostSummaryDto** | projectId, revenue, costs (planned/actual/eac), margins, timestamp | High-level cost and margin summary |
| **EACCalculationDto** | projectId, categories (6), totalCost, margin, timestamp | Detailed EAC with category breakdown |
| **CostBreakdownDto** | projectId, byCategory[], bySource[], timestamp | Drill-down by category and source module |
| **VarianceAnalysisDto** | projectId, variances[], topOverruns[], topUnderruns[], totalVariance, timestamp | Planned vs. Actual comparison |
| **PortfolioSummaryDto** | projectCount, totalRevenue, totalCost, totalMargin, topProjects[], flopProjects[], topVariances[], timestamp | Portfolio-level aggregation |
| **OverheadConfigDto** | tenantId, overheadRate, allocationMethod, exclusions[], updatedBy, updatedAt | Overhead configuration |
| **CostAdjustmentDto** | adjustmentId, scope, projectId?, category, plannedAdjustment?, actualAdjustment?, reason, createdBy, createdAt, tenantId | Manual cost adjustment |

### Command DTOs (Request Bodies)

| DTO | Required Fields | Optional Fields | Description |
|-----|-----------------|-----------------|-------------|
| **SetOverheadConfigCommand** | overheadRate, allocationMethod | exclusions[] | Set/update overhead config |
| **CreateCostAdjustmentCommand** | scope, category, reason | projectId, plannedAdjustment, actualAdjustment | Create manual adjustment |

### Value Objects

| Value Object | Properties | Description |
|--------------|------------|-------------|
| **Money** | amount, currency | Monetary value (amount + ISO 4217 currency) |
| **Revenue** | planned, actual | Planned and actual revenue |
| **CategoryCost** | planned, actual, projected, variance | Cost breakdown for a category |
| **Margin** | amount, percentage | Margin (Revenue - Cost) |
| **ProjectSummary** | projectId, projectName, eacMargin | Project summary for portfolio view |
| **VarianceItem** | projectId, projectName, category, variance, variancePercentage | Variance detail item |

### Enums

| Enum | Values | Description |
|------|--------|-------------|
| **CostCategory** | Material, Labor, Subcontracting, Logistics, Supplier, Overhead | 6 cost categories |
| **OverheadAllocationMethod** | DirectCostPercentage, LaborHours, Revenue | Overhead allocation methods |
| **AdjustmentScope** | Project, Portfolio | Adjustment scope (project-specific or global) |

---

## Integration Contracts (Read-Only)

**These DTOs define data structures from source modules. Kontrolling reads this data but NEVER writes to these modules.**

| Integration DTO | Source Module | Usage | Key Fields |
|-----------------|---------------|-------|------------|
| **MfgPrepCostData** | Production (MfgPrep service) | Planned Material + Labor costs | materialCost, laborCost, estimatedLaborHours |
| **TimeLogCostData** | HR (TimeLog service) | Actual Labor costs | projectId, employeeId, hoursWorked, hourlyRate, costTotal |
| **InboundInvoiceData** | Finance (Invoice service) | Actual Supplier costs | invoiceId, projectId, supplierId, amount, invoiceDate |
| **WarehouseReceiptData** | Warehouse (Receipt service) | Actual Material costs | receiptId, projectId, materialId, quantity, unitCost, totalCost |
| **ShipmentCostData** | Logistics (Shipment service) | Planned + Actual Logistics costs | shipmentId, projectId, estimatedCost, actualCost |

**Integration Pattern:**
- **Option A:** Direct DB queries (RLS-aware, read-only replica) — **Recommended** (faster, calculation-heavy module)
- **Option B:** REST API calls (slower, more decoupled)

---

## Database Schema (Kontrolling Owns)

**Only 2 tables owned by Kontrolling:**

| Table | Schema | Purpose | Key Columns |
|-------|--------|---------|-------------|
| **controlling.config** | tenant_id (PK), overhead_rate, allocation_method, updated_by, updated_at | Tenant-level overhead configuration | overhead_rate (decimal 0-1), allocation_method (enum) |
| **controlling.cost_adjustments** | adjustment_id (PK), scope, project_id, category, planned_adjustment_amount, actual_adjustment_amount, reason, created_by, created_at, tenant_id | Manual cost adjustments | scope (Project/Portfolio), category (enum), reason (text) |

**All other data:** Readonly joins to other modules (Production, HR, Finance, Warehouse, Logistics)

---

## Security & Permissions

| Permission | Endpoints | Description |
|------------|-----------|-------------|
| **controlling.view** | GET /projects/{id}/*, GET /portfolio/*, GET /overhead-config | View cost data |
| **controlling.manage** | POST /adjustments, DELETE /adjustments/{id} | Manage cost adjustments |
| **controlling.admin** | POST /overhead-config, PUT /overhead-config/{tenantId} | Configure overhead allocation |

**Authentication:** Bearer JWT (HttpOnly cookie in production)

---

## Error Responses

| HTTP Code | Error Code | Description | Retry-able | Example Use Case |
|-----------|-----------|-------------|-----------|------------------|
| **400** | VALIDATION_FAILED | Validation error | ✅ | Invalid overhead rate (>1) |
| **401** | UNAUTHORIZED | Token expired | ✅ | JWT expired, refresh token |
| **403** | FORBIDDEN | Permission denied | ❌ | User lacks `controlling.view` |
| **404** | NOT_FOUND | Resource not found | ❌ | Project not found |
| **409** | CONFLICT | Config already exists | ❌ | POST /overhead-config when config exists (use PUT) |
| **500** | INTERNAL_ERROR | Server error | ✅ | Database connection failure |

---

## Acceptance Criteria Mapping

| Criterion | Status | Evidence |
|-----------|--------|----------|
| OpenAPI 3.1 spec file created | ✅ | `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml` |
| 10 endpoints defined (6 GET + 4 POST/PUT/DELETE) | ✅ | See Endpoint Overview above |
| All DTOs match ADR-055 domain model | ✅ | CategoryCost, CostCategory enum, EAC formula documented |
| Integration contracts defined (5 modules) | ✅ | MfgPrepCostData, TimeLogCostData, InboundInvoiceData, WarehouseReceiptData, ShipmentCostData |
| Endpoint inventory matrix created | ✅ | This document |
| Validation passes: `npx @redocly/cli lint` | ⏳ | Pending validation step |
| Code-gen test passes: Orval (Frontend), NSwag (Backend) | ⏳ | Pending code-gen test step |
| Security: Bearer JWT auth scheme defined | ✅ | `securitySchemes.BearerAuth` in spec |

---

## Next Steps

### 1. Validation (Architect — Today)
```bash
npx @redocly/cli lint /opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml
```

### 2. Code-Gen Test (Architect — Today)

**Frontend (Orval):**
```bash
cd /opt/spaceos/datahaven-web/client
npx orval --config orval.kontrolling.config.ts
```

**Backend (NSwag):**
```bash
cd /opt/spaceos/spaceos-modules-kontrolling/Api
dotnet add package NSwag.MSBuild
# Generate C# client from OpenAPI spec
```

### 3. Spec Review (Backend + Conductor — Week 0)
- Backend reviews: .NET 8 feasibility, EF Core mapping
- Conductor approves: Contract locked for Week 1 implementation

### 4. Implementation (Backend — Week 1+)
- Domain layer (Week 1): ProjectCostCalculation, CostAdjustment entities
- Application layer (Week 2): CQRS handlers, calculation engine
- Infrastructure layer (Week 3): EF Core, integration queries
- API layer (Week 4): Controllers, validation
- Testing (Week 5): Unit, integration, performance

---

**Document Owner:** Architect Terminal
**Last Updated:** 2026-07-04
**Status:** DRAFT — Ready for validation
