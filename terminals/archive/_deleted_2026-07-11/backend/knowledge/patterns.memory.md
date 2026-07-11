# Backend Patterns Memory

> **Recurring patterns and best practices** — Warm memory (14d TTL)
>
> Visszatérő minták és bevált gyakorlatok.

## .NET Patterns

### Clean Architecture
[Repository pattern, CQRS, Mediator]

### FSM Aggregate Pattern ⭐ NEW
[State machine aggregates for lifecycle management]
- **Use case:** Lead, Opportunity, HR attendance, QA inspection, Work Orders
- **Benefits:** Type-safe state transitions, PostgreSQL RLS per state, audit trail
- **Pattern:** Aggregate root with explicit FSM state + transition methods
- **Skill:** `fsm-aggregate-generator` (60-70% time savings)
- **Docs:** `/opt/spaceos/docs/knowledge/engineering/BACKEND_PATTERNS.md` (lines 77-180)

**Example:**
```csharp
public class Lead
{
    public LeadState State { get; private set; }

    public Result MarkContacted(DateTime contactedAt)
    {
        if (State != LeadState.New)
            return Result.Error("Can only mark contacted from New state");

        State = LeadState.Contacted;
        AddDomainEvent(new LeadContactedEvent(Id, contactedAt));
        return Result.Success();
    }
}
```

**PostgreSQL RLS:**
```sql
-- Sales users see New/Contacted, Managers see all
CREATE POLICY leads_sales_access ON crm.leads
    USING (state IN (1, 2) OR current_setting('app.user_role') = 'Manager');
```

### Entity Framework
[Migration pattern, DbContext configuration, RLS setup]

### Testing Patterns
[Unit test structure, integration test setup, Testcontainers]

---

## Development Workflow Patterns

### Contract-First Development ⭐ NEW
[OpenAPI Week 0 workflow for parallel Backend + Frontend development]
- **ROI:** $4k investment → $11k-16k savings (175%-300% return)
- **Pattern:** Write complete OpenAPI 3.1 spec **before** implementation
- **Benefits:** Parallel dev, early validation, auto code-gen, zero rework
- **Skill:** `contract-first-development-workflow`
- **Docs:** `/opt/spaceos/docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md`

**Workflow:**
```
Week 0 (3-4 days): OpenAPI spec writing
  ├─ Architect + Backend + Frontend collaborate
  ├─ All endpoints documented with examples
  ├─ Error responses defined (400, 401, 422, 500)
  └─ Teams review and approve spec

Week 1-4: Parallel Development
  ├─ Backend implements API (follows spec)
  └─ Frontend uses mocks (follows spec)

Week 5: Integration works first time ✅
```

**Code Generation:**
- Frontend: Orval (React Query hooks)
- Backend/Orchestrator: NSwag (TypeScript client)

---

## Infrastructure & Operations

### Infrastructure Blocker Resolution ⭐ NEW
[L1-L4 escalation for network, build, deploy, external service issues]
- **MTTR Goal:** <24 hours resolution
- **Pattern:** Structured diagnosis decision tree
- **Escalation:** VPS operator if >24h timeout
- **Skill:** `infrastructure-blocker-resolution-guide`
- **Docs:** `/opt/spaceos/docs/knowledge/debugging/INFRASTRUCTURE_BLOCKER_RESOLUTION.md`

**Decision Tree:**
```
Infrastructure Blocker
  ├─ Network Issue? → Check DNS, firewall, bandwidth
  ├─ Build Issue? → Check toolchain, dependencies, SDK version
  ├─ Deploy Issue? → Check SSH, ports, systemd, .env
  └─ External Service? → Check status page, implement circuit breaker
```

**Case Study:** NuGet Timeout (MSG-ROOT-002)
- Day 1: Backend detects → BLOCKED outbox → Conductor escalates
- Day 1: Root diagnoses network issue → contacts VPS operator
- Day 2: Firewall rule fixed → Backend unblocked

---

## Security Patterns

### Authentication/Authorization
[JWT handling, RBAC, RLS SQL patterns]

### Input Validation
[FluentValidation, SQL injection prevention]

---

## Common Solutions

### Database Gotchas
[MapInboundClaims, GUC RLS, connection string quirks]

### API Endpoint Patterns
[Minimal API structure, TypedResults, error handling]

---

**Last updated:** 2026-07-04 (MSG-BACKEND-129)
**Knowledge source:** Explorer JoineryTech synthesis + Librarian curation
