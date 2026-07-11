---
id: MSG-ARCHITECT-060
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-CTRL
ref: MSG-ARCHITECT-040, ADR-055
created: 2026-07-04
estimated_nwt: 120
content_hash: 97fff1357eac06bcb7e01fb2860f82c312df0a804fdc88c33995d02ee9a0c3bb
---

# JoineryTech Kontrolling — Week 0 Contract-First OpenAPI Spec

**Epic:** EPIC-JT-CTRL (Kontrolling Modul)
**Priority:** HIGH (JoineryTech parallel development)
**Estimated:** 120 NWT (~4 hours)
**Dependency Status:** ✅ UNBLOCKED (EPIC-KERNEL-STABLE DONE)

---

## Context

EPIC-JT-CTRL (Kontrolling modul) **párhuzamosan futhat** EPIC-JT-CRM-mel. Domain model kész (ADR-055), most a Contract-First Week 0 OpenAPI spec következik.

**Parallel Development Strategy:**
- 🔄 **CRM**: Backend API implementáció (MSG-BACKEND-103 in progress)
- 🔄 **Dashboard**: Frontend widgets (MSG-FRONTEND-105 in progress)
- 🆕 **Kontrolling**: Week 0 OpenAPI spec (THIS TASK) → Backend Week 1 → Frontend Week 1.5 (MSW)

**Goal:** Lock OpenAPI contract BEFORE implementation starts → prevent integration rework

---

## Task: Kontrolling Module OpenAPI 3.1 Specification

**Reference:** ADR-055 (JoineryTech Kontrolling Domain Model)

**Contract-First Workflow (Explorer/Librarian validated):**
- **Day 1-2:** Endpoint inventory + data model definition
- **Day 3:** Draft OpenAPI spec (commands + queries)
- **Day 4:** Spec lock + validation + code-gen test

**ROI:** $4k Week 0 → $11-16k savings (2 weeks integration rework prevented)

---

## Deliverables

### 1. OpenAPI 3.1 Spec File

**Location:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`

**Sections:**

#### A. Cost Calculation Queries (Read-Heavy)

Based on ADR-055 section "CQRS Queries":

```yaml
# 6 Query endpoints:
GET /api/kontrolling/projects/{projectId}/cost-summary
GET /api/kontrolling/projects/{projectId}/eac-calculation
GET /api/kontrolling/projects/{projectId}/cost-breakdown
GET /api/kontrolling/projects/{projectId}/variance-analysis
GET /api/kontrolling/portfolio/summary
GET /api/kontrolling/overhead-config
```

**Query Response Models:**
- `CostSummaryDto` (planned, actual, eac, margin)
- `EACCalculationDto` (6 cost categories + overhead)
- `CostBreakdownDto` (by category, by source module)
- `VarianceAnalysisDto` (planned vs actual, percentage)
- `PortfolioSummaryDto` (aggregated, multi-project)
- `OverheadConfigDto` (rate, method, exclusions)

#### B. Configuration Commands (Write)

Based on ADR-055 section "CQRS Commands":

```yaml
# 4 Command endpoints:
POST /api/kontrolling/overhead-config
PUT /api/kontrolling/overhead-config/{tenantId}
POST /api/kontrolling/adjustments
DELETE /api/kontrolling/adjustments/{adjustmentId}
```

**Command Request Models:**
- `SetOverheadConfigCommand` (rate, method, exclusions)
- `CreateCostAdjustmentCommand` (projectId, category, amount, reason, effectiveDate)

#### C. Data Models (Schemas)

**Domain Entities:**
- `ProjectCostCalculation` (calculated, not stored)
- `CostAdjustment` (stored entity)
- `OverheadConfig` (tenant-level configuration)

**Cost Categories Enum:**
```typescript
enum CostCategory {
  Material,      // Warehouse receipts
  Labor,         // HR time logs
  Subcontracting,// B2B handshakes
  Logistics,     // Shipment costs
  Supplier,      // Inbound invoices
  Overhead       // Calculated percentage
}
```

**Integration DTOs (readonly, from other modules):**
- `MfgPrepCostData` (from Production module)
- `TimeLogCostData` (from HR module)
- `WarehouseReceiptData` (from Warehouse module)
- `ShipmentCostData` (from Logistics module)
- `InboundInvoiceData` (from Finance module)

### 2. Endpoint Inventory Matrix

Create a spreadsheet or markdown table:

| Endpoint | Method | Purpose | Request DTO | Response DTO | Dependencies |
|----------|--------|---------|-------------|--------------|--------------|
| `/projects/{id}/cost-summary` | GET | Real-time cost summary | - | CostSummaryDto | Production, HR, Warehouse |
| `/projects/{id}/eac-calculation` | GET | EAC projection | - | EACCalculationDto | All 5 modules |
| `/overhead-config` | POST | Set overhead rate | SetOverheadConfigCommand | OverheadConfigDto | - |
| ... | ... | ... | ... | ... | ... |

**Total:** 10 endpoints (6 queries + 4 commands)

### 3. Integration Contract Definition

**ADR-055 Section: Integration Contracts**

For EACH source module, define:

```yaml
# Example: HR Module Integration
components:
  schemas:
    TimeLogCostData:
      type: object
      properties:
        projectId:
          type: string
          format: uuid
        employeeId:
          type: string
        hoursWorked:
          type: number
        hourlyRate:
          type: number
        costTotal:
          type: number
        periodStart:
          type: string
          format: date
        periodEnd:
          type: string
          format: date
      required: [projectId, hoursWorked, hourlyRate, costTotal]
```

**Integration Modules:**
1. Production (MfgPrep planned costs)
2. HR (TimeLog actual labor)
3. Finance (InboundInvoice supplier costs)
4. Warehouse (Material receipt costs)
5. Logistics (Shipment costs)

### 4. Validation & Code-Gen Test

**Validation:**
```bash
# OpenAPI validation
npx @redocly/cli lint docs/api/joinerytech-kontrolling-v1.yaml

# Schema validation (no $ref errors, all required fields)
```

**Code-Gen Test (Frontend):**
```bash
cd datahaven-web/client
npx orval --config orval.kontrolling.config.ts

# Expected output:
# - src/api/kontrolling/kontrolling.ts (TanStack Query hooks)
# - src/api/kontrolling/model/*.ts (TypeScript types)
```

**Code-Gen Test (Backend):**
```bash
cd spaceos-modules-kontrolling/Api
dotnet add package NSwag.MSBuild

# Expected output:
# - Generated C# client (verify DTO types match domain model)
```

---

## Technical Constraints

### 1. ADR-055 Compliance

**MUST align with ADR-055 architecture:**
- ✅ Calculation-first (no stored calculations, only CostAdjustment stored)
- ✅ One source of truth (integration contracts readonly)
- ✅ EAC formula: `MAX(planned, actual)` per category
- ✅ Overhead allocation: DirectCostPercentage default
- ✅ CQRS pattern (6 queries, 4 commands)

### 2. OpenAPI 3.1 Standards

```yaml
openapi: 3.1.0
info:
  title: JoineryTech Kontrolling API
  version: 1.0.0
  description: |
    Cost calculation, EAC projection, overhead allocation.
    Read-heavy module aggregating Production, HR, Finance, Warehouse, Logistics.
servers:
  - url: https://api.joinerytech.local
    description: Local development
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
security:
  - BearerAuth: []
```

### 3. Integration Pattern

**All external module data is READ-ONLY:**
- Kontrolling NEVER writes to Production/HR/Finance/Warehouse/Logistics
- Integration via:
  - **Option A:** Direct DB queries (RLS-aware, read-only replica)
  - **Option B:** REST API calls (slower, more decoupled)
  - **Recommended:** Direct DB queries (faster, calculation-heavy module)

**Database Schema (Kontrolling owns):**
```sql
-- Only 2 tables owned by Kontrolling:
kontrolling.overhead_configs (tenant-level config)
kontrolling.cost_adjustments (manual corrections)

-- All other data: readonly joins to other modules
```

---

## Acceptance Criteria

**DONE when:**
- [ ] OpenAPI 3.1 spec file: `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- [ ] 10 endpoints defined (6 GET queries, 4 POST/PUT/DELETE commands)
- [ ] All DTOs/schemas match ADR-055 domain model
- [ ] Integration contracts defined (5 modules: Production, HR, Finance, Warehouse, Logistics)
- [ ] Endpoint inventory matrix created (Markdown table or spreadsheet)
- [ ] Validation passes: `npx @redocly/cli lint`
- [ ] Code-gen test passes: Orval (Frontend), NSwag (Backend)
- [ ] No $ref errors, all required fields present
- [ ] Security: Bearer JWT auth scheme defined

**Quality Gates:**
- Spec lock commit: Tag `kontrolling-spec-v1.0.0`
- Review by Conductor (contract clarity, integration feasibility)
- Approved before Backend Week 1 starts

---

## Integration with Existing Work

**ADR-055 Implementation Plan (Week 1-5):**
- ✅ **Week 0** (THIS TASK): OpenAPI spec lock
- ⏳ **Week 1**: Domain layer (ProjectCostCalculation, CostAdjustment)
- ⏳ **Week 2**: Application layer (CQRS handlers, calculation engine)
- ⏳ **Week 3**: Infrastructure layer (EF Core, integration queries)
- ⏳ **Week 4**: API layer (controllers, validation)
- ⏳ **Week 5**: Testing (unit, integration, performance)

**Parallel Development Unlock:**
- After Week 0 spec lock → Backend starts Week 1 → Frontend starts Week 1.5 (MSW mock API)
- No integration rework (contract locked upfront)

---

## References

- **Domain Model:** `/opt/spaceos/docs/architecture/decisions/ADR-055-joinerytech-kontrolling-domain-model.md`
- **Contract-First Pattern:** `/opt/spaceos/docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md`
- **CRM OpenAPI Example:** `/opt/spaceos/terminals/architect/outbox/2026-07-02_044_joinerytech-openapi-week-0-done.md`
- **Integration Architecture:** `ADR-058-joinerytech-integration-architecture.md`
- **Code-Gen Tools:** Orval (Frontend), NSwag (Backend)

---

## Priority Rationale

**Why HIGH priority:**
- ✅ EPIC-JT-CTRL **unblocked** (EPIC-KERNEL-STABLE done)
- ✅ **Parallel development** with EPIC-JT-CRM (Backend/Frontend busy on CRM)
- ✅ Contract-First **prevents 2 weeks integration rework** ($11-16k savings)
- ✅ JoineryTech **top focus** (user explicit request)
- ✅ Week 0 spec **enables early Frontend mockup** (MSW parallel track)

**Timeline:**
- Week 0 (4 hours) → Backend Week 1 dispatch (when CRM API completes) → Frontend Week 1.5 (MSW parallel)

---

**Next After Completion:**
When Architect completes OpenAPI spec → Conductor reviews → Backend Kontrolling Week 1 task dispatch (if CRM API done) OR queue for later.

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
