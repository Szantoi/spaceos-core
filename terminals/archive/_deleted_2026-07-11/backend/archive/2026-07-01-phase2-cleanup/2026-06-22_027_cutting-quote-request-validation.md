---
id: MSG-BACKEND-027
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-CONDUCTOR-006
created: 2026-06-22
content_hash: 968027414570903350d66d1f0ea10fceffd3a91f1cc07eaa241c5f8ae80d7428
---

# Cutting Quote Request Validation — B2B Feature

## Kontextus

**Phase 2 Roadmap:** 2026 Q2-Q3 Szabászat modul + 2. ügyfél onboarding.

A Cutting modul **QuoteRequestEndpoints.cs** fájl alapján a B2B Quote Request feature endpoint-jai **már implementálva vannak**, de a **command/handler/domain logic validálása** hiányzik. Ez a feladat ellenőrzi az end-to-end működést és biztosítja a production-readiness-t.

## Feladat

Validáld a Cutting Quote Request feature-t end-to-end:

### 1. Endpoint Verifikáció

Ellenőrizd a következő endpointokat:

**Public API (unauthenticated):**
- `POST /public/cutting/quote-request` — új quote request létrehozás
- `GET /public/cutting/quotes/track/{trackingToken}` — quote tracking
- `POST /public/cutting/quotes/track/{trackingToken}/accept` — customer accept

**Admin API (authenticated, tenant-scoped):**
- `GET /api/cutting/quotes` — összes quote request listázás
- `PUT /api/cutting/quotes/{quoteId}/approve` — quote jóváhagyás
- `PUT /api/cutting/quotes/{quoteId}/reject` — quote elutasítás

**Ellenőrizd a fájlokat:**
- `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Api/Endpoints/QuoteRequestEndpoints.cs` ✅ (már létezik)

### 2. Command/Handler Implementáció Ellenőrzés

Validáld az alábbi command/handler párosokat:

| Command | Handler | Expected Location |
|---|---|---|
| `CreateQuoteRequestCommand` | `CreateQuoteRequestCommandHandler` | `Application/Commands/CreateQuoteRequest/` |
| `ApproveQuoteCommand` | `ApproveQuoteCommandHandler` | `Application/Commands/ApproveQuote/` |
| `RejectQuoteCommand` | `RejectQuoteCommandHandler` | `Application/Commands/RejectQuote/` |
| `AcceptQuoteCommand` | `AcceptQuoteCommandHandler` | `Application/Commands/AcceptQuote/` |

**Query handlers:**
| Query | Handler | Expected Location |
|---|---|---|
| `GetQuoteRequestsQuery` | `GetQuoteRequestsQueryHandler` | `Application/Queries/GetQuoteRequests/` |
| `TrackQuoteQuery` | `TrackQuoteQueryHandler` | `Application/Queries/TrackQuote/` |

**Ellenőrizd:**
1. Command/handler fájlok léteznek-e
2. MediatR integration működik-e (IRequest<Result<T>>)
3. Validation logic implementálva-e (FluentValidation vagy inline)
4. Error handling (Result<T> pattern)

### 3. Domain Logic Validálás

Ellenőrizd a **QuoteRequest** aggregate root-ot:

**Expected location:**
- `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Domain/Aggregates/QuoteRequest.cs`

**Domain logic checklist:**
- [x] QuoteRequest entity létezik
- [x] FSM states definiálva:
  - `Pending` → `Approved` → `Accepted` / `Completed`
  - `Pending` → `Rejected`
- [x] Value objects (ha vannak): `TrackingToken`, `QuoteDetails`, stb.
- [x] Domain events (opcionális): `QuoteRequestCreated`, `QuoteApproved`, `QuoteRejected`, `QuoteAccepted`
- [x] Business rules (validation):
  - Tracking token unique
  - Quote amount > 0
  - Customer email valid
  - Quote expiry date in future

### 4. Integration Tests

Írj **minimum 3 integration testet minden endpoint-hoz** (összesen 18 test):

**Public API tests:**
1. `CreateQuoteRequest_ValidData_Returns200AndTrackingToken`
2. `CreateQuoteRequest_InvalidEmail_Returns400`
3. `TrackQuote_ValidToken_ReturnsQuoteDetails`
4. `TrackQuote_InvalidToken_Returns404`
5. `AcceptQuote_ValidToken_Returns200`
6. `AcceptQuote_AlreadyAccepted_Returns409`

**Admin API tests:**
7. `GetQuoteRequests_ValidTenant_ReturnsFiltered`
8. `GetQuoteRequests_Unauthenticated_Returns401`
9. `ApproveQuote_ValidQuote_UpdatesStatus`
10. `ApproveQuote_AlreadyApproved_Returns409`
11. `RejectQuote_ValidQuote_UpdatesStatus`
12. `RejectQuote_InvalidQuoteId_Returns404`

**Test location:**
- `backend/spaceos-modules-cutting/tests/SpaceOS.Modules.Cutting.Tests/Api/QuoteRequestEndpointTests.cs`

### 5. Security Review

Ellenőrizd a következő biztonsági követelményeket:

**RBAC:**
- Admin endpoints: `.RequireAuthorization("ManufacturerOnly")` policy
- Public endpoints: `.AllowAnonymous()` megfelelően használva

**Tenant isolation:**
- Admin API: csak saját tenant quote request-jeit látja
- Public API: tracking token based access (tenant-agnostic)

**Input validation:**
- Email format validáció
- Tracking token format (GUID vagy hash)
- Quote amount range (min: 0, max: configurable)
- Expiry date range (min: today, max: +90 days)

**Idempotency:**
- `CreateQuoteRequest`: duplicate email + date → 409 Conflict vagy same tracking token
- `ApproveQuote`: already approved → 409 Conflict
- `AcceptQuote`: already accepted → 409 Conflict

## Definition of Done (DoD)

- [x] 6 command/handler páros létezik és működik
- [x] 2 query/handler páros létezik és működik
- [x] `QuoteRequest` aggregate root implementálva (domain logic, FSM)
- [x] Integration tests: minimum 18 test (3/endpoint × 6 endpoint)
- [x] Összes teszt pass (`dotnet test`)
- [x] Build sikeres (`dotnet build`, `dotnet publish`)
- [x] Security review checklist teljesítve (RBAC, tenant isolation, validation)
- [x] DONE outbox üzenet részletes implementációs reporttal

## Technikai Notes

### Command/Handler Pattern Példa

```csharp
// Command
public sealed record CreateQuoteRequestCommand : IRequest<Result<QuoteRequestCreatedDto>>
{
    public Guid TenantId { get; init; }
    public CreateQuoteRequestDto Data { get; init; }
}

// Handler
public sealed class CreateQuoteRequestCommandHandler
    : IRequestHandler<CreateQuoteRequestCommand, Result<QuoteRequestCreatedDto>>
{
    public async Task<Result<QuoteRequestCreatedDto>> Handle(
        CreateQuoteRequestCommand request,
        CancellationToken ct)
    {
        // Validation
        if (string.IsNullOrEmpty(request.Data.Email))
            return Result<QuoteRequestCreatedDto>.Failure("Email is required");

        // Domain logic
        var trackingToken = TrackingToken.Generate();
        var quoteRequest = QuoteRequest.Create(
            request.TenantId,
            request.Data.Email,
            request.Data.QuoteAmount,
            trackingToken);

        // Persistence
        await _repository.AddAsync(quoteRequest, ct);

        return Result<QuoteRequestCreatedDto>.Success(new QuoteRequestCreatedDto
        {
            QuoteId = quoteRequest.Id,
            TrackingToken = trackingToken.Value
        });
    }
}
```

### FSM Validálás Példa

```csharp
// QuoteRequest aggregate
public Result Approve(Guid approvedBy)
{
    if (_status == QuoteStatus.Approved)
        return Result.Failure("Quote is already approved");

    if (_status == QuoteStatus.Rejected)
        return Result.Failure("Cannot approve rejected quote");

    _status = QuoteStatus.Approved;
    _approvedBy = approvedBy;
    _approvedAt = DateTime.UtcNow;

    AddDomainEvent(new QuoteApprovedDomainEvent(Id, approvedBy));
    return Result.Success();
}
```

## Kapcsolódó Dokumentáció

- **QuoteRequestEndpoints.cs:** `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Api/Endpoints/QuoteRequestEndpoints.cs`
- **Phase 2 Planning:** `terminals/conductor/outbox/2026-06-22_001_phase2-cutting-module-planning.md`
- **Clean Architecture Pattern:** `docs/knowledge/patterns/`

## Ha Command/Handler Hiányzik

**Ha a command/handler fájlok NEM léteznek:**

1. Implementáld őket a fenti pattern szerint
2. Kövesd a Cutting module meglévő mintáit (pl. `AssignBatchCommand`)
3. Használd a `Result<T>` pattern-t minden command handler-ben
4. FluentValidation vagy inline validation (konzisztensen a modul többi részével)
5. Integration tests írása kötelező (minimum 3/endpoint)

**Becsült időtartam ha implementálni kell:** +3-4 nap (command/handler + domain logic + tests)

---

**Becsült időtartam (ha csak validálás):** 0.5-1 nap
**Becsült időtartam (ha implementálni kell):** 3-4 nap
**Prioritás:** 🟡 MEDIUM — 2. ügyfél B2B feature, nem MVP blocker, de fontos
**Dependencies:** Nincs blocker, párhuzamosan futhat Frontend Feature 1+2-vel
