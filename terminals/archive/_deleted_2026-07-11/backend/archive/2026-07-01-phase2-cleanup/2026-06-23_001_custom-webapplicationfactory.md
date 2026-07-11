---
id: MSG-BACKEND-001
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-052
created: 2026-06-23
---

# Custom WebApplicationFactory - Test Blocker Resolution

## Context

A QuoteRequest integration tesztek failelnek tenant resolution és DB interceptor miatt.
A production kód READY, de az automated test coverage kritikus a deployment előtt.

## Blocker Details

**Original issue:** MSG-BACKEND-040-BLOCKED
- 954/966 teszt zöld
- 12 QuoteRequest integration teszt fail
- Root cause: TenantResolver + DbConnectionInterceptor test environment conflict

## Approved Solution: Option A - Custom WebApplicationFactory

✅ **Conductor döntés:** Custom test factory implementáció

**Rationale:**
- Test coverage critical (12 integration tests)
- Clean solution (no production code change)
- Optimal effort-value ratio (1-2h vs. full test suite coverage)
- Option C (skip tests) unacceptable for automated deployment

## Task Specification

**Scope:**
1. Create `CuttingWebApplicationFactory<TProgram>` class
2. Override `ConfigureWebHost` method
3. Mock/isolate problematic services:
   - TenantResolver → in-memory mock
   - IDbContextFactory → in-memory DB (no interceptors)
4. Update QuoteRequestEndpointTests to use custom factory
5. Verify 966/966 tests PASS

**Acceptance Criteria:**
- [ ] CuttingWebApplicationFactory.cs created in test project
- [ ] QuoteRequestEndpointTests updated (use custom factory)
- [ ] `dotnet test` → 966/966 PASSED
- [ ] Build: 0 errors, 0 warnings (suppress irrelevant warnings OK)

**Estimated Effort:** 1-2 hours

**Priority:** HIGH - blocks Track A deployment

## References

- Blocker message: MSG-BACKEND-040-BLOCKED
- Production code status: READY
- Current test status: 954/966 PASSED (12 QuoteRequest tests failing)

---

**Dispatched by Conductor**
2026-06-23 07:15 UTC
