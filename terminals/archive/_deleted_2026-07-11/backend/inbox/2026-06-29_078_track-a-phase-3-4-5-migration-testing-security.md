---
id: MSG-BACKEND-078
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-030-PARTIAL
created: 2026-06-29
processed: 2026-06-29
result: PARTIAL
outbox: MSG-BACKEND-084
completion: 60% (Phase 3 code + Phase 4 unit tests done, Phase 5 + integration tests remain)
content_hash: fca4b4edb8f334654e3256806bb3171916e09647e3bf6223f3b08679ae6d2f16
---

# Q3 Track A Phase 3-4-5: Migration, Testing, Security

## Összefoglaló

Az MSG-BACKEND-030 Track A Quote Request API **40% kész** — Phase 1-2 (API, Application, Domain, Infrastructure) implementálva.

**Ez a feladat:** Phase 3-4-5 befejezése (persistence, testing, security) → 100% production-ready.

---

## Kontextus

### Conductor Döntés

✅ **ACCEPT MSG-BACKEND-030 PARTIAL DONE**

Indoklás:
- Phase 1-2 solid foundation (0 build errors)
- Javaslat: Accept PARTIAL + create continuation (ez az üzenet)
- Frontend Track A KÉSZ (MSG-FRONTEND-061 100%, spec-konform)
- Backend Phase 3-4-5 szükséges Frontend integrációhoz

### Fájlok Referenciák

**Korábbi munka (MSG-BACKEND-030):**
- `SpaceOS.Modules.Cutting.Api/Endpoints/QuoteRequestEndpoints.cs` — POST endpoint kész
- `SpaceOS.Modules.Cutting.Application/Commands/CreatePublicQuoteRequest/` — CQRS ready
- `SpaceOS.Modules.Cutting.Domain/Entities/PublicQuoteRequest.cs` — Entity kész
- `SpaceOS.Modules.Cutting.Infrastructure/Persistence/` — DbContext + configuration kész

**TODO szinteken:**
- Persistence uncomment: `CreatePublicQuoteRequestCommandHandler.cs` line 47-48
- Migration létrehozása: `dotnet ef migrations add AddPublicQuoteRequestTable`

---

## Feladat: Phase 3-4-5

### Phase 3: Persistence (1 óra) ✅ REQUIRED

**1. EF Core Migration**
```bash
# Working directory: backend/spaceos-modules-cutting/
dotnet ef migrations add AddPublicQuoteRequestTable \
  -p src/SpaceOS.Modules.Cutting.Infrastructure \
  -s src/SpaceOS.Modules.Cutting.Api
```

**2. Handler Persistence Uncomment**
- Fájl: `src/SpaceOS.Modules.Cutting.Application/Commands/CreatePublicQuoteRequest/CreatePublicQuoteRequestCommandHandler.cs`
- Sor 47-48: Uncomment az `await _quoteRepository.AddAsync(quoteRequest);` és commit-et
- Vagy: `await _context.PublicQuoteRequests.AddAsync(quoteRequest); await _context.SaveChangesAsync();`

**3. Schema Verification**
```bash
# PostgreSQL kapcsolat
psql -d spaceos_dev -c "\d spaceos_cutting.public_quote_requests"
psql -d spaceos_dev -c "\d+ spaceos_cutting.public_quote_requests"
```

**Definition of Done (Phase 3):**
- ✅ Migration létrejött
- ✅ Schema helyesen létrehozva
- ✅ Indexes létrehozva (customer_email, created_at DESC)
- ✅ Handler persistence bekapcsolva (nem commented out)
- ✅ `dotnet build` PASS

---

### Phase 4: Testing (2 óra) ✅ REQUIRED

**Unit Tesztek (Minimum 85% coverage)**

**Fájl:** `src/SpaceOS.Modules.Cutting.Application.Tests/Commands/CreatePublicQuoteRequestCommandHandlerTests.cs`

**Test cases:**
1. ✅ Happy path — valid request → 201 response
   - Input: teljes quote request (name, email, phone, material, dimensions, quantity)
   - Expected: CreatePublicQuoteRequestResponse (quoteId, estimatedReplyTime, createdAt)
   - Verify: DB persistence

2. ✅ Validation failures
   - Invalid email format → 400 Bad Request
   - Dimensions out of range (length < 1 mm vagy > 10000 mm) → 400
   - Quantity ≤ 0 → 400
   - Missing required fields → 400

3. ✅ Urgency calculation
   - Standard urgency (default) → estimatedReplyTime = 2 business days
   - Express urgency → estimatedReplyTime = 1 business day
   - Verify: response.estimatedReplyTime contains correct date

4. ✅ Timestamp generation
   - CreatedAt ≈ now (±1 second)
   - UpdatedAt == CreatedAt

**Integration Tesztek**

**Fájl:** `src/SpaceOS.Modules.Cutting.Api.Tests/Endpoints/PublicQuoteRequestEndpointTests.cs`

**Test cases:**
1. ✅ POST `/api/public/cutting/quote-request` — 201 Created
   - Request body: valid PublicQuoteRequestDto
   - Response: 201 + quoteId + Location header
   - DB verify: record created

2. ✅ POST with file attachment — 201 Created
   - Request: FormData with file (base64 or multipart)
   - Response: 201 + quoteId
   - Verify: File stored (filesystem vagy DB)

3. ✅ Validation failures
   - 400 Bad Request (email format, dimensions, quantity)
   - Response: error details

4. ✅ Concurrent requests
   - Multiple requests samtidigt → GUID-ek unique (no collisions)

5. ✅ Response structure validation
   - Fields: quoteId, estimatedReplyTime, createdAt, trackingToken
   - Types: string, DateTime, etc.

**Coverage Requirement:**
- Command handler: ≥90%
- Endpoint: ≥85%
- Overall: ≥85%

**Definition of Done (Phase 4):**
- ✅ Összes unit teszt passa
- ✅ Összes integration teszt passa
- ✅ Code coverage ≥85%
- ✅ No test flakiness

---

### Phase 5: Security & Features (1 óra) ✅ REQUIRED

**1. Rate Limiting**

**Spec:** Max 50 requests/hour per IP

**Implementation:**
```csharp
// Program.cs vagy startup
services.AddRateLimiter(options => {
  options.AddFixedWindowLimiter("PublicCuttingLimiter", limiterOptions => {
    limiterOptions.PermitLimit = 50;
    limiterOptions.Window = TimeSpan.FromHours(1);
    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    limiterOptions.QueueLimit = 2;
  });
});

// Endpoint
[HttpPost("/api/public/cutting/quote-request")]
[Authorize(Policy = "PublicCuttingLimiter")]  // Or: MapToApiVersion + rate limiter middleware
public async Task<IActionResult> CreatePublicQuoteRequest(...)
```

**Response na limit:**
- 429 Too Many Requests
- Retry-After: seconds

**2. File Attachment Handling**

**Spec:** Optional file upload (cutting list/drawing)

**Implementation:**
- Decode base64 vagy multipart upload
- Validation:
  - File size: max 5 MB per attachment
  - File types: whitelist .pdf, .jpg, .png, .dxf
  - MIME type check
- Storage:
  - Filesystem: `/var/spaceos/cutting-uploads/<quoteId>/`
  - Blob (Azure, S3): Configure later
- Security:
  - Sanitize filename
  - Virus scan (ClamAV) — optional for Phase 5

**3. Audit Logging**

**Spec:** Log minden public quote request

**Data to log:**
- Customer email
- Material requested
- Dimensions
- IP address (from HttpContext.Connection.RemoteIpAddress)
- Timestamp
- User agent (optional)

**Storage:**
- Option A: LocalOutbox table `spaceos_cutting.public_quote_request_audit`
- Option B: Separate audit service (if you have one)

**Example:**
```csharp
var auditEntry = new PublicQuoteRequestAuditLog
{
  QuoteId = quoteRequest.Id,
  CustomerEmail = dto.Email,
  IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
  CreatedAt = DateTime.UtcNow,
};
await _auditLogger.LogAsync(auditEntry);
```

**4. CORS Configuration**

**Spec:** Frontend domain allowed

**Implementation:**
```csharp
// Program.cs
services.AddCors(options => {
  options.AddPolicy("PublicCutting", builder => {
    builder.WithOrigins("http://localhost:3000", "https://datahaven.joinerytech.hu")
      .AllowAnyMethod()
      .AllowAnyHeader();
  });
});

// Endpoint
[EnableCors("PublicCutting")]
[HttpPost("/api/public/cutting/quote-request")]
public async Task<IActionResult> CreatePublicQuoteRequest(...)
```

---

## Definition of Done — Phase 3-4-5

| Kritérium | Felelős |
|---|---|
| ✅ Phase 3: Migration, schema, persistence | Backend |
| ✅ Phase 4: Unit + integration tests (85%+ coverage) | Backend |
| ✅ Phase 5: Rate limiting, file handling, audit, CORS | Backend |
| ✅ `dotnet build` PASS (0 errors) | Backend |
| ✅ All tests GREEN | Backend |
| ✅ Frontend integration ready (Backend APIs exist) | Backend |

---

## Dependencies & Blocking

**Frontend Wait:**
- Frontend MSG-061 100% DONE, waiting for Backend Phase 3-4-5 completion
- Once Phase 3-4-5 done → Frontend can test `GET /public/cutting/quotes/track/{trackingToken}` API

**Track B Start:**
- Do NOT start Track B (Pricing Integration) until Phase 3-4-5 done
- Track B depends on Phase 3-4-5 Continuation Task completion

---

## Effort & Timeline

| Phase | Effort | Crítico |
|---|---|---|
| Phase 3: Persistence | 1 óra | ⚠️ Crítico (data must persist) |
| Phase 4: Testing | 2 óra | ✅ High |
| Phase 5: Security | 1 óra | ✅ High |
| **Total** | **~4 óra** | |

**Expected completion:** 2026-06-30 (next day) or same day if focused

---

## Success Criteria

1. ✅ Phase 3: Schema created, migration applied, persistence ON
2. ✅ Phase 4: All tests passing (85%+ coverage)
3. ✅ Phase 5: Rate limiting, audit logging, CORS configured
4. ✅ Build: `dotnet build` — 0 errors, 0 warnings (related to this change)
5. ✅ Ready for: Frontend integration testing (MSG-FRONTEND-061 consumption)

---

## Files to Modify/Create

**Existing (modify):**
- `CreatePublicQuoteRequestCommandHandler.cs` — uncomment line 47-48

**New migrations:**
- `Infrastructure/Migrations/AddPublicQuoteRequestTable.cs` (auto-generated)

**New tests:**
- `CreatePublicQuoteRequestCommandHandlerTests.cs` (unit)
- `PublicQuoteRequestEndpointTests.cs` (integration)

**New audit/security:**
- `Infrastructure/Persistence/Configurations/PublicQuoteRequestAuditLogConfiguration.cs` (optional)

---

## Next Steps (Conductor)

1. ✅ Backend processes this task (Phase 3-4-5 implementation)
2. Once DONE → Frontend can integrate
3. Track B dispatch (Pricing Integration) after Phase 3-4-5

---

**Conductor**
2026-06-29 — Q3 Track A Continuation (Phase 3-4-5)
