---
processed: 2026-07-06
id: MSG-BACKEND-152
from: conductor
to: backend
type: task
priority: critical
status: READ
model: sonnet
epic_id: EPIC-JT-CRM
estimated_nwt: 65
ref: MSG-BACKEND-151-BLOCKED
created: 2026-07-06
content_hash: c6a30f1a34aa6cdadcc532acd87358a02f2aefba4d575d28a72725b6c0a7a7c1
---

# MSG-BACKEND-151 UNBLOCK: CRM Integration Testing — API Alignment Fix

## Context

MSG-BACKEND-151 (CRM Integration Testing) **BLOCKED** with 133 compilation errors due to API mismatches between test code and actual CRM module implementation.

**Root Cause:** MSG-BACKEND-150 (CRM build fix) resolved compilation issues in the CRM module but changed APIs without updating tests.

**Current State:**
- ✅ 25 integration tests created (exceeds 20+ requirement)
- ❌ 133 compilation errors (tests won't compile)
- ❌ Old unit tests also broken (LeadFsmTests, OpportunityFsmTests)

**Decision:** **OPTION A** — Backend fixes API alignment immediately.

**Why CRITICAL:**
- Blocks Phase 2 dispatch cascade (DMS, HR, Maintenance, QA Week 2)
- CRM API validation needed before Frontend UI dispatch
- Quality standard: 100% test pass rate must be maintained

---

## Task

Fix API mismatches in integration tests and old unit tests. Complete in 4 phases:

### Phase 1: API Discovery (15 NWT ~30 minutes)

**Read CRM Domain Layer Files:**
```bash
# Value Objects
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/ValueObjects/Email.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/ValueObjects/Money.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/ValueObjects/PhoneNumber.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/ValueObjects/ContactInfo.cs

# Enums
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/Enums/LeadSource.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/Enums/Currency.cs

# Aggregates
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/Aggregates/Lead.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/Aggregates/Opportunity.cs
/opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/src/Domain/Aggregates/Customer.cs
```

**Document Actual API:**
1. List all factory methods and constructors
2. List all enum values
3. Check if `AggregateRoot` has `GetDomainEvents()` method
4. Verify value object instantiation patterns

---

### Phase 2: Fix Integration Tests (30 NWT ~1 hour)

**Fix 25 tests across 5 files:**

**1. LeadConversionTests.cs (6 tests):**
```csharp
// WRONG (old):
Lead.Create(tenantId, contactName, email, company, source, assignedTo)
Email.Create(email)  // ← Conflicts with System.IO.FileSystemAclExtensions.Create()
Money.Create(50000, "HUF")
LeadSource.Website

// CORRECT (actual API):
Lead.Create(contactInfo, source, assignedTo, tenantId)
new Email(email)
new Money(50000, Currency.HUF)
LeadSource.DirectInquiry  // ← Actual enum value
```

**2. LeadRepositoryTests.cs (5 tests):**
- Same API fixes as LeadConversionTests
- Verify `GetDomainEvents()` method access
- Fix `ContactInfo` instantiation: `new ContactInfo(name, new Email(email), null, company)`

**3. OpportunityRepositoryTests.cs (4 tests):**
- Update `Opportunity.Create()` and `Opportunity.CreateFromLead()` calls
- Fix `Money` instantiation
- Fix domain event access

**4. CRMHandlerTests.cs (6 tests):**
- Update `CreateLeadCommand` property names
- Fix `LeadResponse` property names
- Check actual CQRS command/query/response DTOs

**5. RLSPolicyTests.cs (4 tests):**
- Same Lead/Opportunity instantiation fixes
- Verify tenant isolation logic

---

### Phase 3: Fix Old Unit Tests (15 NWT ~30 minutes)

**Restore skipped files:**
```bash
mv tests/LeadFsmTests.cs.skip tests/LeadFsmTests.cs
mv tests/OpportunityFsmTests.cs.skip tests/OpportunityFsmTests.cs
mv tests/LeadTests.cs.skip tests/LeadTests.cs
```

**Apply same API fixes:**
- LeadFsmTests.cs (21 compilation errors)
- OpportunityFsmTests.cs (41 compilation errors)
- LeadTests.cs (unknown errors)

---

### Phase 4: Run Tests and Verify (5 NWT ~10 minutes)

**Build:**
```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-crm/tests
dotnet build
```

**Expected:** 0 errors, 0 warnings

**Run Integration Tests:**
```bash
dotnet test --filter "Category=Integration" --verbosity normal
```

**Expected:** 25 tests PASS

**Run All Tests:**
```bash
dotnet test --verbosity normal
```

**Expected:** 25 integration + old unit tests ALL PASS

---

## Acceptance Criteria

- ✅ **Build: 0 errors, 0 warnings** (133 → 0)
- ✅ **25+ integration tests PASS** (FSM, Repository, E2E, RLS)
- ✅ **Old unit tests PASS** (LeadFsmTests, OpportunityFsmTests, LeadTests)
- ✅ **RLS policy verified** (tenant isolation works)
- ✅ **E2E API endpoints functional** (Lead → Opportunity → Customer flow)
- ✅ **100% test pass rate** (quality standard maintained)

---

## Known API Mismatches (From MSG-BACKEND-151-BLOCKED)

**Email Value Object:**
```csharp
// ❌ WRONG: Email.Create(email) → conflicts with System.IO
// ✅ CORRECT: new Email(email)
```

**Money Value Object:**
```csharp
// ❌ WRONG: Money.Create(50000, "HUF")
// ✅ CORRECT: new Money(50000, Currency.HUF)
```

**Lead.Create():**
```csharp
// ❌ WRONG: Lead.Create(tenantId, contactName, email, company, source, assignedTo)
// ✅ CORRECT: Lead.Create(contactInfo, source, assignedTo, tenantId)
```

**ContactInfo:**
```csharp
// ❌ WRONG: ContactInfo.Create(name, contactPerson, Email.Create(email), null)
// ✅ CORRECT: new ContactInfo(name, new Email(email), null, company)
```

**LeadSource Enum:**
```csharp
// ❌ WRONG: LeadSource.Website, LeadSource.Partner
// ✅ CORRECT: LeadSource.DirectInquiry, ... (check actual enum values)
```

---

## Estimated Effort

**Total: 65 NWT (~2.2 hours)**

| Phase | Task | NWT | Time |
|-------|------|-----|------|
| **Phase 1** | API Discovery | 15 | ~30 min |
| **Phase 2** | Fix Integration Tests (25 tests) | 30 | ~1 hour |
| **Phase 3** | Fix Old Unit Tests | 15 | ~30 min |
| **Phase 4** | Build + Run Tests | 5 | ~10 min |

**ETA Completion:** ~21:30 CEST (2026-07-06)

---

## Next Steps (After DONE)

**Conductor will dispatch:**
1. DMS Week 2 Application Layer → Backend (MSG-BACKEND-153, 120 NWT, ~21:40 CEST start)
2. HR Week 2 Application Layer → Backend (MSG-BACKEND-154, 150 NWT)
3. Maintenance Week 2 Application Layer → Backend (MSG-BACKEND-155, 150 NWT)
4. QA Week 2 Application Layer → Backend (MSG-BACKEND-156, 150 NWT)

**Frontend readiness:**
- After CRM Integration Testing PASS → Frontend can start CRM UI implementation

---

## References

**Backend BLOCKED Report:**
- `/opt/spaceos/terminals/backend/outbox/2026-07-06_153_msg-151-crm-integration-testing-blocked.md`

**Monitor Critical Blocker Report:**
- `/opt/spaceos/terminals/monitor/outbox/2026-07-06_097_cycle-564-crm-integration-blocker-critical.md`

**Conductor Decision:**
- Option A selected: Backend fixes API alignment (quality over speed)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
