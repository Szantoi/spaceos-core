# SpaceOS Catalog + EHS Hybrid Architecture (v4 BACKEND REVIEW)

**Status:** REVIEW
**Created:** 2026-06-23
**Reviewer:** Architect (Senior Backend Engineer perspective)
**Base Documents:** v1 + v2 DB Review + v3 Security Review
**Framework:** Clean Architecture + CQRS + SOLID Principles

---

## Review Summary

This document reviews the backend API design, validation, error handling, and performance from a senior backend engineering perspective. The review addresses **2 CRITICAL security findings from v3** and validates overall implementation quality.

---

## Findings Overview

| Severity | Count | Category |
|---|---|---|
| 🔴 CRITICAL | 0 | (v3 security fixes addressed) |
| 🟠 HIGH | 3 | Pagination, validation drift, error handling |
| 🟡 MEDIUM | 4 | Caching, repository pattern, testing strategy |
| 🟢 LOW | 2 | Code quality, performance optimization |

---

## 🟠 HIGH Severity Findings

### H1: Missing Pagination on History Endpoint

**Location:** `GET /api/ehs/risk-assessments/:assessmentId/history`
**Issue:** No pagination on potentially large result sets

**Current implementation (PROBLEMATIC):**
```csharp
// Returns ALL history records (unbounded)
public async Task<RiskAssessmentHistoryResponse> Handle(...)
{
    var history = await _dbContext.RiskAssessments
        .Where(r => r.AssessmentId == query.AssessmentId)
        .Where(r => r.DeletedAt == null)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();  // ⚠️ Could return 10,000+ records

    return mapper.Map(history);
}
```

**Problem:**
- A long-running assessment (5+ years) could have 1000+ risk calculations
- Returning all records in one response = slow query + large payload + client memory issues

**Required fix:**
```csharp
// Contracts/Requests/GetRiskAssessmentHistoryQuery.cs

public class GetRiskAssessmentHistoryQuery : IRequest<PagedRiskAssessmentHistoryResponse>
{
    public int AssessmentId { get; set; }
    public string? Period { get; set; } = "30d";  // 7d | 30d | 90d | all
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;  // Max 100
}

// Application/Handlers/GetRiskAssessmentHistoryHandler.cs

public async Task<PagedRiskAssessmentHistoryResponse> Handle(...)
{
    var skip = (query.Page - 1) * query.PageSize;
    var take = Math.Min(query.PageSize, 100);  // ✅ Cap at 100

    var historyQuery = _dbContext.RiskAssessments
        .Where(r => r.AssessmentId == query.AssessmentId)
        .Where(r => r.DeletedAt == null)
        .OrderByDescending(r => r.CreatedAt);

    // ✅ Get total count for pagination metadata
    var totalCount = await historyQuery.CountAsync();

    // ✅ Apply pagination
    var items = await historyQuery
        .Skip(skip)
        .Take(take)
        .ToListAsync();

    return new PagedRiskAssessmentHistoryResponse
    {
        Items = mapper.Map(items),
        Page = query.Page,
        PageSize = take,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)take)
    };
}

// Response DTO
public class PagedRiskAssessmentHistoryResponse
{
    public List<RiskAssessmentResponse> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
```

**Frontend update:**
```typescript
// hooks/useRiskCalculator.js

const loadHistory = async (assessmentId, page = 1) => {
  const response = await fetch(
    `/api/ehs/risk-assessments/${assessmentId}/history?page=${page}&pageSize=50`
  );
  const data = await response.json();

  set(state => ({
    riskAssessmentHistory: page === 1
      ? data.items  // First page: replace
      : [...state.riskAssessmentHistory, ...data.items],  // Load more: append
    historyPagination: {
      page: data.page,
      totalPages: data.totalPages,
      hasMore: data.page < data.totalPages
    }
  }));
};
```

**Validation checklist:**
- [ ] Add pagination parameters to query DTO
- [ ] Cap PageSize at 100 (prevent abuse)
- [ ] Return pagination metadata (total count, total pages)
- [ ] Update frontend to handle paginated responses
- [ ] Write E2E test: 150 assessments → 3 pages (50 per page)

**Impact if not fixed:**
- ❌ Slow queries on large datasets
- ❌ Client memory exhaustion (10k+ records)
- ❌ Poor UX (5+ second page loads)

**Recommendation:** **MUST FIX** in Week 1

---

### H2: Validation Schema Drift Risk (FluentValidation vs Zod)

**Location:** Validation schemas in backend (C#) and frontend (TypeScript)
**Issue:** Manual sync between FluentValidation and Zod schemas

**Current approach (RISKY):**
```csharp
// Backend: FluentValidation
public class CreateRiskAssessmentValidator : AbstractValidator<CreateRiskAssessmentRequest>
{
    public CreateRiskAssessmentValidator()
    {
        RuleFor(x => x.LikelihoodBefore).InclusiveBetween(1, 5);
        RuleFor(x => x.SeverityBefore).InclusiveBetween(1, 5);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

// Frontend: Zod (MANUALLY SYNCED!)
export const createRiskAssessmentSchema = z.object({
  likelihoodBefore: z.number().int().min(1).max(5),
  severityBefore: z.number().int().min(1).max(5),
  notes: z.string().max(2000).optional()
});
```

**Problem:**
If a developer updates FluentValidation rules (e.g., `MaximumLength(1000)`) but forgets to update Zod, **validation becomes inconsistent**:
- Frontend accepts 1500-char notes
- Backend rejects with 400 error
- User frustration: "Form said it was OK!"

**Solution Options:**

**Option A: Shared JSON Schema (RECOMMENDED for v2):**
```json
// shared/schemas/risk-assessment.schema.json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "likelihoodBefore": { "type": "integer", "minimum": 1, "maximum": 5 },
    "severityBefore": { "type": "integer", "minimum": 1, "maximum": 5 },
    "notes": { "type": "string", "maxLength": 2000 }
  },
  "required": ["likelihoodBefore", "severityBefore"]
}
```

Generate Zod + FluentValidation from JSON Schema:
```bash
# Generate Zod schema
npx json-schema-to-zod shared/schemas/*.schema.json --output src/schemas/

# Generate FluentValidation (manual or custom tool)
```

**Option B: OpenAPI-based validation (RECOMMENDED for v1 simplicity):**
```csharp
// Install Swashbuckle.AspNetCore
dotnet add package Swashbuckle.AspNetCore

// Generate OpenAPI spec
dotnet swagger tofile --output swagger.json

// Frontend generates Zod from OpenAPI
npx openapi-zod-client swagger.json --output src/api/schemas.ts
```

**Option C: Manual sync with validation tests (INTERIM FIX for Week 1):**
```typescript
// __tests__/schema-sync.test.ts

describe('Schema Sync: FluentValidation vs Zod', () => {
  it('notes field max length matches backend', async () => {
    // ✅ Test against live backend validation
    const payload = {
      likelihoodBefore: 3,
      severityBefore: 4,
      category: 'machinery',
      notes: 'x'.repeat(2001)  // Over limit
    };

    const response = await fetch('/api/ehs/risk-assessments', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({
      errors: { notes: expect.stringContaining('2000') }
    });

    // ✅ Zod should reject same payload
    const zodResult = createRiskAssessmentSchema.safeParse(payload);
    expect(zodResult.success).toBe(false);
  });
});
```

**Validation checklist:**
- [ ] Document validation rules in single source of truth (OpenAPI or JSON Schema)
- [ ] Write E2E test: Backend 400 error → Zod also rejects
- [ ] Set up CI check: OpenAPI spec validation

**Impact if not fixed:**
- 🟡 User frustration (frontend says OK, backend rejects)
- 🟡 Support tickets ("Form validation is broken")

**Recommendation:** **SHOULD FIX** in Week 2 (Option B - OpenAPI), interim fix Week 1 (Option C - tests)

---

### H3: Unstructured Error Responses

**Location:** Global exception handler
**Issue:** No standardized error response format

**Current approach (INCONSISTENT):**
```json
// Validation error (FluentValidation)
{
  "errors": {
    "LikelihoodBefore": ["'Likelihood Before' must be between 1 and 5."]
  }
}

// Domain exception
{
  "message": "Cannot create assessment for different organization"
}

// Unhandled exception
{
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while processing your request."
}
```

**Problem:**
Frontend needs to handle 3 different error formats. Inconsistent UX:
```typescript
// Brittle error handling
try {
  await createAssessment(data);
} catch (error) {
  // Which format? 🤷
  const message = error.errors?.LikelihoodBefore?.[0]
    || error.message
    || error.detail;
}
```

**Required fix (RFC 7807 - Problem Details):**
```csharp
// Contracts/Responses/ProblemDetailsResponse.cs

public class ProblemDetailsResponse
{
    public string Type { get; set; }  // "validation-error" | "not-found" | "forbidden"
    public string Title { get; set; }
    public int Status { get; set; }
    public string Detail { get; set; }
    public string Instance { get; set; }  // Request path
    public Dictionary<string, string[]>? Errors { get; set; }  // Validation errors
}

// Middleware/GlobalExceptionHandler.cs

public async ValueTask<bool> TryHandleAsync(...)
{
    var problem = exception switch
    {
        ValidationException validationEx => new ProblemDetailsResponse
        {
            Type = "validation-error",
            Title = "Validation failed",
            Status = 400,
            Detail = "One or more validation errors occurred",
            Instance = httpContext.Request.Path,
            Errors = validationEx.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
        },

        NotFoundException notFoundEx => new ProblemDetailsResponse
        {
            Type = "not-found",
            Title = "Resource not found",
            Status = 404,
            Detail = notFoundEx.Message,
            Instance = httpContext.Request.Path
        },

        ForbiddenAccessException forbiddenEx => new ProblemDetailsResponse
        {
            Type = "forbidden",
            Title = "Access denied",
            Status = 403,
            Detail = forbiddenEx.Message,
            Instance = httpContext.Request.Path
        },

        _ => new ProblemDetailsResponse
        {
            Type = "internal-error",
            Title = "Internal server error",
            Status = 500,
            Detail = _env.IsDevelopment() ? exception.Message : "An error occurred",
            Instance = httpContext.Request.Path
        }
    };

    httpContext.Response.StatusCode = problem.Status;
    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsJsonAsync(problem);

    return true;
}
```

**Frontend update:**
```typescript
// utils/api-client.ts

interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(public problem: ProblemDetails) {
    super(problem.detail);
  }

  get isValidationError() {
    return this.problem.type === 'validation-error';
  }

  get fieldErrors() {
    return this.problem.errors || {};
  }
}

// Usage in React component
try {
  await createAssessment(data);
} catch (error) {
  if (error instanceof ApiError && error.isValidationError) {
    // ✅ Consistent handling
    Object.entries(error.fieldErrors).forEach(([field, messages]) => {
      setError(field, { message: messages[0] });
    });
  } else {
    toast.error(error.message);
  }
}
```

**Validation checklist:**
- [ ] Implement RFC 7807 Problem Details response
- [ ] Return same structure for all error types
- [ ] Include correlation ID for error tracking
- [ ] Update frontend error handling

**Recommendation:** **SHOULD FIX** in Week 1

---

## 🟡 MEDIUM Severity Findings

### M1: Missing Caching Strategy

**Location:** `GET /api/ehs/risk-assessments/:assessmentId/history`
**Issue:** No HTTP caching headers

**Problem:**
Every request fetches data from DB, even if nothing changed. Wasted bandwidth + DB load.

**Required fix:**
```csharp
// Application/Handlers/GetRiskAssessmentHistoryHandler.cs

public async Task<PagedRiskAssessmentHistoryResponse> Handle(...)
{
    var history = await _dbContext.RiskAssessments
        .Where(...)
        .ToListAsync();

    // ✅ Generate ETag based on last modified timestamp
    var lastModified = history.MaxBy(r => r.CreatedAt)?.CreatedAt ?? DateTime.UtcNow;
    var etag = $"\"{lastModified:yyyyMMddHHmmss}\"";

    // ✅ Set caching headers
    httpContext.Response.Headers.Add("Cache-Control", "private, max-age=60");
    httpContext.Response.Headers.Add("ETag", etag);
    httpContext.Response.Headers.Add("Last-Modified", lastModified.ToString("R"));

    // ✅ Check If-None-Match header (client sends cached ETag)
    if (httpContext.Request.Headers["If-None-Match"] == etag)
    {
        httpContext.Response.StatusCode = 304;  // Not Modified
        return null;  // No body
    }

    return response;
}
```

**Validation checklist:**
- [ ] Add Cache-Control headers (private, max-age=60)
- [ ] Generate ETag based on last modified timestamp
- [ ] Return 304 Not Modified if ETag matches
- [ ] Write E2E test: Second request with ETag → 304 response

**Recommendation:** **SHOULD ADD** in Week 2

---

### M2: No Repository Abstraction

**Location:** Direct `DbContext` usage in handlers
**Issue:** Tight coupling to EF Core, hard to test

**Current approach:**
```csharp
// Handler tightly coupled to DbContext
public async Task<Response> Handle(...)
{
    var assessment = await _dbContext.RiskAssessments
        .Where(...)
        .FirstOrDefaultAsync();  // ⚠️ Direct EF Core usage
}
```

**Problem:**
- Hard to unit test (requires in-memory DB or Testcontainers)
- Cannot swap ORM (e.g., Dapper for perf queries)
- Complex queries repeated across handlers

**Recommended fix (Repository + Specification patterns):**
```csharp
// Domain/Repositories/IRiskAssessmentRepository.cs

public interface IRiskAssessmentRepository
{
    Task<RiskAssessment?> GetByIdAsync(Guid id);
    Task<List<RiskAssessment>> GetHistoryAsync(int assessmentId, int page, int pageSize);
    Task<int> CountHistoryAsync(int assessmentId);
    Task AddAsync(RiskAssessment assessment);
}

// Infrastructure/Repositories/RiskAssessmentRepository.cs

public class RiskAssessmentRepository : IRiskAssessmentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public async Task<List<RiskAssessment>> GetHistoryAsync(...)
    {
        return await _dbContext.RiskAssessments
            .Where(r => r.AssessmentId == assessmentId)
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

// Handler now depends on abstraction
public class GetRiskAssessmentHistoryHandler
{
    private readonly IRiskAssessmentRepository _repository;

    public async Task<Response> Handle(...)
    {
        var items = await _repository.GetHistoryAsync(query.AssessmentId, query.Page, query.PageSize);
        var total = await _repository.CountHistoryAsync(query.AssessmentId);
        ...
    }
}
```

**Unit test (mocked repository):**
```csharp
// Tests/Application/Handlers/GetRiskAssessmentHistoryHandlerTests.cs

[Fact]
public async Task Handle_ReturnsPagedHistory()
{
    // ✅ Mock repository (no DB needed)
    var mockRepo = new Mock<IRiskAssessmentRepository>();
    mockRepo.Setup(r => r.GetHistoryAsync(123, 1, 50))
        .ReturnsAsync(new List<RiskAssessment> { /* test data */ });

    var handler = new GetRiskAssessmentHistoryHandler(mockRepo.Object);

    var result = await handler.Handle(new GetRiskAssessmentHistoryQuery { AssessmentId = 123 });

    Assert.Equal(50, result.Items.Count);
}
```

**Recommendation:** **SHOULD ADD** in Week 2 (improves testability)

---

### M3: No Domain Validation in Entity

**Location:** `RiskAssessment` entity factory method
**Issue:** Factory method only creates, doesn't validate business rules

**Current implementation:**
```csharp
public static RiskAssessment Create(...)
{
    return new RiskAssessment
    {
        Id = Guid.NewGuid(),
        LikelihoodBefore = likelihoodBefore,  // ⚠️ No validation
        ...
    };
}
```

**Problem:**
Business rule: "If likelihood × severity > 15, notes are REQUIRED (high risk mitigation plan)."

This rule is NOT enforced in the domain model → validation only happens in FluentValidation (application layer). Domain layer is anemic.

**Recommended fix (Rich Domain Model):**
```csharp
// Domain/Entities/RiskAssessment.cs

public class RiskAssessment
{
    // ... properties ...

    public static RiskAssessment Create(...)
    {
        // ✅ Domain validation
        ValidateLikelihood(likelihoodBefore);
        ValidateSeverity(severityBefore);

        var riskScore = likelihoodBefore * severityBefore;

        if (riskScore > 15 && string.IsNullOrWhiteSpace(notes))
        {
            throw new DomainException("High-risk assessments (score > 15) require mitigation notes");
        }

        var assessment = new RiskAssessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LikelihoodBefore = likelihoodBefore,
            SeverityBefore = severityBefore,
            ...
        };

        assessment.DataHash = assessment.ComputeHash();
        return assessment;
    }

    private static void ValidateLikelihood(int value)
    {
        if (value < 1 || value > 5)
            throw new DomainException($"Likelihood must be between 1 and 5 (got {value})");
    }

    private static void ValidateSeverity(int value)
    {
        if (value < 1 || value > 5)
            throw new DomainException($"Severity must be between 1 and 5 (got {value})");
    }
}
```

**FluentValidation delegates to domain:**
```csharp
public class CreateRiskAssessmentValidator : AbstractValidator<CreateRiskAssessmentRequest>
{
    public CreateRiskAssessmentValidator()
    {
        // ✅ Basic format validation only (application layer)
        RuleFor(x => x.LikelihoodBefore).NotEmpty();
        RuleFor(x => x.SeverityBefore).NotEmpty();

        // ✅ Business rules in domain model (will throw DomainException)
    }
}
```

**Recommendation:** **SHOULD ADD** in Week 1 (critical business rules belong in domain)

---

### M4: No Testing Strategy Documented

**Location:** Architecture docs
**Issue:** No test pyramid, coverage targets, or test templates

**Required documentation:**
```markdown
## Testing Strategy

### Test Pyramid

1. **Unit Tests (60%)** - Domain logic, business rules
   - RiskAssessment factory method validation
   - Achievement unlock logic
   - Risk level calculation

2. **Integration Tests (30%)** - API endpoints, DB queries
   - POST /api/ehs/risk-assessments → DB insert + RLS policy
   - GET /api/ehs/risk-assessments/:id/history → pagination
   - Testcontainers PostgreSQL

3. **E2E Tests (10%)** - Critical user flows
   - Create risk assessment → View in history chart
   - XSS attack blocked in catalog filter
   - IDOR attack blocked (org isolation)

### Coverage Targets

- Domain layer: 90%+
- Application layer: 80%+
- Infrastructure layer: 60%+ (focus on critical paths)

### Test Templates

**Unit Test Template:**
```csharp
[Fact]
public void Create_HighRiskWithoutNotes_ThrowsDomainException()
{
    // Arrange
    var likelihood = 5;
    var severity = 5;  // score = 25 > 15

    // Act & Assert
    var exception = Assert.Throws<DomainException>(() =>
        RiskAssessment.Create(
            organizationId: 1,
            likelihoodBefore: likelihood,
            severityBefore: severity,
            category: "machinery",
            createdBy: "user123",
            notes: null  // ⚠️ Missing notes for high-risk
        )
    );

    Assert.Contains("require mitigation notes", exception.Message);
}
```

**Integration Test Template (Testcontainers):**
```csharp
public class RiskAssessmentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateRiskAssessment_ValidRequest_Returns201()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new
        {
            likelihoodBefore = 3,
            severityBefore = 4,
            category = "machinery",
            notes = "Test assessment"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ehs/risk-assessments", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RiskAssessmentResponse>();
        Assert.Equal(12, result.RiskScoreBefore);  // 3 × 4
    }
}
```
```

**Recommendation:** **SHOULD DOCUMENT** in Week 1, implement Week 2

---

## 🟢 LOW Severity Findings

### L1: Code Duplication in Validation Logic

**Location:** Domain model + FluentValidation
**Issue:** Range validation (1-5) duplicated

**Current:**
```csharp
// Domain
private static void ValidateLikelihood(int value)
{
    if (value < 1 || value > 5) throw ...;
}

// FluentValidation
RuleFor(x => x.LikelihoodBefore).InclusiveBetween(1, 5);
```

**Recommended fix:**
```csharp
// Domain/ValueObjects/RiskRating.cs (Value Object pattern)

public class RiskRating : IComparable<RiskRating>
{
    public int Value { get; private set; }

    private RiskRating(int value)
    {
        if (value < 1 || value > 5)
            throw new DomainException($"Risk rating must be between 1 and 5 (got {value})");

        Value = value;
    }

    public static RiskRating FromInt(int value) => new RiskRating(value);

    public static implicit operator int(RiskRating rating) => rating.Value;
}

// Usage
public class RiskAssessment
{
    public RiskRating LikelihoodBefore { get; private set; }
    public RiskRating SeverityBefore { get; private set; }

    public static RiskAssessment Create(int likelihoodBefore, ...)
    {
        return new RiskAssessment
        {
            LikelihoodBefore = RiskRating.FromInt(likelihoodBefore),  // ✅ Validation in one place
            ...
        };
    }
}
```

**Recommendation:** **DEFER** to refactoring sprint (not blocking)

---

### L2: N+1 Query Risk (if navigation properties added)

**Location:** Future `Assessment` parent entity
**Issue:** If lazy loading is enabled, N+1 queries possible

**Scenario:**
```csharp
// If Assessment has navigation property
public class Assessment
{
    public virtual ICollection<RiskAssessment> RiskAssessments { get; set; }
}

// This would trigger N+1
var assessments = await _dbContext.Assessments.ToListAsync();

foreach (var assessment in assessments)
{
    // ⚠️ Each access loads RiskAssessments (N queries)
    Console.WriteLine(assessment.RiskAssessments.Count);
}
```

**Recommended fix:**
```csharp
// Eager loading with Include
var assessments = await _dbContext.Assessments
    .Include(a => a.RiskAssessments)
    .ToListAsync();
```

**Recommendation:** **MONITOR** - add logging to detect N+1 queries in development

---

## Critical Fixes from v3 Security Review

### ✅ Fix C1: RLS Policy Bypass

**Implementation checklist:**
- [ ] Remove `organizationId` from `CreateRiskAssessmentRequest` DTO
- [ ] Add `ICurrentUserService` interface (extract org ID from JWT)
- [ ] Implement `TenantIsolationInterceptor` (set GUC parameter)
- [ ] Write integration test: Org A cannot create assessment with Org B's ID
- [ ] Write integration test: Org A cannot read Org B's assessments

**Code snippet:**
```csharp
// Application/Common/Interfaces/ICurrentUserService.cs

public interface ICurrentUserService
{
    int OrganizationId { get; }
    string UserId { get; }
    bool IsAuthenticated { get; }
}

// Infrastructure/Identity/CurrentUserService.cs

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContext;

    public int OrganizationId =>
        int.Parse(_httpContext.HttpContext?.User?.FindFirstValue("organization_id") ?? "0");

    public string UserId =>
        _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}

// Application/Handlers/CreateRiskAssessmentHandler.cs

public async Task<RiskAssessmentResponse> Handle(...)
{
    var organizationId = _currentUser.OrganizationId;  // ✅ From JWT only

    var assessment = RiskAssessment.Create(
        organizationId: organizationId,  // ✅ Server-controlled
        ...
    );

    await _dbContext.AddAsync(assessment);
    await _dbContext.SaveChangesAsync();

    return mapper.Map(assessment);
}
```

---

### ✅ Fix C2: Mass Assignment Vulnerability

**Implementation checklist:**
- [ ] Remove audit fields from `CreateRiskAssessmentRequest` DTO
- [ ] Verify factory method sets `CreatedAt = DateTime.UtcNow`
- [ ] Verify factory method gets `CreatedBy` from `ICurrentUserService`
- [ ] Write unit test: Factory method generates hash automatically
- [ ] Write integration test: Client cannot override `created_at` or `data_hash`

**Code snippet:**
```csharp
// Contracts/Requests/CreateRiskAssessmentRequest.cs

public class CreateRiskAssessmentRequest
{
    // ✅ Only business fields (NO AUDIT FIELDS)
    public int LikelihoodBefore { get; set; }
    public int SeverityBefore { get; set; }
    public int? LikelihoodAfter { get; set; }
    public int? SeverityAfter { get; set; }
    public string Category { get; set; }
    public string? Notes { get; set; }

    // ❌ REMOVED:
    // public DateTime CreatedAt { get; set; }
    // public string CreatedBy { get; set; }
    // public int OrganizationId { get; set; }
    // public string DataHash { get; set; }
}
```

---

## v4 Review Status: ✅ APPROVED WITH FIXES

**Summary:**
The backend architecture is **SOLID** with Clean Architecture + CQRS patterns. **3 HIGH severity** issues require fixes:

1. **H1:** Add pagination to history endpoint (Week 1 blocker)
2. **H2:** Prevent validation schema drift (Week 2 - OpenAPI generation)
3. **H3:** Standardize error responses (RFC 7807 Problem Details)

Additionally, **2 CRITICAL security fixes** from v3 must be implemented before deployment.

**Next step:** Break down architecture into **1-2h implementation tasks** for Backend and Frontend terminals.

---

**END OF v4 BACKEND REVIEW**
