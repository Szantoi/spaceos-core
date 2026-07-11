# ADR-055: JoineryTech Kontrolling Domain Model Design

**Status:** DRAFT
**Date:** 2026-07-01
**Epic:** EPIC-JT-CTRL
**Author:** Architect Terminal
**Reviewers:** Backend, Conductor

---

## Context

The JoineryTech ERP system requires a **Controlling (Cost Management)** module to provide project profitability analysis, planned vs. actual cost tracking, and portfolio-level financial insights. The Controlling module is a **calculated layer** over existing data — it does not create new state machines, but aggregates data from Production, HR, Finance, and Logistics modules.

**Key Requirements:**
1. **Planned vs. Actual Cost Tracking** — Compare budgeted costs to actual spend across cost categories
2. **EAC (Estimate at Completion)** — Realistic projection of final project costs
3. **Margin Analysis** — Revenue minus costs, both planned and actual
4. **Overhead Allocation** — Fixed cost distribution across projects
5. **Portfolio Aggregation** — Cross-project financial dashboard
6. **Manual Adjustments** — Correction mechanism for calculation gaps

**Design Philosophy:**
- **One source of truth** — Controlling does NOT duplicate data, it CALCULATES from existing modules
- **Immutable calculations** — Results are derived on-demand, not stored
- **Manual corrections only** — The only writable data is user-entered adjustments

---

## Decision

### 1. Aggregate Boundaries

The Controlling domain has **ONE primary aggregate** and **ONE supporting entity**:

#### 1.1 ProjectCostCalculation (Aggregate Root — Calculated)

```
ProjectCostCalculation (NOT STORED — Calculated on-demand)
├── ProjectId (Guid)
├── Revenue (Money)
│   ├── Planned (from Contract value)
│   └── Actual (from Invoices)
├── CostCategories (Dictionary<CostCategory, CategoryCost>)
│   ├── Material
│   ├── Labor
│   ├── Subcontracting
│   ├── Logistics
│   ├── Supplier
│   └── Overhead
├── PlannedMargin (Money)
├── ActualMargin (Money)
├── EACMargin (Money) ← Estimate at Completion margin
└── TenantId
```

**Invariants:**
- Revenue.Planned ≥ 0
- All cost values ≥ 0
- Margin = Revenue - Total Cost
- EAC calculations must use max(planned, actual) per category

**Cost Category Structure:**

```csharp
public sealed class CategoryCost : ValueObject
{
    public Money Planned { get; }
    public Money Actual { get; }
    public Money Projected { get; }  // EAC: max(Planned, Actual)
    public Money Variance { get; }   // Actual - Planned

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Planned;
        yield return Actual;
        yield return Projected;
        yield return Variance;
    }
}

public enum CostCategory
{
    Material = 1,
    Labor = 2,
    Subcontracting = 3,
    Logistics = 4,
    Supplier = 5,
    Overhead = 6
}
```

#### 1.2 CostAdjustment (Entity — STORED)

```
CostAdjustment (Manual Correction)
├── AdjustmentId (Guid)
├── Scope (AdjustmentScope enum)
│   ├── Project → Specific ProjectId
│   └── Portfolio → All projects
├── Category (CostCategory enum)
├── PlannedAdjustment (Money?)
├── ActualAdjustment (Money?)
├── Reason (string)
├── CreatedBy (UserId)
├── CreatedAt (DateTime)
└── TenantId
```

**Purpose:** Manual corrections for:
- Missing data (e.g., subcontractor invoice not yet entered)
- Calculation errors (e.g., incorrect material allocation)
- Forecast adjustments (e.g., expected cost overrun)

**Invariants:**
- At least one adjustment (Planned or Actual) must be non-null
- Reason is mandatory
- Scope.Project requires valid ProjectId

---

### 2. Calculation Engine

The Controlling module is a **calculation-heavy, read-oriented** system. All cost values are computed on-demand from source data.

#### 2.1 Cost Data Sources

| Category | Planned Source | Actual Source |
|----------|----------------|---------------|
| **Material** | `MfgPrep.derive(project)` material costs | `WarehouseReceipts` filtered by project |
| **Labor** | `MfgPrep.derive(project)` labor hours × rate | `HR.TimeLogs` filtered by project × hourly rate |
| **Subcontracting** | `B2BHandshakes` (outbound, planned value) | `B2BHandshakes` (outbound, invoiced value) |
| **Logistics** | Linked `Shipments` (estimated cost) | Linked `Shipments` (actual cost from invoices) |
| **Supplier** | N/A | Inbound invoices for materials |
| **Overhead** | Fixed percentage of direct costs | Same percentage applied to actual direct costs |

#### 2.2 Calculation Flow

```csharp
public sealed class ProjectCostCalculation
{
    public static ProjectCostCalculation Calculate(
        Guid projectId,
        Guid tenantId,
        IProjectRepository projectRepo,
        IMfgPrepService mfgPrepService,
        IWarehouseRepository warehouseRepo,
        IHRRepository hrRepo,
        IB2BRepository b2bRepo,
        ILogisticsRepository logisticsRepo,
        IFinanceRepository financeRepo,
        IControllingConfig config,
        IEnumerable<CostAdjustment> adjustments)
    {
        var project = await projectRepo.GetByIdAsync(projectId, tenantId);
        var mfgPrep = await mfgPrepService.DeriveForProjectAsync(projectId);

        // 1. PLANNED COSTS
        var plannedMaterial = mfgPrep.MaterialCost;
        var plannedLabor = mfgPrep.LaborCost;
        var plannedSubcontracting = CalculatePlannedSubcontracting(projectId, b2bRepo);
        var plannedLogistics = CalculatePlannedLogistics(projectId, logisticsRepo);
        var plannedDirect = plannedMaterial + plannedLabor + plannedSubcontracting + plannedLogistics;
        var plannedOverhead = plannedDirect * config.OverheadRate;

        // 2. ACTUAL COSTS
        var actualMaterial = CalculateActualMaterial(projectId, warehouseRepo);
        var actualLabor = CalculateActualLabor(projectId, hrRepo);
        var actualSubcontracting = CalculateActualSubcontracting(projectId, b2bRepo);
        var actualLogistics = CalculateActualLogistics(projectId, logisticsRepo);
        var actualSupplier = CalculateActualSupplier(projectId, financeRepo);
        var actualDirect = actualMaterial + actualLabor + actualSubcontracting + actualLogistics + actualSupplier;
        var actualOverhead = actualDirect * config.OverheadRate;

        // 3. APPLY ADJUSTMENTS
        var adjustedPlanned = ApplyAdjustments(plannedCosts, adjustments.Where(a => a.PlannedAdjustment != null));
        var adjustedActual = ApplyAdjustments(actualCosts, adjustments.Where(a => a.ActualAdjustment != null));

        // 4. EAC (PROJECTED)
        var projectedMaterial = Math.Max(adjustedPlanned.Material, adjustedActual.Material);
        var projectedLabor = Math.Max(adjustedPlanned.Labor, adjustedActual.Labor);
        // ... (same for all categories)

        // 5. REVENUE
        var plannedRevenue = project.ContractValue;
        var actualRevenue = CalculateActualRevenue(projectId, financeRepo);

        // 6. MARGINS
        var plannedMargin = plannedRevenue - plannedTotal;
        var actualMargin = actualRevenue - actualTotal;
        var eacMargin = actualRevenue - eacTotal;

        return new ProjectCostCalculation
        {
            ProjectId = projectId,
            Revenue = new Revenue { Planned = plannedRevenue, Actual = actualRevenue },
            CostCategories = /* ... */,
            PlannedMargin = plannedMargin,
            ActualMargin = actualMargin,
            EACMargin = eacMargin,
            TenantId = tenantId
        };
    }
}
```

#### 2.3 EAC (Estimate at Completion) Logic

**Formula:**
```
For each cost category:
  projected[category] = MAX(planned[category], actual[category])

costEAC = SUM(projected[category]) + overhead

eacMargin = actualRevenue - costEAC
eacMarginPct = eacMargin / actualRevenue * 100
```

**Rationale:**
- **Unrealized costs carry their planned value** (lower bound guarantee)
- **Realized overspend is incorporated** (realistic projection)
- **Result: Stable, realistic margin forecast** throughout project lifecycle

**Example:**

| Category | Planned | Actual | Projected (EAC) |
|----------|---------|--------|-----------------|
| Material | 1,000,000 HUF | 1,200,000 HUF | **1,200,000 HUF** (overspend) |
| Labor | 500,000 HUF | 300,000 HUF | **500,000 HUF** (not yet complete) |
| Logistics | 200,000 HUF | 0 HUF | **200,000 HUF** (not yet incurred) |
| **Total** | **1,700,000 HUF** | **1,500,000 HUF** | **1,900,000 HUF** |

Revenue: 2,500,000 HUF
EAC Margin: 2,500,000 - 1,900,000 = **600,000 HUF (24%)**

---

### 3. Overhead Allocation

**Overhead** = Fixed costs (rent, utilities, admin salaries) allocated to projects.

#### 3.1 Configuration

```csharp
public sealed class ControllingConfig : ValueObject
{
    public decimal OverheadRate { get; }  // e.g., 0.15 = 15%
    public OverheadAllocationMethod Method { get; }

    public ControllingConfig(decimal overheadRate, OverheadAllocationMethod method)
    {
        if (overheadRate < 0 || overheadRate > 1)
            throw new ArgumentException("Overhead rate must be between 0 and 1");

        OverheadRate = overheadRate;
        Method = method;
    }
}

public enum OverheadAllocationMethod
{
    DirectCostPercentage = 1,  // Default: % of direct costs
    LaborHours = 2,            // Based on labor hours
    Revenue = 3                // Based on project revenue
}
```

**Default Method:** `DirectCostPercentage`

**Calculation:**
```csharp
var directCosts = material + labor + subcontracting + logistics + supplier;
var overhead = directCosts * config.OverheadRate;
```

#### 3.2 Tenant-Level Configuration

Each tenant can configure:
- Overhead rate (default: 15%)
- Allocation method
- Exclusions (e.g., exclude Logistics from overhead base)

Stored in: `controlling_config` table (one row per tenant)

---

### 4. Portfolio Aggregation

Portfolio-level view aggregates all active projects for a tenant.

```csharp
public sealed class PortfolioCostCalculation
{
    public int ProjectCount { get; }
    public Money TotalPlannedRevenue { get; }
    public Money TotalActualRevenue { get; }
    public Money TotalPlannedCost { get; }
    public Money TotalActualCost { get; }
    public Money TotalEACCost { get; }
    public Money PlannedMargin { get; }
    public Money ActualMargin { get; }
    public Money EACMargin { get; }
    public decimal PlannedMarginPct { get; }
    public decimal ActualMarginPct { get; }
    public decimal EACMarginPct { get; }

    public IEnumerable<ProjectSummary> TopProjects { get; }   // Top 5 by margin
    public IEnumerable<ProjectSummary> FlopProjects { get; }  // Bottom 5 by margin
    public IEnumerable<VarianceItem> TopVariances { get; }    // Largest overruns
}
```

**Aggregation Logic:**
```csharp
public static PortfolioCostCalculation CalculatePortfolio(
    Guid tenantId,
    IEnumerable<ProjectCostCalculation> projects)
{
    return new PortfolioCostCalculation
    {
        ProjectCount = projects.Count(),
        TotalPlannedRevenue = projects.Sum(p => p.Revenue.Planned),
        TotalActualRevenue = projects.Sum(p => p.Revenue.Actual),
        TotalPlannedCost = projects.Sum(p => p.TotalPlannedCost),
        TotalActualCost = projects.Sum(p => p.TotalActualCost),
        TotalEACCost = projects.Sum(p => p.TotalEACCost),
        PlannedMargin = /* Total Revenue - Total Planned Cost */,
        ActualMargin = /* Total Revenue - Total Actual Cost */,
        EACMargin = /* Total Revenue - Total EAC Cost */,
        TopProjects = projects.OrderByDescending(p => p.EACMargin).Take(5),
        FlopProjects = projects.OrderBy(p => p.EACMargin).Take(5),
        TopVariances = /* Top 10 largest cost overruns across all categories */
    };
}
```

---

### 5. Manual Adjustments

Manual adjustments allow users to correct calculation gaps or forecast future costs.

#### 5.1 Adjustment Model

```csharp
public sealed class CostAdjustment : Entity
{
    public Guid Id { get; }
    public AdjustmentScope Scope { get; }
    public Guid? ProjectId { get; }  // Required if Scope = Project
    public CostCategory Category { get; }
    public Money? PlannedAdjustment { get; }
    public Money? ActualAdjustment { get; }
    public string Reason { get; }
    public Guid CreatedBy { get; }
    public DateTime CreatedAt { get; }
    public Guid TenantId { get; }

    // Factory method
    public static CostAdjustment Create(
        AdjustmentScope scope,
        CostCategory category,
        Money? plannedAdjustment,
        Money? actualAdjustment,
        string reason,
        Guid createdBy,
        Guid tenantId,
        Guid? projectId = null)
    {
        if (plannedAdjustment == null && actualAdjustment == null)
            throw new ArgumentException("At least one adjustment must be provided");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required");

        if (scope == AdjustmentScope.Project && projectId == null)
            throw new ArgumentException("ProjectId is required for project-scoped adjustments");

        return new CostAdjustment
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            ProjectId = projectId,
            Category = category,
            PlannedAdjustment = plannedAdjustment,
            ActualAdjustment = actualAdjustment,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }
}

public enum AdjustmentScope
{
    Project = 1,    // Applies to a specific project
    Portfolio = 2   // Applies to all projects (e.g., global overhead correction)
}
```

#### 5.2 Application to Calculations

```csharp
private static Dictionary<CostCategory, CategoryCost> ApplyAdjustments(
    Dictionary<CostCategory, CategoryCost> baseCosts,
    IEnumerable<CostAdjustment> adjustments)
{
    var result = new Dictionary<CostCategory, CategoryCost>(baseCosts);

    foreach (var adj in adjustments)
    {
        if (!result.ContainsKey(adj.Category))
            result[adj.Category] = new CategoryCost(Money.Zero, Money.Zero);

        var existing = result[adj.Category];

        result[adj.Category] = new CategoryCost(
            planned: existing.Planned + (adj.PlannedAdjustment ?? Money.Zero),
            actual: existing.Actual + (adj.ActualAdjustment ?? Money.Zero)
        );
    }

    return result;
}
```

#### 5.3 Use Cases

**Example 1: Missing Subcontractor Invoice**
- Scope: Project
- Category: Subcontracting
- Actual Adjustment: +500,000 HUF
- Reason: "Subcontractor invoice pending entry, expected amount"

**Example 2: Global Overhead Rate Increase**
- Scope: Portfolio
- Category: Overhead
- Planned Adjustment: +5%
- Reason: "Rent increase effective Q3 2026"

---

### 6. Domain Events Catalog

Controlling events are minimal since most operations are calculations. Only manual adjustments and config changes emit events.

| Event | Payload | Raised When |
|-------|---------|-------------|
| `CostAdjustmentCreated` | `{ AdjustmentId, Scope, ProjectId, Category, PlannedAdjustment, ActualAdjustment, Reason, CreatedBy, TenantId }` | Manual adjustment created |
| `CostAdjustmentUpdated` | `{ AdjustmentId, UpdatedFields, UpdatedBy }` | Manual adjustment modified |
| `CostAdjustmentDeleted` | `{ AdjustmentId, DeletedBy }` | Manual adjustment removed |
| `ControllingConfigUpdated` | `{ TenantId, OverheadRate, AllocationMethod, UpdatedBy }` | Tenant config changed |

**No events for calculations** — Calculations are read-only and ephemeral.

---

### 7. Integration Contracts

#### 7.1 Controlling ← Production Integration

**Service Interface:**

```csharp
// SpaceOS.Modules.Production.Contracts
public interface IMfgPrepService
{
    Task<MfgPrepData> DeriveForProjectAsync(Guid projectId, CancellationToken ct = default);
}

public sealed class MfgPrepData
{
    public Money MaterialCost { get; init; }
    public Money LaborCost { get; init; }
    public int EstimatedLaborHours { get; init; }
    public IEnumerable<MaterialItem> Materials { get; init; }
}
```

**Usage in Controlling:**
- Planned Material cost: `mfgPrep.MaterialCost`
- Planned Labor cost: `mfgPrep.LaborCost`

#### 7.2 Controlling ← HR Integration

**Service Interface:**

```csharp
// SpaceOS.Modules.HR.Contracts
public interface ITimeLogService
{
    Task<IEnumerable<TimeLogEntry>> GetTimeLogsForProjectAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default);
}

public sealed class TimeLogEntry
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal Hours { get; init; }
    public Money HourlyRate { get; init; }
    public DateTime LogDate { get; init; }
}
```

**Usage in Controlling:**
- Actual Labor cost: `SUM(timeLogs.Hours * timeLogs.HourlyRate)`

#### 7.3 Controlling ← Finance Integration

**Service Interface:**

```csharp
// SpaceOS.Modules.Finance.Contracts
public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetInvoicesForProjectAsync(
        Guid projectId,
        InvoiceDirection direction,
        Guid tenantId,
        CancellationToken ct = default);
}

public enum InvoiceDirection
{
    Outbound = 1,  // Revenue (to customer)
    Inbound = 2    // Costs (from suppliers)
}
```

**Usage in Controlling:**
- Actual Revenue: Outbound invoices
- Actual Supplier costs: Inbound invoices

#### 7.4 Controlling ← Warehouse Integration

**Service Interface:**

```csharp
// SpaceOS.Modules.Warehouse.Contracts
public interface IWarehouseReceiptService
{
    Task<IEnumerable<WarehouseReceipt>> GetReceiptsForProjectAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default);
}

public sealed class WarehouseReceipt
{
    public Guid Id { get; init; }
    public Guid MaterialId { get; init; }
    public decimal Quantity { get; init; }
    public Money UnitCost { get; init; }
    public Money TotalCost { get; init; }
    public DateTime ReceivedAt { get; init; }
}
```

**Usage in Controlling:**
- Actual Material cost: `SUM(receipts.TotalCost)`

#### 7.5 Controlling ← Logistics Integration

**Service Interface:**

```csharp
// SpaceOS.Modules.Logistics.Contracts
public interface IShipmentCostService
{
    Task<IEnumerable<ShipmentCost>> GetShipmentCostsForProjectAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default);
}

public sealed class ShipmentCost
{
    public Guid ShipmentId { get; init; }
    public Money EstimatedCost { get; init; }
    public Money ActualCost { get; init; }
    public DateTime ShipmentDate { get; init; }
}
```

**Usage in Controlling:**
- Planned Logistics cost: `SUM(shipments.EstimatedCost)`
- Actual Logistics cost: `SUM(shipments.ActualCost)`

---

### 8. Database Schema

#### 8.1 Controlling Configuration Table

```sql
CREATE TABLE controlling.config (
    tenant_id UUID PRIMARY KEY,
    overhead_rate DECIMAL(5, 4) NOT NULL DEFAULT 0.15,  -- 15%
    allocation_method VARCHAR(50) NOT NULL DEFAULT 'DirectCostPercentage',

    updated_by UUID NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_config_tenant FOREIGN KEY (tenant_id) REFERENCES kernel.tenants(id),
    CONSTRAINT chk_overhead_rate CHECK (overhead_rate >= 0 AND overhead_rate <= 1),
    CONSTRAINT chk_allocation_method CHECK (allocation_method IN ('DirectCostPercentage', 'LaborHours', 'Revenue'))
);

-- RLS Policy
ALTER TABLE controlling.config ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON controlling.config
    USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

#### 8.2 Cost Adjustments Table

```sql
CREATE TABLE controlling.cost_adjustments (
    adjustment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scope VARCHAR(20) NOT NULL,  -- 'Project' or 'Portfolio'
    project_id UUID,  -- Nullable, required if scope = Project
    category VARCHAR(50) NOT NULL,  -- 'Material', 'Labor', etc.

    planned_adjustment_amount DECIMAL(18, 2),
    planned_adjustment_currency VARCHAR(3),
    actual_adjustment_amount DECIMAL(18, 2),
    actual_adjustment_currency VARCHAR(3),

    reason TEXT NOT NULL,

    created_by UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tenant_id UUID NOT NULL,

    CONSTRAINT fk_adjustments_tenant FOREIGN KEY (tenant_id) REFERENCES kernel.tenants(id),
    CONSTRAINT fk_adjustments_project FOREIGN KEY (project_id) REFERENCES projects.projects(project_id),
    CONSTRAINT chk_adjustment_scope CHECK (scope IN ('Project', 'Portfolio')),
    CONSTRAINT chk_adjustment_category CHECK (category IN ('Material', 'Labor', 'Subcontracting', 'Logistics', 'Supplier', 'Overhead')),
    CONSTRAINT chk_adjustment_required CHECK (
        (planned_adjustment_amount IS NOT NULL AND planned_adjustment_currency IS NOT NULL) OR
        (actual_adjustment_amount IS NOT NULL AND actual_adjustment_currency IS NOT NULL)
    ),
    CONSTRAINT chk_project_scope CHECK (
        (scope = 'Project' AND project_id IS NOT NULL) OR
        (scope = 'Portfolio' AND project_id IS NULL)
    )
);

-- RLS Policy
ALTER TABLE controlling.cost_adjustments ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON controlling.cost_adjustments
    USING (tenant_id = current_setting('app.current_tenant')::uuid);

-- Indexes
CREATE INDEX idx_adjustments_project ON controlling.cost_adjustments(project_id) WHERE project_id IS NOT NULL;
CREATE INDEX idx_adjustments_scope_category ON controlling.cost_adjustments(scope, category);
CREATE INDEX idx_adjustments_created_at ON controlling.cost_adjustments(created_at DESC);
```

**Note:** No table for `ProjectCostCalculation` — it's calculated on-demand.

---

### 9. CQRS Query Handlers

Controlling is **query-heavy** with minimal write operations. No command handlers for calculations.

#### 9.1 Queries (Read Side)

| Query | Handler | Returns |
|-------|---------|---------|
| `GetProjectCostQuery` | `GetProjectCostQueryHandler` | `ProjectCostDto` (calculated) |
| `GetPortfolioCostQuery` | `GetPortfolioCostQueryHandler` | `PortfolioCostDto` (aggregated) |
| `GetTopProjectsQuery` | `GetTopProjectsQueryHandler` | `List<ProjectSummaryDto>` (by margin) |
| `GetVarianceAnalysisQuery` | `GetVarianceAnalysisQueryHandler` | `VarianceReportDto` |
| `GetCostAdjustmentsQuery` | `GetCostAdjustmentsQueryHandler` | `List<CostAdjustmentDto>` |
| `GetControllingConfigQuery` | `GetControllingConfigQueryHandler` | `ControllingConfigDto` |

#### 9.2 Commands (Write Side)

| Command | Handler | Events Raised |
|---------|---------|---------------|
| `CreateCostAdjustmentCommand` | `CreateCostAdjustmentCommandHandler` | `CostAdjustmentCreated` |
| `UpdateCostAdjustmentCommand` | `UpdateCostAdjustmentCommandHandler` | `CostAdjustmentUpdated` |
| `DeleteCostAdjustmentCommand` | `DeleteCostAdjustmentCommandHandler` | `CostAdjustmentDeleted` |
| `UpdateControllingConfigCommand` | `UpdateControllingConfigCommandHandler` | `ControllingConfigUpdated` |

**Command Example:**

```csharp
// Application/Commands/Controlling/CreateCostAdjustmentCommand.cs
public sealed class CreateCostAdjustmentCommand : IRequest<Guid>
{
    public AdjustmentScope Scope { get; init; }
    public Guid? ProjectId { get; init; }
    public CostCategory Category { get; init; }
    public Money? PlannedAdjustment { get; init; }
    public Money? ActualAdjustment { get; init; }
    public string Reason { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid TenantId { get; init; }
}

// Application/Commands/Controlling/CreateCostAdjustmentCommandHandler.cs
public sealed class CreateCostAdjustmentCommandHandler : IRequestHandler<CreateCostAdjustmentCommand, Guid>
{
    private readonly IControllingRepository _repository;
    private readonly IEventBus _eventBus;

    public async Task<Guid> Handle(CreateCostAdjustmentCommand request, CancellationToken ct)
    {
        // 1. Create adjustment
        var adjustment = CostAdjustment.Create(
            scope: request.Scope,
            category: request.Category,
            plannedAdjustment: request.PlannedAdjustment,
            actualAdjustment: request.ActualAdjustment,
            reason: request.Reason,
            createdBy: request.CreatedBy,
            tenantId: request.TenantId,
            projectId: request.ProjectId);

        // 2. Persist
        await _repository.AddAdjustmentAsync(adjustment, ct);

        // 3. Publish event
        await _eventBus.PublishAsync(new CostAdjustmentCreated
        {
            AdjustmentId = adjustment.Id,
            Scope = adjustment.Scope,
            ProjectId = adjustment.ProjectId,
            Category = adjustment.Category,
            PlannedAdjustment = adjustment.PlannedAdjustment,
            ActualAdjustment = adjustment.ActualAdjustment,
            Reason = adjustment.Reason,
            CreatedBy = adjustment.CreatedBy,
            TenantId = adjustment.TenantId
        }, ct);

        return adjustment.Id;
    }
}
```

---

### 10. API Endpoints

#### 10.1 Query Endpoints (Read-Heavy)

| Method | Endpoint | Query | Auth |
|--------|----------|-------|------|
| GET | `/api/controlling/projects/{projectId}/costs` | `GetProjectCostQuery` | `controlling.view` |
| GET | `/api/controlling/portfolio/costs` | `GetPortfolioCostQuery` | `controlling.view` |
| GET | `/api/controlling/portfolio/top-projects` | `GetTopProjectsQuery` | `controlling.view` |
| GET | `/api/controlling/projects/{projectId}/variance` | `GetVarianceAnalysisQuery` | `controlling.view` |
| GET | `/api/controlling/adjustments?projectId={id}` | `GetCostAdjustmentsQuery` | `controlling.view` |
| GET | `/api/controlling/config` | `GetControllingConfigQuery` | `controlling.view` |

#### 10.2 Command Endpoints (Write-Light)

| Method | Endpoint | Command | Auth |
|--------|----------|---------|------|
| POST | `/api/controlling/adjustments` | `CreateCostAdjustmentCommand` | `controlling.manage` |
| PUT | `/api/controlling/adjustments/{id}` | `UpdateCostAdjustmentCommand` | `controlling.manage` |
| DELETE | `/api/controlling/adjustments/{id}` | `DeleteCostAdjustmentCommand` | `controlling.manage` |
| PUT | `/api/controlling/config` | `UpdateControllingConfigCommand` | `controlling.admin` |

---

### 11. Testing Strategy

#### 11.1 Unit Tests (Calculation Logic)

```csharp
// Domain.UnitTests/ControllingTests.cs
public class ProjectCostCalculationTests
{
    [Fact]
    public void EAC_Calculation_UsesMaxOfPlannedAndActual()
    {
        // Arrange
        var planned = new Money(1000, Currency.HUF);
        var actual = new Money(1200, Currency.HUF);

        // Act
        var projected = Math.Max(planned.Amount, actual.Amount);

        // Assert
        Assert.Equal(1200, projected);
    }

    [Fact]
    public void Overhead_Calculation_UsesDirectCosts()
    {
        // Arrange
        var directCosts = new Money(10000, Currency.HUF);
        var overheadRate = 0.15m;

        // Act
        var overhead = directCosts.Amount * overheadRate;

        // Assert
        Assert.Equal(1500, overhead);
    }

    [Fact]
    public void Margin_Calculation_RevenueMinusCosts()
    {
        // Arrange
        var revenue = new Money(20000, Currency.HUF);
        var costs = new Money(15000, Currency.HUF);

        // Act
        var margin = revenue.Amount - costs.Amount;
        var marginPct = margin / revenue.Amount * 100;

        // Assert
        Assert.Equal(5000, margin);
        Assert.Equal(25m, marginPct);
    }
}
```

#### 11.2 Integration Tests (Full Calculation Pipeline)

```csharp
// Application.IntegrationTests/ControllingIntegrationTests.cs
public class ControllingCalculationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetProjectCost_CalculatesFromMultipleSources()
    {
        // Arrange (seed data)
        var projectId = await CreateProjectAsync();
        await CreateMfgPrepAsync(projectId, materialCost: 1000);
        await CreateTimeLogAsync(projectId, hours: 10, hourlyRate: 50);
        await CreateWarehouseReceiptAsync(projectId, cost: 1200);

        // Act
        var response = await _client.GetAsync($"/api/controlling/projects/{projectId}/costs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProjectCostDto>();
        Assert.Equal(1000, result.CostCategories["Material"].Planned);
        Assert.Equal(1200, result.CostCategories["Material"].Actual);
        Assert.Equal(500, result.CostCategories["Labor"].Planned);
        Assert.Equal(500, result.CostCategories["Labor"].Actual);
    }

    [Fact]
    public async Task GetPortfolioCost_AggregatesMultipleProjects()
    {
        // Arrange
        var project1 = await CreateProjectAsync();
        var project2 = await CreateProjectAsync();
        // ... seed data

        // Act
        var response = await _client.GetAsync("/api/controlling/portfolio/costs");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PortfolioCostDto>();
        Assert.Equal(2, result.ProjectCount);
        Assert.True(result.TotalActualRevenue > 0);
    }
}
```

#### 11.3 Performance Tests

```csharp
[Fact]
public async Task ProjectCostCalculation_CompleteWithin500ms()
{
    // Arrange
    var projectId = /* large project with 100+ cost entries */;
    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = await _handler.Handle(new GetProjectCostQuery { ProjectId = projectId });

    // Assert
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 500, "Calculation should complete within 500ms");
}
```

---

### 12. Performance & Scalability Considerations

#### 12.1 Caching Strategy

- **Project costs:** Cache per project for 5 minutes (invalidate on related data change)
- **Portfolio aggregation:** Cache for 10 minutes
- **Config:** Cache indefinitely (invalidate on update)

```csharp
public class CachedProjectCostQuery : IRequestHandler<GetProjectCostQuery, ProjectCostDto>
{
    private readonly IMemoryCache _cache;
    private readonly IMediator _mediator;

    public async Task<ProjectCostDto> Handle(GetProjectCostQuery request, CancellationToken ct)
    {
        var cacheKey = $"project-cost-{request.ProjectId}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _mediator.Send(new GetProjectCostQuery { ProjectId = request.ProjectId }, ct);
        });
    }
}
```

#### 12.2 Database Optimization

- **Avoid N+1 queries:** Use batch loading for related data
- **Indexed foreign keys:** `project_id`, `tenant_id` on all cost source tables
- **Materialized view (optional):** Pre-aggregate common queries for dashboard

```sql
CREATE MATERIALIZED VIEW controlling.project_cost_summary AS
SELECT
    p.project_id,
    p.tenant_id,
    -- ... aggregated costs
FROM projects.projects p
-- ... joins to cost sources
GROUP BY p.project_id, p.tenant_id;

CREATE UNIQUE INDEX ON controlling.project_cost_summary(project_id);
```

**Refresh Strategy:** Hourly cron job or on-demand refresh

#### 12.3 Archival Strategy

- **Old adjustments:** Archive adjustments older than 2 years
- **Historical calculations:** Store monthly snapshots for trend analysis

---

## Consequences

### Positive

1. **Single Source of Truth:** No data duplication, calculations derive from existing modules
2. **Real-Time Insights:** On-demand calculations reflect current state
3. **Flexible Corrections:** Manual adjustments handle edge cases without polluting source data
4. **Scalable Architecture:** Calculation engine can be optimized independently
5. **Multi-Tenant Safe:** RLS policies ensure tenant isolation on config and adjustments

### Negative

1. **Calculation Performance:** Large portfolios may require caching or pre-aggregation
2. **Cross-Module Dependencies:** Requires stable contracts from 5+ modules
3. **Eventual Consistency:** Real-time data may lag if source modules batch updates
4. **Manual Adjustment Overhead:** Users must understand when to apply corrections

### Risks

| Risk | Mitigation |
|------|------------|
| **Slow Calculations** | Caching + materialized views + database indexing |
| **Integration Failures** | Circuit breakers + fallback to cached values |
| **Incorrect Adjustments** | Audit trail + approval workflow (future) |
| **Data Staleness** | Cache invalidation on source data change events |

---

## Implementation Plan

### Phase 1: Core Calculation Engine (Week 1)

1. Implement `ProjectCostCalculation` domain model
2. Implement `CategoryCost` value object
3. Implement EAC calculation logic
4. Unit tests for all calculation formulas

### Phase 2: Integration Contracts (Week 2)

1. Define service interfaces for all 5 integrations
2. Mock implementations for testing
3. Integration tests with mock data

### Phase 3: Manual Adjustments (Week 3)

1. Implement `CostAdjustment` entity
2. Database migrations (config, adjustments tables)
3. CRUD operations for adjustments
4. Event publishing

### Phase 4: Query Handlers (Week 4)

1. Implement all query handlers
2. Implement caching layer
3. Performance testing and optimization

### Phase 5: API Layer & Integration (Week 5)

1. REST API controllers
2. OpenAPI documentation
3. Authorization policies
4. Integrate with real Production, HR, Finance modules
5. E2E test: Create project → Track costs → Generate report

---

## References

- **ADR-002:** Modular Monolith — Separation of calculation engine from source modules
- **ADR-003:** Immutability & Audit Trail — Adjustments as immutable events
- **ADR-004:** Role-Based Access Control (RBAC) — controlling.view, controlling.manage, controlling.admin
- **JoineryTech Controlling Spec:** `/opt/spaceos/docs/joinerytech/CLAUDE.md` (§Kontrolling)
- **Event Sourcing Patterns:** `/opt/spaceos/docs/knowledge/patterns/EVENT_SOURCING_PATTERNS.md`
- **Database Patterns:** `/opt/spaceos/docs/knowledge/patterns/DATABASE_PATTERNS.md`

---

## Appendix A: Cost Category Mapping

| Category | JoineryTech Term | Planned Source | Actual Source |
|----------|------------------|----------------|---------------|
| **Material** | Anyag | MfgPrep material list | Warehouse receipts |
| **Labor** | Munka | MfgPrep labor hours | Time logs |
| **Subcontracting** | Bérmunka | B2B handshakes (planned) | B2B invoices (actual) |
| **Logistics** | Szállítás | Shipment estimates | Shipment invoices |
| **Supplier** | Beszállító | N/A | Inbound invoices |
| **Overhead** | Rezsi | % of direct costs | % of actual direct costs |

---

## Appendix B: Calculation Formulas Summary

```
# PLANNED COSTS
plannedMaterial = MfgPrep.MaterialCost
plannedLabor = MfgPrep.LaborCost
plannedSubcontracting = SUM(B2BHandshakes.PlannedValue)
plannedLogistics = SUM(Shipments.EstimatedCost)
plannedDirect = plannedMaterial + plannedLabor + plannedSubcontracting + plannedLogistics
plannedOverhead = plannedDirect * overheadRate

# ACTUAL COSTS
actualMaterial = SUM(WarehouseReceipts.TotalCost)
actualLabor = SUM(TimeLogs.Hours * TimeLogs.HourlyRate)
actualSubcontracting = SUM(B2BHandshakes.InvoicedValue)
actualLogistics = SUM(Shipments.ActualCost)
actualSupplier = SUM(InboundInvoices.Amount)
actualDirect = actualMaterial + actualLabor + actualSubcontracting + actualLogistics + actualSupplier
actualOverhead = actualDirect * overheadRate

# EAC (PROJECTED)
FOR EACH category:
    projected[category] = MAX(planned[category], actual[category])
costEAC = SUM(projected[category]) + overhead

# MARGINS
plannedMargin = plannedRevenue - (plannedDirect + plannedOverhead)
actualMargin = actualRevenue - (actualDirect + actualOverhead)
eacMargin = actualRevenue - costEAC

# PERCENTAGES
plannedMarginPct = plannedMargin / plannedRevenue * 100
actualMarginPct = actualMargin / actualRevenue * 100
eacMarginPct = eacMargin / actualRevenue * 100
```

---

**END OF ADR-055**
