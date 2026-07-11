---
id: MSG-BACKEND-141
from: conductor
to: backend
type: task
priority: high
status: READ
read_at: 2026-07-04
model: sonnet
epic_id: EPIC-JT-CTRL
ref: MSG-ARCHITECT-060-DONE
created: 2026-07-04
estimated_nwt: 240
blocked_by: CP-CRM-BACKEND
content_hash: 30d2e2f4d06b58893bc2f726fb8fd2185ad54cbe05efb750e52408b2944768c0
---

# JoineryTech Kontrolling — Week 1 Domain Layer Implementation

**Epic:** EPIC-JT-CTRL (Kontrolling Modul)
**Priority:** HIGH (queued, process after CRM API completion)
**Estimated:** 240 NWT (~8 hours)
**Blocked By:** MSG-BACKEND-103 (CRM API implementation — in progress)

---

## ⚠️ TASK DEPENDENCY

**DO NOT START** until MSG-BACKEND-103 (CRM API) is complete and DONE.

**Rationale:** Focus on CRM API completion first, then switch to Kontrolling Week 1.

**When to start:**
- ✅ MSG-BACKEND-103 status = DONE
- ✅ CRM API endpoints tested and validated
- ✅ CP-CRM-BACKEND checkpoint marked done in EPICS.yaml

---

## Context

Kontrolling Module Week 0 (OpenAPI spec) is **DONE** (MSG-ARCHITECT-060-DONE).

**OpenAPI Spec:**
- `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml` (~1,200 lines)
- 10 endpoints (6 GET queries + 4 POST/PUT/DELETE commands)
- Orval code-gen tested ✅ (Frontend TypeScript client generated)

**Domain Model Reference:** ADR-055 (JoineryTech Kontrolling Domain Model)

---

## Task: Week 1 — Domain Layer Implementation

**Week 1 Focus:** Domain entities, value objects, aggregates, FSM (if applicable)

### Deliverables

#### 1. Project Structure

**Create module:** `/opt/spaceos/spaceos-modules-kontrolling/`

```
spaceos-modules-kontrolling/
  Domain/
    Aggregates/
      ProjectCostCalculation.cs          # Calculated aggregate (not stored)
      CostAdjustment.cs                  # Stored entity
    ValueObjects/
      Money.cs                           # Monetary value (amount + currency)
      CategoryCost.cs                    # Cost breakdown (planned, actual, projected, variance)
      Revenue.cs                         # Planned and actual revenue
      Margin.cs                          # Margin (amount + percentage)
    Enums/
      CostCategory.cs                    # Material, Labor, Subcontracting, Logistics, Supplier, Overhead
      OverheadAllocationMethod.cs        # DirectCostPercentage, LaborHours, Revenue
      AdjustmentScope.cs                 # Project, Portfolio
    Events/
      CostAdjustmentCreated.cs
      CostAdjustmentDeleted.cs
      OverheadConfigUpdated.cs
  Application/
    (deferred to Week 2)
  Infrastructure/
    (deferred to Week 3)
  Api/
    (deferred to Week 4)
```

#### 2. Domain Aggregates

**A. ProjectCostCalculation (Calculated, NOT Stored)**

**Purpose:** Real-time cost calculation aggregate

**Properties:**
```csharp
public class ProjectCostCalculation
{
    public Guid ProjectId { get; private set; }

    // Revenue
    public Revenue Revenue { get; private set; }

    // Costs by category
    public Dictionary<CostCategory, CategoryCost> CostByCategory { get; private set; }

    // Calculated fields
    public Money TotalPlannedCost { get; private set; }
    public Money TotalActualCost { get; private set; }
    public Money CostEAC { get; private set; }        // Estimate at Completion
    public Margin EACMargin { get; private set; }     // EAC-based margin
    public Money Overhead { get; private set; }

    // Variance
    public Money TotalVariance { get; private set; }
    public decimal VariancePercentage { get; private set; }
}
```

**Key Methods:**
- `Calculate(...)` — Main calculation method (takes integration data + overhead config)
- `CalculateEAC()` — EAC projection using `MAX(planned, actual)` per category
- `CalculateOverhead(OverheadConfig)` — Overhead calculation (DirectCostPercentage default)
- `CalculateVariance()` — Planned vs. Actual variance

**EAC Formula (ADR-055):**
```csharp
For each category:
  projected[category] = Math.Max(planned[category], actual[category])

costEAC = projected.Sum() + overhead

eacMargin = actualRevenue - costEAC
eacMarginPct = eacMargin / actualRevenue * 100
```

**B. CostAdjustment (Stored Entity)**

**Purpose:** Manual cost adjustment (corrections, reclassifications)

**Properties:**
```csharp
public class CostAdjustment : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid? ProjectId { get; private set; }      // Nullable if scope = Portfolio
    public CostCategory Category { get; private set; }
    public Money Amount { get; private set; }
    public string Reason { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public AdjustmentScope Scope { get; private set; } // Project or Portfolio
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

**Domain Events:**
- `CostAdjustmentCreated` (when adjustment is created)
- `CostAdjustmentDeleted` (when adjustment is deleted)

**Invariants:**
- `Reason` must be at least 10 characters
- `Amount` must be non-zero
- If `Scope = Project`, then `ProjectId` is required

#### 3. Value Objects

**A. Money**
```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "HUF") => new Money(0, currency);
    public Money Add(Money other) => new Money(Amount + other.Amount, Currency);
    public Money Subtract(Money other) => new Money(Amount - other.Amount, Currency);
    // ... operators
}
```

**B. CategoryCost**
```csharp
public record CategoryCost(
    Money Planned,
    Money Actual,
    Money Projected,    // MAX(Planned, Actual) for EAC
    Money Variance      // Actual - Planned
);
```

**C. Revenue**
```csharp
public record Revenue(Money Planned, Money Actual);
```

**D. Margin**
```csharp
public record Margin(Money Amount, decimal Percentage);
```

#### 4. Enums

**A. CostCategory**
```csharp
public enum CostCategory
{
    Material = 1,      // Warehouse receipts
    Labor = 2,         // HR time logs
    Subcontracting = 3,// B2B handshakes
    Logistics = 4,     // Shipment costs
    Supplier = 5,      // Inbound invoices
    Overhead = 6       // Calculated percentage
}
```

**B. OverheadAllocationMethod**
```csharp
public enum OverheadAllocationMethod
{
    DirectCostPercentage = 1,  // Default: overhead = directCosts * rate
    LaborHours = 2,            // overhead = totalLaborHours * hourlyRate
    Revenue = 3                // overhead = revenue * rate
}
```

**C. AdjustmentScope**
```csharp
public enum AdjustmentScope
{
    Project = 1,    // Adjustment applies to specific project
    Portfolio = 2   // Adjustment applies to all projects
}
```

#### 5. Domain Events

**A. CostAdjustmentCreated**
```csharp
public record CostAdjustmentCreated(
    Guid AdjustmentId,
    Guid TenantId,
    Guid? ProjectId,
    CostCategory Category,
    Money Amount,
    string Reason,
    DateTime EffectiveDate,
    AdjustmentScope Scope
) : DomainEvent;
```

**B. CostAdjustmentDeleted**
```csharp
public record CostAdjustmentDeleted(
    Guid AdjustmentId,
    Guid TenantId
) : DomainEvent;
```

**C. OverheadConfigUpdated**
```csharp
public record OverheadConfigUpdated(
    Guid TenantId,
    decimal OverheadRate,
    OverheadAllocationMethod Method
) : DomainEvent;
```

---

## Technical Constraints

### 1. ADR-055 Compliance

**Calculation-First Architecture:**
- ✅ `ProjectCostCalculation` is **calculated**, NOT stored
- ✅ Only `CostAdjustment` is stored (manual corrections only)
- ✅ One source of truth: integration data from 5 modules
- ✅ EAC formula: `MAX(planned, actual)` per category

### 2. .NET 8 Clean Architecture Pattern

**Follow existing Kernel patterns:**
- Use `Entity<TId>` base class (from Kernel.Domain.Abstractions)
- Use `DomainEvent` for events
- Use `ValueObject` pattern for Money, CategoryCost, etc.
- Use FluentValidation for invariants (Week 2)

### 3. Integration Data (Read-Only)

**ProjectCostCalculation.Calculate()** takes integration data as parameters:
```csharp
public void Calculate(
    Revenue revenue,
    MfgPrepCostData mfgPrepData,
    IEnumerable<TimeLogCostData> timeLogs,
    IEnumerable<WarehouseReceiptData> warehouseReceipts,
    IEnumerable<ShipmentCostData> shipments,
    IEnumerable<InboundInvoiceData> supplierInvoices,
    OverheadConfig overheadConfig
)
{
    // 1. Calculate planned costs from MfgPrep
    // 2. Calculate actual costs from integration data
    // 3. Calculate EAC using MAX(planned, actual)
    // 4. Calculate overhead
    // 5. Calculate variance
}
```

**Integration DTOs** (readonly, from other modules):
- `MfgPrepCostData` — Production module (planned Material + Labor)
- `TimeLogCostData` — HR module (actual Labor)
- `InboundInvoiceData` — Finance module (actual Supplier costs)
- `WarehouseReceiptData` — Warehouse module (actual Material)
- `ShipmentCostData` — Logistics module (actual Logistics)

---

## Acceptance Criteria

**DONE when:**
- [ ] Module structure created (`spaceos-modules-kontrolling/Domain/`)
- [ ] `ProjectCostCalculation` aggregate implemented (calculated, not stored)
- [ ] `CostAdjustment` entity implemented (stored)
- [ ] Value objects implemented: Money, CategoryCost, Revenue, Margin
- [ ] Enums implemented: CostCategory, OverheadAllocationMethod, AdjustmentScope
- [ ] Domain events implemented: CostAdjustmentCreated, CostAdjustmentDeleted, OverheadConfigUpdated
- [ ] EAC calculation method implemented (MAX formula)
- [ ] Overhead calculation methods implemented (3 allocation methods)
- [ ] Variance calculation implemented
- [ ] Unit tests: EAC calculation scenarios (5+ test cases)
- [ ] Unit tests: Overhead allocation (3 methods)
- [ ] Unit tests: CostAdjustment invariants
- [ ] Build succeeds: 0 errors, 0 warnings
- [ ] Code coverage: >80% (domain logic)

---

## Testing Requirements

### Unit Test Cases (Domain Logic)

**EAC Calculation:**
1. All costs realized → EAC = actual
2. No costs realized → EAC = planned
3. Partial realization → EAC = mix of planned + actual
4. Material overspend → EAC reflects overspend
5. Labor underutilization → EAC uses planned (not realized actual)

**Overhead Allocation:**
1. DirectCostPercentage (15% rate)
2. LaborHours (100 hours * 5000 HUF/hour)
3. Revenue (10% of revenue)

**CostAdjustment Invariants:**
1. Reason too short → validation error
2. Amount zero → validation error
3. Scope = Project but ProjectId null → validation error

**Example Test:**
```csharp
[Fact]
public void Calculate_WithPartialRealization_UsesEACFormula()
{
    // Arrange
    var plannedMaterial = Money.FromHUF(1_000_000);
    var actualMaterial = Money.FromHUF(1_200_000); // Overspend
    var plannedLabor = Money.FromHUF(500_000);
    var actualLabor = Money.FromHUF(0);            // Not yet realized

    // Act
    var calculation = ProjectCostCalculation.Calculate(...);

    // Assert
    calculation.CostEAC.Amount.Should().Be(
        1_200_000 + // Material (actual > planned → use actual)
        500_000     // Labor (actual = 0 → use planned)
    );
}
```

---

## Integration Points (Week 2-3)

**NOT in Week 1 scope:**
- EF Core configuration (Week 3)
- CQRS query handlers (Week 2)
- REST API controllers (Week 4)

**Week 1 Output:**
- Pure domain logic (no infrastructure dependencies)
- Testable in isolation (unit tests only)

---

## Files to Create (Week 1)

```
spaceos-modules-kontrolling/
  Domain/
    Aggregates/
      ProjectCostCalculation.cs         # 200-250 lines
      CostAdjustment.cs                 # 80-100 lines
    ValueObjects/
      Money.cs                          # 60-80 lines
      CategoryCost.cs                   # 30-40 lines
      Revenue.cs                        # 20-30 lines
      Margin.cs                         # 20-30 lines
    Enums/
      CostCategory.cs                   # 15 lines
      OverheadAllocationMethod.cs       # 10 lines
      AdjustmentScope.cs                # 8 lines
    Events/
      CostAdjustmentCreated.cs          # 15 lines
      CostAdjustmentDeleted.cs          # 10 lines
      OverheadConfigUpdated.cs          # 12 lines
  Domain.Tests/
    ProjectCostCalculationTests.cs      # 150-200 lines (5+ scenarios)
    CostAdjustmentTests.cs              # 80-100 lines (invariant tests)
    OverheadCalculationTests.cs         # 100-120 lines (3 allocation methods)
```

**Total:** ~1,000-1,200 lines of domain code + tests

---

## References

- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- **Domain Model:** `/opt/spaceos/docs/architecture/decisions/ADR-055-joinerytech-kontrolling-domain-model.md`
- **Endpoint Inventory:** `/opt/spaceos/docs/api/kontrolling-endpoint-inventory.md`
- **Kernel Base Classes:** `spaceos-kernel/Kernel.Domain.Abstractions/`
- **CRM Domain Example:** `spaceos-modules-crm/Domain/` (reference pattern)

---

## Priority Rationale

**Why QUEUED (not immediate):**
- ✅ Backend focused on CRM API (MSG-BACKEND-103) — **don't interrupt**
- ✅ Week 0 spec DONE (foundation ready)
- ✅ No urgency (Kontrolling parallel track, not blocking CRM)

**When to ACTIVATE:**
- ✅ CRM API complete (MSG-BACKEND-103 DONE)
- ✅ EPICS.yaml CP-CRM-BACKEND = done
- ✅ Backend available capacity

**Timeline:**
- CRM API: ~8-12 hours remaining (estimated)
- Then: Kontrolling Week 1 (8 hours)

---

**Next After Completion:**
Week 2 — Application layer (CQRS handlers, calculation engine service)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
