---
id: MSG-BACKEND-095
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-030-DONE, MSG-CONDUCTOR-007
epic: EPIC-CUTTING-Q3
created: 2026-06-30
content_hash: faad942f86aa10d0cd2528c4cfc902a0e3c7d1643d4267a8009c1443b7525449
---

# Track A Test Suite — 23 Tests

## Context

MSG-BACKEND-030 (Track A Customer Portal Backend) implementation is DONE and building successfully, but **0 tests** were written.

The original task spec called for **23 tests** (18 unit + 5 integration):
- TenantResolverTests (10 unit)
- EmailServiceTests (8 unit)
- QuoteRequestEndpointTests (5 integration)

## Task

Implement the **complete test suite** for Track A backend components.

## Test Requirements

### 1. TenantResolverTests (10 unit tests)

**File:** `SpaceOS.Modules.Cutting.Tests/Services/TenantResolverTests.cs`

**Tests:**
1. ✅ Valid subdomain extraction from hostname
2. ✅ Subdomain resolution to TenantId (database query)
3. ✅ TenantNotFoundException when subdomain not found
4. ✅ InvalidOperationException when multiple tenants match
5. ✅ Null hostname handling
6. ✅ Empty hostname handling
7. ✅ Hostname without subdomain (e.g., "joinerytech.hu")
8. ✅ Complex hostname with multiple dots (e.g., "api.doorstar.joinerytech.hu")
9. ✅ Case-insensitive subdomain matching
10. ✅ Logging verification (ILogger mock)

**Mocking:**
- Mock `CuttingDbContext` with in-memory database (EF Core InMemory)
- Mock `ILogger<TenantResolver>`

**Test Data:**
- Seed Tenants: `doorstar` (TenantId: 1), `lapszabasz-kft` (TenantId: 2)

---

### 2. EmailServiceTests (8 unit tests)

**File:** `SpaceOS.Modules.Cutting.Tests/Services/EmailServiceTests.cs`

**Tests:**
1. ✅ SendQuoteRequestNotification sends 2 emails (customer + admin)
2. ✅ SendQuoteApprovedNotification sends 1 email (customer)
3. ✅ SendQuoteRejectedNotification sends 1 email (customer)
4. ✅ Email template placeholders replaced correctly
5. ✅ SMTP connection error handling (Exception → logged)
6. ✅ Invalid email address validation
7. ✅ Configuration validation (SmtpHost, SmtpPort required)
8. ✅ Email content encoding (UTF-8)

**Mocking:**
- Mock `ISmtpClient` (MailKit wrapper)
- Mock `ILogger<EmailService>`
- Mock `IConfiguration` (appsettings Email section)

**Test Data:**
- Valid email: "test@example.com"
- Invalid email: "invalid-email"
- Quote details: QuoteRequestId, CustomerName, Dimensions, Material

---

### 3. QuoteRequestEndpointTests (5 integration tests)

**File:** `SpaceOS.Modules.Cutting.Tests/Endpoints/QuoteRequestEndpointTests.cs`

**Tests:**
1. ✅ POST /api/cutting/quote-request returns 201 Created
2. ✅ POST with X-Original-Host header resolves tenant
3. ✅ POST with invalid subdomain returns 404 (TenantNotFoundException)
4. ✅ POST triggers email notifications (verify EmailService called)
5. ✅ POST with invalid request body returns 400 Bad Request

**Test Setup:**
- Use `WebApplicationFactory<Program>` (ASP.NET Core integration test)
- Mock `ITenantResolver` (return TenantId: 1 for "doorstar")
- Mock `IEmailService` (verify SendQuoteRequestNotification called)
- In-memory database (EF Core InMemory)

**Test Data:**
- Valid request body:
  ```json
  {
    "customerName": "Test Customer",
    "email": "test@example.com",
    "phone": "+36 30 123 4567",
    "width": 1000,
    "height": 2000,
    "panelCount": 5,
    "materialId": 1
  }
  ```

---

## Acceptance Criteria

- [ ] All 23 tests implemented and passing
- [ ] Test projects build successfully (0 errors)
- [ ] Coverage: >80% for TenantResolver, EmailService, QuoteRequestEndpoints
- [ ] Tests use proper naming convention: `MethodName_Scenario_ExpectedBehavior`
- [ ] Tests use Arrange-Act-Assert pattern
- [ ] Mocking: Moq library (already in project)
- [ ] Integration tests: WebApplicationFactory

---

## Files to Create/Modify

**New test files:**
1. `backend/spaceos-modules-cutting/tests/SpaceOS.Modules.Cutting.Tests/Services/TenantResolverTests.cs`
2. `backend/spaceos-modules-cutting/tests/SpaceOS.Modules.Cutting.Tests/Services/EmailServiceTests.cs`
3. `backend/spaceos-modules-cutting/tests/SpaceOS.Modules.Cutting.Tests/Endpoints/QuoteRequestEndpointTests.cs`

**Test project setup (if missing):**
- `SpaceOS.Modules.Cutting.Tests.csproj` — ensure dependencies:
  - `xUnit` 2.4+
  - `Moq` 4.18+
  - `Microsoft.EntityFrameworkCore.InMemory` 8.0+
  - `Microsoft.AspNetCore.Mvc.Testing` 8.0+

---

## Related Files (Reference)

**Implementation files:**
- `/opt/spaceos/backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Infrastructure/Services/TenantResolver.cs`
- `/opt/spaceos/backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Infrastructure/Services/EmailService.cs`
- `/opt/spaceos/backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Api/Endpoints/QuoteRequestEndpoints.cs`

**Original DONE message:**
- `/opt/spaceos/terminals/backend/outbox/2026-06-23_030_track-a-customer-portal-done.md`

---

## Priority Justification

**High priority** because:
1. Track A frontend (MSG-FRONTEND-018) is APPROVED and ready
2. Production deployment requires test coverage
3. Q3 roadmap: 2. ügyfél onboarding depends on Track A completion
4. Current status: 80% complete (missing only tests + deployment)

---

## Expected Effort

**Estimate:** 1 day (6-8 hours)
- TenantResolverTests: 2-3 hours
- EmailServiceTests: 2-3 hours
- QuoteRequestEndpointTests: 2-3 hours

---

## Success Metrics

```bash
dotnet test --no-build --verbosity normal
# Expected output:
# Passed! - Failed: 0, Passed: 23, Skipped: 0, Total: 23
```

---

**Dispatched by:** Conductor (Karmester)
**Date:** 2026-06-30T16:40:00Z
**Track:** Q3 Cutting Expansion - Track A (Customer Portal)
