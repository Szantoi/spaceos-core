# SpaceOS Catalog + EHS Hybrid Architecture (v3 SECURITY REVIEW)

**Status:** REVIEW
**Created:** 2026-06-23
**Reviewer:** Architect (Senior Security Engineer perspective)
**Base Documents:** v1 + v2 DB Review
**Framework:** OWASP Top 10 (2021) + SpaceOS Security Patterns

---

## Review Summary

This document performs a security audit of the Catalog + EHS Hybrid architecture using:
- **OWASP Top 10 (2021)** threat model
- **SpaceOS Golden Rules** (#4 Need-to-Know RBAC, #3 Immutability & Trust)
- **Multi-tenant SaaS security** best practices
- **ISO 45001 audit compliance** requirements

---

## Findings Overview

| Severity | Count | Category |
|---|---|---|
| 🔴 CRITICAL | 2 | Access control, mass assignment |
| 🟠 HIGH | 4 | XSS, IDOR, CSRF, rate limiting |
| 🟡 MEDIUM | 5 | Input validation, error handling, logging |
| 🟢 LOW | 3 | Security headers, dependency scanning |

---

## 🔴 CRITICAL Severity Findings

### C1: Broken Access Control - RLS Policy Bypass Risk (A01:2021)

**OWASP Category:** A01:2021 – Broken Access Control
**Location:** EHS API endpoints + RLS policy
**Attack Vector:** Organization ID manipulation

**Vulnerability:**
The v1 architecture relies on `current_setting('app.current_organization_id')` for RLS policy enforcement. If this setting is NOT properly initialized or can be overridden by the client, **tenant isolation is broken**.

**Attack scenario:**
```http
POST /api/ehs/risk-assessments
Authorization: Bearer <valid-jwt-for-org-123>
Content-Type: application/json

{
  "organizationId": 456,  // ATTACKER TRIES TO WRITE TO DIFFERENT ORG
  "likelihoodBefore": 3,
  "severityBefore": 4,
  ...
}
```

**Current code (VULNERABLE if not fixed):**
```csharp
// RiskAssessment.Create factory method
public static RiskAssessment Create(
    int organizationId,  // ⚠️ Client-provided value!
    ...
) {
    var assessment = new RiskAssessment {
        OrganizationId = organizationId,  // ⚠️ DANGEROUS
        ...
    };
}
```

**Root cause:**
- `organizationId` is accepted from client request body
- No validation that `organizationId` matches JWT claims
- RLS policy only filters reads, not writes

**Required fix:**
```csharp
// EHS.Application/Commands/CreateRiskAssessmentHandler.cs

public class CreateRiskAssessmentHandler : IRequestHandler<CreateRiskAssessmentCommand, RiskAssessmentResponse>
{
    private readonly ICurrentUserService _currentUser;

    public async Task<RiskAssessmentResponse> Handle(CreateRiskAssessmentCommand request, ...)
    {
        // ✅ FIX: Get org ID from JWT claims, NEVER from client request
        var organizationId = _currentUser.OrganizationId;

        // ✅ Reject if request tries to override
        if (request.OrganizationId.HasValue && request.OrganizationId != organizationId)
        {
            throw new ForbiddenAccessException("Cannot create assessment for different organization");
        }

        var assessment = RiskAssessment.Create(
            organizationId: organizationId,  // ✅ Server-side value only
            likelihoodBefore: request.LikelihoodBefore,
            ...
        );

        // ✅ Set GUC parameter BEFORE query execution (DbConnectionInterceptor)
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL app.current_organization_id = {0}", organizationId
        );

        await _dbContext.RiskAssessments.AddAsync(assessment);
        await _dbContext.SaveChangesAsync();

        return mapper.Map(assessment);
    }
}
```

**Additional safeguards:**
```csharp
// Infrastructure/Persistence/DbConnectionInterceptor.cs

public class TenantIsolationInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, ...)
    {
        // ✅ Set GUC parameter on EVERY query
        var orgId = _currentUser.OrganizationId;
        command.CommandText = $"SET LOCAL app.current_organization_id = {orgId}; {command.CommandText}";

        return await base.ReaderExecutingAsync(command, commandEventData, result);
    }
}
```

**Validation checklist:**
- [ ] Remove `organizationId` from API request DTO
- [ ] Get org ID from JWT claims only (`ICurrentUserService`)
- [ ] Set GUC parameter in `DbConnectionInterceptor`
- [ ] Write integration test: Org A cannot create assessment for Org B
- [ ] Write integration test: Org A cannot read Org B's assessments

**Impact if not fixed:**
- ❌ **CRITICAL DATA BREACH** - any authenticated user can read/write other tenants' data
- ❌ **ISO 45001 compliance violation** - audit trail contaminated
- ❌ **Legal liability** - GDPR violation (unauthorized data access)

**Recommendation:** **MUST FIX IMMEDIATELY** before any deployment

---

### C2: Mass Assignment Vulnerability - Audit Trail Tampering (A04:2021)

**OWASP Category:** A04:2021 – Insecure Design
**Location:** EHS API `CreateRiskAssessmentRequest` DTO
**Attack Vector:** Client sends extra fields to override server-side values

**Vulnerability:**
If the API request DTO includes audit fields (`created_at`, `created_by`, `data_hash`), a malicious client can tamper with immutable audit data.

**Attack scenario:**
```http
POST /api/ehs/risk-assessments
Content-Type: application/json

{
  "likelihoodBefore": 3,
  "severityBefore": 4,
  "createdAt": "2025-01-01T00:00:00Z",  // ⚠️ BACKDATED TIMESTAMP
  "createdBy": "admin@example.com",      // ⚠️ IMPERSONATION
  "dataHash": "fakehash123"               // ⚠️ INTEGRITY BYPASS
}
```

**Current code (VULNERABLE if auto-mapping is used):**
```csharp
// ⚠️ DANGEROUS: Auto-mapping from request to entity
var assessment = _mapper.Map<RiskAssessment>(request);
await _dbContext.AddAsync(assessment);
```

**Root cause:**
- Request DTO includes audit fields
- Auto-mapping copies all fields from request to entity
- No explicit whitelist of allowed fields

**Required fix:**
```csharp
// Contracts/Requests/CreateRiskAssessmentRequest.cs

public class CreateRiskAssessmentRequest
{
    // ✅ ONLY business fields, NO audit fields
    public int LikelihoodBefore { get; set; }
    public int SeverityBefore { get; set; }
    public int? LikelihoodAfter { get; set; }
    public int? SeverityAfter { get; set; }
    public string Category { get; set; }
    public string? Notes { get; set; }

    // ❌ REMOVED: public DateTime CreatedAt { get; set; }
    // ❌ REMOVED: public string CreatedBy { get; set; }
    // ❌ REMOVED: public int OrganizationId { get; set; }
    // ❌ REMOVED: public string DataHash { get; set; }
}

// Application/Handlers/CreateRiskAssessmentHandler.cs

public async Task<RiskAssessmentResponse> Handle(...)
{
    var organizationId = _currentUser.OrganizationId;  // ✅ Server-side
    var createdBy = _currentUser.UserId;               // ✅ Server-side

    var assessment = RiskAssessment.Create(
        organizationId: organizationId,
        likelihoodBefore: request.LikelihoodBefore,
        severityBefore: request.SeverityBefore,
        likelihoodAfter: request.LikelihoodAfter,
        severityAfter: request.SeverityAfter,
        category: request.Category,
        createdBy: createdBy,  // ✅ From JWT, NOT client
        notes: request.Notes
    );
    // ✅ CreatedAt + DataHash set in factory method

    await _dbContext.AddAsync(assessment);
}
```

**Validation checklist:**
- [ ] Remove audit fields from request DTO
- [ ] Use explicit field mapping (NOT AutoMapper for audit fields)
- [ ] Write unit test: Verify factory method sets `CreatedAt = DateTime.UtcNow`
- [ ] Write integration test: Client cannot override `created_by` or `data_hash`

**Impact if not fixed:**
- ❌ **Audit trail tampering** - forged timestamps, impersonation
- ❌ **Data integrity violation** - fake hashes bypass verification
- ❌ **ISO 45001 non-compliance** - audit records unreliable

**Recommendation:** **MUST FIX** before v1 deployment

---

## 🟠 HIGH Severity Findings

### H1: Cross-Site Scripting (XSS) in Catalog Filter (A03:2021)

**OWASP Category:** A03:2021 – Injection
**Location:** Catalog filter components (search bar, URL sync, localStorage)
**Attack Vector:** Stored XSS via malicious search query

**Vulnerability:**
User input from search bar is stored in `localStorage` and URL parameters without sanitization. If rendered in DOM without escaping, XSS is possible.

**Attack scenario:**
```javascript
// Attacker shares malicious filter link
https://portal.spaceos.com/catalog?search=<img src=x onerror=alert(document.cookie)>

// OR types in search bar
setFilter('search', '<script>steal_session()</script>');

// Vulnerable code (if search value is rendered as innerHTML)
<div>{catalogFilters.search}</div>  // ⚠️ XSS if not escaped
```

**Required fix:**
```jsx
// ✅ Use React's built-in XSS protection (text content only)
<div>{catalogFilters.search}</div>  // ✅ SAFE (React escapes automatically)

// ✅ Sanitize URL parameters on load
const loadFiltersFromURL = () => {
  const params = new URLSearchParams(window.location.search);

  const filters = {
    search: DOMPurify.sanitize(params.get('search') || ''),  // ✅ Sanitize
    category: params.get('cat')?.split(',').map(c => DOMPurify.sanitize(c)) || [],
    ...
  };

  set({ catalogFilters: filters });
};

// ✅ Validate on setFilter
const setFilter = (key, value) => {
  if (key === 'search' && typeof value === 'string') {
    value = value.replace(/<[^>]*>/g, '');  // ✅ Strip HTML tags
  }

  set(state => ({
    catalogFilters: { ...state.catalogFilters, [key]: value }
  }));
};
```

**Additional safeguards:**
```jsx
// Install DOMPurify for robust sanitization
npm install dompurify

// utils/sanitize.js
import DOMPurify from 'dompurify';

export const sanitizeInput = (input) => {
  return DOMPurify.sanitize(input, {
    ALLOWED_TAGS: [],  // Strip ALL HTML tags
    ALLOWED_ATTR: []
  });
};
```

**Validation checklist:**
- [ ] Audit all `catalogFilters` render points (no `dangerouslySetInnerHTML`)
- [ ] Sanitize URL parameters on `loadFiltersFromURL`
- [ ] Strip HTML tags in `setFilter` before localStorage write
- [ ] Add Content Security Policy (CSP) header: `script-src 'self'`
- [ ] Write E2E test: XSS payloads are escaped in search results

**Impact if not fixed:**
- ❌ Session hijacking (steal JWT from cookie)
- ❌ Keylogger injection
- ❌ Phishing attack (fake login form)

**Recommendation:** **MUST FIX** before Week 1 deployment

---

### H2: Insecure Direct Object Reference (IDOR) on Assessment ID (A01:2021)

**OWASP Category:** A01:2021 – Broken Access Control
**Location:** `GET /api/ehs/risk-assessments/:assessmentId/history`
**Attack Vector:** Assessment ID enumeration

**Vulnerability:**
The `assessmentId` is a sequential integer (SERIAL PRIMARY KEY). An attacker can enumerate all assessments by incrementing the ID:

```http
GET /api/ehs/risk-assessments/1/history
GET /api/ehs/risk-assessments/2/history
GET /api/ehs/risk-assessments/3/history
...
```

If RLS policy is not correctly enforced, or if the endpoint doesn't validate ownership, **cross-tenant data access** is possible.

**Attack scenario:**
```http
// Attacker (Org 123) tries to access Org 456's assessment
GET /api/ehs/risk-assessments/9999/history
Authorization: Bearer <valid-jwt-for-org-123>

// If RLS policy is bypassed or endpoint doesn't check ownership:
// ❌ Response: { items: [ ... Org 456 data ... ] }
```

**Required fix:**
```csharp
// EHS.Application/Queries/GetRiskAssessmentHistoryHandler.cs

public async Task<RiskAssessmentHistoryResponse> Handle(GetRiskAssessmentHistoryQuery query, ...)
{
    var organizationId = _currentUser.OrganizationId;

    // ✅ Verify assessment belongs to current org
    var assessment = await _dbContext.Assessments
        .Where(a => a.Id == query.AssessmentId)
        .Where(a => a.OrganizationId == organizationId)  // ✅ Authorization check
        .FirstOrDefaultAsync();

    if (assessment == null)
    {
        throw new NotFoundException($"Assessment {query.AssessmentId} not found");
        // ✅ Same error message for non-existent and unauthorized (prevent enumeration)
    }

    var history = await _dbContext.RiskAssessments
        .Where(r => r.AssessmentId == query.AssessmentId)
        .Where(r => r.DeletedAt == null)  // ✅ Soft delete filter
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    return mapper.Map(history);
}
```

**Additional hardening (UUID instead of SERIAL):**
```sql
-- Use UUID for assessments table to prevent enumeration
CREATE TABLE ehs.assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),  -- ✅ Non-enumerable
    ...
);
```

**Validation checklist:**
- [ ] Every `assessmentId` endpoint checks `organizationId` match
- [ ] Write integration test: Org A cannot access Org B's assessment history
- [ ] Write integration test: Non-existent ID returns 404 (not 403 to prevent enumeration)
- [ ] Consider UUID for `assessments` table in v2

**Impact if not fixed:**
- ❌ Cross-tenant data leakage
- ❌ Enumeration of all assessments (business intelligence theft)

**Recommendation:** **MUST FIX** before v1 deployment

---

### H3: Missing CSRF Protection on POST Endpoints (A05:2021)

**OWASP Category:** A05:2021 – Security Misconfiguration
**Location:** `POST /api/ehs/risk-assessments`
**Attack Vector:** Cross-Site Request Forgery

**Vulnerability:**
If the API accepts cookies for authentication (instead of Authorization header), a malicious site can forge requests:

**Attack scenario:**
```html
<!-- Attacker's website -->
<form action="https://portal.spaceos.com/api/ehs/risk-assessments" method="POST">
  <input name="likelihoodBefore" value="5">
  <input name="severityBefore" value="5">
  <input name="category" value="machinery">
</form>
<script>document.forms[0].submit();</script>
```

If user is logged in to SpaceOS and visits attacker's site, the form auto-submits with user's session cookie.

**Required fix (Option 1 - SameSite cookies):**
```csharp
// Startup.cs or Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // ✅ Accept JWT from Authorization header ONLY (not cookies)
                var token = context.Request.Headers["Authorization"]
                    .FirstOrDefault()?.Split(" ").Last();

                context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;  // ✅ CSRF protection
    options.Cookie.HttpOnly = true;                 // ✅ XSS protection
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // ✅ HTTPS only
});
```

**Required fix (Option 2 - CSRF token):**
```csharp
// Add CSRF middleware
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// In controller
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateRiskAssessment([FromBody] CreateRiskAssessmentRequest request)
{
    ...
}
```

**Validation checklist:**
- [ ] Verify JWT is sent via `Authorization: Bearer` header (NOT cookies)
- [ ] Set `SameSite=Strict` on any session cookies
- [ ] Add `[ValidateAntiForgeryToken]` if using cookie auth
- [ ] Write E2E test: CSRF attack is blocked

**Impact if not fixed:**
- ❌ Attacker can create/modify assessments on behalf of victim
- ❌ Audit trail shows legitimate user (hard to detect)

**Recommendation:** **SHOULD FIX** in Week 1

---

### H4: Missing Rate Limiting - API Abuse / DoS (A05:2021)

**OWASP Category:** A05:2021 – Security Misconfiguration
**Location:** All EHS API endpoints
**Attack Vector:** Brute-force, denial of service

**Vulnerability:**
No rate limiting on API endpoints allows attackers to:
- Brute-force assessment IDs (enumerate all assessments)
- Create thousands of fake assessments (storage DoS)
- Overwhelm database with queries (performance DoS)

**Attack scenario:**
```bash
# Attacker creates 10,000 fake assessments in 1 minute
for i in {1..10000}; do
  curl -X POST https://portal.spaceos.com/api/ehs/risk-assessments \
    -H "Authorization: Bearer $TOKEN" \
    -d '{"likelihoodBefore":1,"severityBefore":1,"category":"other"}' &
done
```

**Required fix:**
```csharp
// Install AspNetCoreRateLimit
dotnet add package AspNetCoreRateLimit

// Startup.cs
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddInMemoryRateLimiting();

// appsettings.json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/ehs/*",
        "Period": "1m",
        "Limit": 10  // Max 10 POST requests per minute per IP
      },
      {
        "Endpoint": "GET:/api/ehs/*",
        "Period": "1m",
        "Limit": 100  // Max 100 GET requests per minute per IP
      }
    ]
  }
}

// Program.cs
app.UseIpRateLimiting();
```

**Validation checklist:**
- [ ] Apply rate limits to all POST endpoints (10/min)
- [ ] Apply rate limits to all GET endpoints (100/min)
- [ ] Return `429 Too Many Requests` with `Retry-After` header
- [ ] Write E2E test: 11th request in 1 minute is blocked

**Impact if not fixed:**
- ❌ Storage DoS (DB fills with fake data)
- ❌ Performance DoS (slow queries for all users)
- ❌ Brute-force enumeration

**Recommendation:** **SHOULD FIX** in Week 1

---

## 🟡 MEDIUM Severity Findings

### M1: SQL Injection Risk in Notes Field (A03:2021)

**OWASP Category:** A03:2021 – Injection
**Location:** `notes` field in `CreateRiskAssessmentRequest`
**Risk:** LOW (EF Core uses parameterized queries by default, but worth validating)

**Attack scenario:**
```http
POST /api/ehs/risk-assessments
{
  "notes": "'; DROP TABLE ehs.risk_assessments; --"
}
```

**Mitigation (already in place if using EF Core):**
```csharp
// EF Core automatically parameterizes queries
var assessment = RiskAssessment.Create(..., notes: request.Notes);
await _dbContext.AddAsync(assessment);  // ✅ SAFE (parameterized query)
```

**Validation:**
- [ ] Verify all DB queries use EF Core (no raw SQL with string interpolation)
- [ ] Audit any `FromSqlRaw` or `ExecuteSqlRaw` calls
- [ ] Write unit test: SQL injection payloads are escaped

**Recommendation:** **VERIFY** - should be safe with EF Core

---

### M2: Information Disclosure in Error Messages (A05:2021)

**OWASP Category:** A05:2021 – Security Misconfiguration
**Location:** Global exception handler
**Risk:** MEDIUM

**Vulnerability:**
Default .NET error messages expose DB schema, stack traces, file paths:

```json
{
  "error": "An error occurred while saving the entity changes. See the inner exception for details.",
  "innerException": "The INSERT statement conflicted with the FOREIGN KEY constraint \"fk_organization\"..."
}
```

**Required fix:**
```csharp
// Middleware/GlobalExceptionHandler.cs

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // ✅ Log full details server-side
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var statusCode = exception switch
        {
            NotFoundException => 404,
            ForbiddenAccessException => 403,
            ValidationException => 400,
            _ => 500
        };

        var response = new ErrorResponse
        {
            Message = statusCode == 500
                ? "An internal server error occurred"  // ✅ Generic message for 500
                : exception.Message,

            // ✅ Only include stack trace in Development
            Details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
```

**Recommendation:** **SHOULD FIX** in Week 1

---

### M3: Insufficient Security Logging (A09:2021)

**OWASP Category:** A09:2021 – Security Logging and Monitoring Failures
**Location:** EHS API handlers
**Risk:** MEDIUM

**Missing logs:**
- Failed authorization attempts (org ID mismatch)
- IDOR attempts (accessing other org's assessments)
- Rate limit violations
- Suspicious data patterns (10+ assessments in 1 minute)

**Required fix:**
```csharp
// Application/Handlers/CreateRiskAssessmentHandler.cs

public async Task<RiskAssessmentResponse> Handle(...)
{
    var organizationId = _currentUser.OrganizationId;

    // ✅ Log security event if org ID mismatch attempted
    if (request.OrganizationId.HasValue && request.OrganizationId != organizationId)
    {
        _logger.LogWarning(
            "SECURITY: User {UserId} attempted to create assessment for different org {AttemptedOrgId} (actual org {ActualOrgId})",
            _currentUser.UserId,
            request.OrganizationId,
            organizationId
        );

        throw new ForbiddenAccessException();
    }

    var assessment = RiskAssessment.Create(...);
    await _dbContext.AddAsync(assessment);

    // ✅ Log successful creation
    _logger.LogInformation(
        "Risk assessment {AssessmentId} created by user {UserId} for org {OrgId}",
        assessment.Id,
        _currentUser.UserId,
        organizationId
    );

    return mapper.Map(assessment);
}
```

**Recommendation:** **SHOULD FIX** in Week 1

---

### M4: Weak Input Validation on Voice Search Transcript (A03:2021)

**OWASP Category:** A03:2021 – Injection
**Location:** `VoiceSearchButton` component
**Risk:** LOW (frontend-only, but worth sanitizing)

**Attack scenario:**
User speaks malicious input that gets transcribed as XSS payload:
```javascript
recognition.onresult = (e) => {
  const transcript = e.results[0][0].transcript;
  // transcript = "<img src=x onerror=alert(1)>"
  setFilter('search', transcript);  // ⚠️ Stored in localStorage + URL
};
```

**Required fix:**
```javascript
recognition.onresult = (e) => {
  const transcript = e.results[0][0].transcript;

  // ✅ Sanitize before setting filter
  const sanitized = transcript.replace(/<[^>]*>/g, '');

  setFilter('search', sanitized);
};
```

**Recommendation:** **SHOULD FIX** in Week 1

---

### M5: Missing Subresource Integrity (SRI) for CDN Assets (A06:2021)

**OWASP Category:** A06:2021 – Vulnerable and Outdated Components
**Location:** Frontend package imports (if using CDN)
**Risk:** LOW

**Vulnerability:**
If loading libraries from CDN without SRI, a compromised CDN can inject malicious code.

**Required fix (if using CDN):**
```html
<script
  src="https://cdn.jsdelivr.net/npm/fuzzysort@2.0.4/fuzzysort.min.js"
  integrity="sha384-..."
  crossorigin="anonymous"
></script>
```

**Recommendation:** **Use npm** instead of CDN (bundled with Vite/Webpack) - no fix needed if all dependencies are npm-installed.

---

## 🟢 LOW Severity Findings

### L1: Missing Security Headers

**Issue:** No Content Security Policy (CSP), X-Frame-Options, etc.

**Required fix:**
```csharp
// Middleware/SecurityHeadersMiddleware.cs

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");

    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    await next();
});
```

**Recommendation:** **SHOULD ADD** in Week 1

---

### L2: No Automated Dependency Scanning

**Issue:** No CI/CD pipeline check for vulnerable npm/NuGet packages

**Required fix:**
```yaml
# .github/workflows/security-scan.yml

name: Security Scan
on: [push, pull_request]

jobs:
  npm-audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - run: npm audit --production

  dotnet-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - run: dotnet list package --vulnerable
```

**Recommendation:** **SHOULD ADD** before production

---

### L3: No Secrets Scanning in Git Commits

**Issue:** Risk of accidentally committing JWT secrets, DB passwords, API keys

**Required fix:**
```bash
# Install pre-commit hook
npm install -D husky

# .husky/pre-commit
#!/bin/sh
npx detect-secrets-launcher --baseline .secrets.baseline
```

**Recommendation:** **SHOULD ADD** before Week 1

---

## Security Checklist (v3 → v1 Required Fixes)

### CRITICAL (Block deployment)
- [ ] **C1:** Remove `organizationId` from request DTO, get from JWT only
- [ ] **C1:** Set GUC parameter (`app.current_organization_id`) in DbConnectionInterceptor
- [ ] **C2:** Remove audit fields from request DTO (created_at, created_by, data_hash)

### HIGH (Required for Week 1)
- [ ] **H1:** Sanitize catalog search input (strip HTML tags)
- [ ] **H1:** Add CSP header: `script-src 'self'`
- [ ] **H2:** Validate `assessmentId` ownership in GET /history endpoint
- [ ] **H3:** Verify JWT is sent via Authorization header (not cookies)
- [ ] **H4:** Add rate limiting: 10 POST/min, 100 GET/min

### MEDIUM (Should fix in Week 1-2)
- [ ] **M1:** Audit all SQL queries (verify EF Core parameterization)
- [ ] **M2:** Generic error messages for 500 errors in Production
- [ ] **M3:** Log security events (authorization failures, IDOR attempts)
- [ ] **M4:** Sanitize voice search transcript
- [ ] **M5:** Use npm packages (not CDN) or add SRI

### LOW (Backlog)
- [ ] **L1:** Add security headers (CSP, X-Frame-Options, etc.)
- [ ] **L2:** Add GitHub Actions for npm audit + dotnet vulnerable packages
- [ ] **L3:** Add pre-commit hook for secrets detection

---

## v3 Review Status: ✅ APPROVED WITH CRITICAL FIXES

**Summary:**
The v1+v2 architecture has **2 CRITICAL security vulnerabilities** that MUST be fixed:

1. **Broken Access Control** - RLS bypass via client-provided `organizationId`
2. **Mass Assignment** - Audit trail tampering via client-provided audit fields

Additionally, **4 HIGH severity** issues should be fixed in Week 1:
- XSS in catalog filter
- IDOR on assessment endpoints
- CSRF protection
- Rate limiting

**Next step:** Proceed to **v4 Backend Review** to validate API design and implementation patterns.

---

**END OF v3 SECURITY REVIEW**
