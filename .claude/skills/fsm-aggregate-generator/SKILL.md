# Skill: FSM Aggregate Generator

## Overview

Reusable Finite State Machine (FSM) aggregate root templates for DDD domains. Accelerates new module development from 2-3 days to 8-12 hours.

## When to Use

- Starting new domain module (Kontrolling, HR, QA, Maintenance, etc.)
- FSM state transitions need enforcement
- Multi-tenant isolation required (PostgreSQL RLS)
- CQRS pattern needed (Commands + Queries)

## Domain Aggregate Templates

### 1. Lead Aggregate (CRM Template)

**FSM States:** `New` → `Contacted` → `Qualified` → `Converted` (or `Lost`/`Abandoned`)

**Copy-paste ready C# skeleton:**

```csharp
public class Lead : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Company { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }

    public LeadStatus Status { get; private set; } // FSM State
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // FSM Transitions
    public void MarkContacted(string notes)
    {
        if (Status != LeadStatus.New)
            throw new InvalidOperationException("Cannot mark as contacted");
        Status = LeadStatus.Contacted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkQualified(decimal estimatedValue)
    {
        if (Status != LeadStatus.Contacted)
            throw new InvalidOperationException("Cannot mark as qualified");
        Status = LeadStatus.Qualified;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Convert(Guid opportunityId)
    {
        if (Status != LeadStatus.Qualified)
            throw new InvalidOperationException("Cannot convert unqualified lead");
        Status = LeadStatus.Converted;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Converted = 3,
    Lost = 4,
    Abandoned = 5
}
```

**PostgreSQL RLS:**
```sql
-- Leads table with multi-tenant isolation
CREATE TABLE leads (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    company VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(20),
    status INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

-- RLS Policy: User only sees their tenant's leads
CREATE POLICY leads_rls ON leads USING (
    tenant_id = current_setting('app.current_tenant_id')::UUID
);
```

**CQRS Handlers:**

```csharp
// Commands
public class CreateLeadCommand : ICommand<LeadDTO>
{
    public Guid TenantId { get; set; }
    public string Company { get; set; }
    public string Email { get; set; }
}

public class CreateLeadCommandHandler : ICommandHandler<CreateLeadCommand, LeadDTO>
{
    public async Task<LeadDTO> Handle(CreateLeadCommand cmd, CancellationToken ct)
    {
        var lead = new Lead { Id = Guid.NewGuid(), Company = cmd.Company, /* ... */ };
        await _repository.Add(lead, cmd.TenantId, ct);
        return _mapper.Map<LeadDTO>(lead);
    }
}

// Queries
public class GetLeadsQuery : IQuery<IEnumerable<LeadDTO>>
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}

public class GetLeadsQueryHandler : IQueryHandler<GetLeadsQuery, IEnumerable<LeadDTO>>
{
    public async Task<IEnumerable<LeadDTO>> Handle(GetLeadsQuery query, CancellationToken ct)
    {
        var leads = await _repository.GetLeads(query.TenantId, query.Page, query.Limit, ct);
        return leads.Select(l => _mapper.Map<LeadDTO>(l));
    }
}
```

### 2. Opportunity Aggregate (CRM Template)

**FSM States:** `Draft` → `Proposal` → `Negotiation` → `Won` (or `Lost`/`Abandoned`)

**Similar structure to Lead, with additions:**
- Owner (sales rep)
- Probability (0-100%)
- Expected value (currency)
- Close date

### 3. HR Time Tracking FSM

**States:** `Pending` → `Submitted` → `Approved` → `Paid` (or `Rejected`)

**Specific fields:**
- Employee ID
- Start/end time
- Hours worked
- Rate (hourly/daily)
- Status FSM

### 4. QA Inspection FSM

**States:** `Pending` → `InProgress` → `Reviewed` → `Passed` (or `Failed`/`Rework`)

**Specific fields:**
- Batch/lot ID
- Inspection type
- Findings (photo + notes)
- Verdict (pass/fail criteria)

### 5. Maintenance Work Order FSM

**States:** `Scheduled` → `InProgress` → `Completed` → `Closed` (or `Cancelled`)

**Specific fields:**
- Equipment ID
- Technician ID
- Start/end time
- Parts used
- Labor hours

## Boilerplate Package

### A. Repository Interface Pattern

```csharp
public interface ILeadRepository
{
    Task<Lead> GetById(Guid id, Guid tenantId, CancellationToken ct);
    Task<IEnumerable<Lead>> GetByStatus(LeadStatus status, Guid tenantId, CancellationToken ct);
    Task<Lead> Add(Lead lead, Guid tenantId, CancellationToken ct);
    Task<Lead> Update(Lead lead, Guid tenantId, CancellationToken ct);
    Task Delete(Guid id, Guid tenantId, CancellationToken ct);
}
```

### B. FluentValidation Preset

```csharp
public class CreateLeadValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Company is required")
            .MaximumLength(255).WithMessage("Company name too long");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Invalid phone format");
    }
}
```

### C. TypeScript DTOs

```typescript
export interface LeadDTO {
  id: string;
  tenantId: string;
  company: string;
  email: string;
  phone: string;
  status: 'new' | 'contacted' | 'qualified' | 'converted' | 'lost' | 'abandoned';
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeadRequest {
  company: string;
  email: string;
  phone: string;
}
```

## Quick Start Checklist

- [ ] Copy aggregate template (Lead/Opportunity/HR/QA)
- [ ] Customize FSM states (if different from template)
- [ ] Implement RLS PostgreSQL policy
- [ ] Create Command/Query handlers (CQRS)
- [ ] Add FluentValidation validators
- [ ] Write TypeScript DTOs (Frontend)
- [ ] Generate OpenAPI endpoints (via contract-first-development-workflow)
- [ ] Test FSM state transitions (unit tests)
- [ ] Verify RLS enforcement (integration tests)

## Time Estimate

| Task | Duration |
|------|----------|
| Copy template | 15 min |
| Customize FSM | 30 min |
| PostgreSQL RLS | 30 min |
| CQRS handlers | 1 hour |
| Validators | 30 min |
| TypeScript DTOs | 30 min |
| **Total** | **~3.5 hours** (vs 2-3 days without template) |

## Examples from JoineryTech

**Lead FSM (CRM):**
```
New → Contacted → Qualified → Converted
          ↓          ↓            ↓
        Lost    Abandoned    Closed
```

**Opportunity FSM (CRM):**
```
Draft → Proposal → Negotiation → Won
         ↓            ↓           ↓
       Lost      Abandoned    Closed
```

## Pattern: RLS + CQRS + FSM

All new modules follow this pattern:
1. **RLS:** Row-level security (PostgreSQL policies)
2. **CQRS:** Commands (mutations) + Queries (reads) separation
3. **FSM:** Finite state machine enforcement (aggregate root)

Together: Safe, scalable, auditable domain models.

## Related Patterns

- **Modular Monolith** (8 modules, each with FSM aggregates)
- **Walking Skeleton First** (Phase 1: auth FSM only)
- **Event Sourcing** (Phase 3: full audit trail)

## References

- ADR-058: JoineryTech Integration Architecture
- Domain-Driven Design (Evans)
- Clean Architecture (Martin)
- CQRS Pattern (Microsoft)

---

**Skill Created:** 2026-07-04
**Source:** JoineryTech CRM research (Lead + Opportunity aggregates)
**Time Savings:** 2-3 days → 8-12 hours per module
**Reusable Domains:** CRM, Kontrolling, HR, QA, Maintenance, EHS, DMS
