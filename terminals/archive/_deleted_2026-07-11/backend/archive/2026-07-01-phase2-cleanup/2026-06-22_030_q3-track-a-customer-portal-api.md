---
id: MSG-BACKEND-030
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: 55a4d36d9e369eedc687371092efd12dd1ac5aa00612205ce6654227d0579744
---

# Q3 Track A: Customer Self-Service Portal - Backend API

## Összefoglaló

Implementáld a **Customer Self-Service Portal** backend API-ját, amely lehetővé teszi, hogy lapszabász KKV ügyfelek (B2C) online árajánlatkérést nyújtsanak be.

## Scope

**Modul:** `spaceos-modules-cutting` (port 5005)
**Időkeret:** 4 nap (Track A)
**Prioritás:** HIGH — 2. ügyfél (Lapszabász KKV) onboarding Q3 2026

## Implementációs lépések

### 1. Domain Layer (1 nap)

**Új aggregate:** `PublicQuoteRequest`

```csharp
public class PublicQuoteRequest : AggregateRoot<Guid>
{
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public string CustomerPhone { get; private set; }
    public List<CutPieceRequest> Pieces { get; private set; }
    public QuoteRequestStatus Status { get; private set; } // Submitted, Processing, Quoted, Rejected
    public DateTime SubmittedAt { get; private set; }
    public Guid? ProcessedByUserId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // Factory method
    public static PublicQuoteRequest Submit(
        string customerName,
        string customerEmail,
        string customerPhone,
        List<CutPieceRequest> pieces);

    // FSM transitions
    public void MarkAsProcessing(Guid userId);
    public void GenerateQuote(decimal totalPrice, string quoteDocument);
    public void Reject(string reason);
}

public enum QuoteRequestStatus
{
    Submitted = 0,
    Processing = 1,
    Quoted = 2,
    Rejected = 3
}

public class CutPieceRequest : ValueObject
{
    public string MaterialCode { get; private set; }
    public decimal Length { get; private set; }
    public decimal Width { get; private set; }
    public int Quantity { get; private set; }
    public EdgeBanding EdgeBanding { get; private set; } // None, All, Custom
}
```

**Domain Events:**
- `PublicQuoteRequestSubmittedEvent`
- `QuoteRequestProcessingStartedEvent`
- `QuoteGeneratedEvent`
- `QuoteRequestRejectedEvent`

### 2. Application Layer (1 nap)

**Commands:**
```csharp
public record SubmitPublicQuoteRequestCommand(
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    List<CutPieceRequestDto> Pieces
) : IRequest<Guid>;

public record ProcessQuoteRequestCommand(
    Guid QuoteRequestId,
    Guid ProcessedByUserId
) : IRequest;

public record GenerateQuoteCommand(
    Guid QuoteRequestId,
    decimal TotalPrice,
    string QuoteDocumentUrl
) : IRequest;
```

**Queries:**
```csharp
public record GetPublicQuoteRequestsQuery(
    QuoteRequestStatus? Status,
    DateTime? FromDate,
    int PageSize = 20,
    int Page = 1
) : IRequest<PagedResult<PublicQuoteRequestDto>>;

public record GetQuoteRequestByIdQuery(Guid Id) : IRequest<PublicQuoteRequestDto>;
```

### 3. Infrastructure Layer (1 nap)

**Database:**
- Schema: `spaceos_cutting`
- Tábla: `public_quote_requests`
- Migráció: `AddPublicQuoteRequests`

**Repository:**
```csharp
public interface IPublicQuoteRequestRepository : IRepository<PublicQuoteRequest>
{
    Task<PagedResult<PublicQuoteRequest>> GetByStatusAsync(
        QuoteRequestStatus? status,
        DateTime? fromDate,
        int pageSize,
        int page,
        CancellationToken ct);
}
```

### 4. API Layer (1 nap)

**Public Endpoints (Unauthenticated):**

```http
POST /cutting/api/public/quote-requests
Content-Type: application/json

{
  "customerName": "Kovács János",
  "customerEmail": "kovacs@example.com",
  "customerPhone": "+36301234567",
  "pieces": [
    {
      "materialCode": "PAL-18-WHITE",
      "length": 2000,
      "width": 600,
      "quantity": 10,
      "edgeBanding": "All"
    }
  ]
}

Response: 201 Created
{
  "quoteRequestId": "guid",
  "submittedAt": "2026-06-22T10:00:00Z",
  "estimatedResponseTime": "24 hours"
}
```

**Internal Endpoints (Authenticated, Role: cutting_operator, cutting_admin):**

```http
GET /cutting/api/quote-requests?status=Submitted&pageSize=20&page=1

Response: 200 OK
{
  "items": [...],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20
}

POST /cutting/api/quote-requests/{id}/process
Authorization: Bearer {token}

POST /cutting/api/quote-requests/{id}/generate-quote
Content-Type: application/json
{
  "totalPrice": 150000,
  "quoteDocumentUrl": "https://..."
}

POST /cutting/api/quote-requests/{id}/reject
Content-Type: application/json
{
  "reason": "Requested material not available"
}
```

### 5. Security

**Rate Limiting:**
- Public endpoint: 10 submissions / hour / IP
- Implement `IRateLimitingService` (Redis-based)

**Validation:**
- Email format validation
- Phone number format validation (Hungarian +36)
- Material code validation (must exist in Abstractions Catalog)
- Max 50 pieces per quote request
- Dimension validation (10mm - 3000mm)

**SPAM Protection:**
- Turnstile verification (optional — later)
- Email domain blacklist check

### 6. Tesztek

**Minimum:**
- 15 unit tests (domain FSM transitions)
- 10 integration tests (EF Core + Testcontainers)
- 8 API tests (WebApplicationFactory)

**Test coverage cél:** 90%+ (domain + application layer)

## Definition of Done

✅ Domain aggregate + events + FSM implementálva
✅ Command/Query handlers tesztelve
✅ Database migration + repository
✅ Public + internal API endpoints
✅ Rate limiting + validation
✅ 33+ teszt pass (15 unit + 10 IT + 8 API)
✅ OpenAPI dokumentáció frissítve
✅ `dotnet build` és `dotnet test` sikeresen lefut

## Blokkolók

**NONE** — minden dependency rendelkezésre áll.

## Kapcsolódó feladatok

- **Frontend:** MSG-FRONTEND-018 (Public Quote Request Form UI)
- **Track B:** MSG-BACKEND-031 (Pricing Integration)

## Referenciák

- ADR-039: Cutting Module Architecture
- Cutting Module API Contract: `/cutting/api/*`
- Abstractions Catalog API: `/abstractions/api/modules/materials`

---

**Határidő:** 2026-06-26 (Track A, 4 nap)
**Assigned to:** Backend terminal
**Model:** sonnet
