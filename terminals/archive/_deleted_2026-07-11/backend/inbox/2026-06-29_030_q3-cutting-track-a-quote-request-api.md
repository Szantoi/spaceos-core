---
id: MSG-BACKEND-030
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-007,ADR-046
created: 2026-06-29
processed: 2026-06-29
result: PARTIAL_DONE
outbox: MSG-BACKEND-081
content_hash: 33da24d581255c4a24101eddeb4b7641e1709910939aa01a4109ba33ac44280a
---

# Q3 Cutting Expansion — Track A: Quote Request API

## Context

**Q3 Cutting Expansion APPROVED** by Root (MSG-CONDUCTOR-007). This is Track A implementation: Customer Self-Service Portal (B2C).

**Track scope:**
- Public Quote Request API (unauthenticated)
- Email notification integration (Brevo SMTP)
- Basic quote status tracking storage

## Task: Quote Request API Implementation

### API Endpoint Definition

```
POST /api/public/cutting/quote-request
Content-Type: application/json

{
  "customerName": "string",
  "customerEmail": "string",
  "customerPhone": "string",
  "companyName": "string",
  "material": "string",          // pl: "tölgy", "fenyő", "mdf"
  "dimensions": {
    "length": number,            // mm
    "width": number,             // mm
    "thickness": number          // mm
  },
  "quantity": number,
  "edgeType": "string",          // pl: "ABS", "PVC", "furnir"
  "surface": "string",           // pl: "matt", "fényes", "textúra"
  "urgency": "standard" | "express",
  "notes": "string",
  "attachments": [               // base64 encoded or file upload
    { "filename": "...", "data": "..." }
  ]
}

Response 201 Created:
{
  "quoteId": "uuid",
  "status": "received",
  "estimatedReplyTime": "2 business days",
  "trackingUrl": "/public/quote/{quoteId}/status"
}
```

### Database Schema

**Table: `PublicQuoteRequest`**
```sql
CREATE TABLE public_quote_requests (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_name VARCHAR(255) NOT NULL,
  customer_email VARCHAR(255) NOT NULL,
  customer_phone VARCHAR(20),
  company_name VARCHAR(255),
  material VARCHAR(100),
  length_mm DECIMAL(10,2),
  width_mm DECIMAL(10,2),
  thickness_mm DECIMAL(10,2),
  quantity INT,
  edge_type VARCHAR(50),
  surface VARCHAR(50),
  urgency VARCHAR(20) DEFAULT 'standard',
  notes TEXT,
  status VARCHAR(20) DEFAULT 'received',  -- received | reviewed | quoted | rejected
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_quote_email ON public_quote_requests(customer_email);
CREATE INDEX idx_quote_created ON public_quote_requests(created_at DESC);
```

### Implementation Checklist

#### Phase 1: API Layer (2 hours)

- [ ] POST endpoint in `Controllers/PublicQuotingController.cs`
- [ ] Input validation:
  - [ ] Email format validation
  - [ ] Dimensions > 0
  - [ ] Quantity > 0
  - [ ] Required fields presence
- [ ] Error responses:
  - [ ] 400 Bad Request (validation failed)
  - [ ] 201 Created (success)
  - [ ] 500 error handling + logging

#### Phase 2: Domain Logic (2 hours)

- [ ] `QuoteRequest` aggregate in `Modules/Cutting/Domain/`
- [ ] `CreateQuoteRequest` command
- [ ] `QuoteRequestService` — business logic:
  - [ ] Quote ID generation (UUID)
  - [ ] Estimated reply time calculation (2 business days)
  - [ ] Material/dimension validation against catalog
- [ ] Event: `QuoteRequestCreated`

#### Phase 3: Persistence (1 hour)

- [ ] EF Core DbContext mapping
- [ ] Migration: new table `PublicQuoteRequests`
- [ ] Repository pattern (if not using direct DbContext)

#### Phase 4: Testing (2 hours)

- [ ] Unit test: `CreateQuoteRequest` command
- [ ] Integration test: POST /api/public/cutting/quote-request
  - [ ] Happy path (201)
  - [ ] Validation failures (400)
  - [ ] DB persistence verification
- [ ] Test coverage target: 85%+

### API Security Notes

⚠️ **This endpoint is PUBLIC and UNAUTHENTICATED**

- [ ] Rate limiting: 50 requests/hour per IP
- [ ] CORS: Allow frontend domain only
- [ ] Input sanitization (prevent SQL injection, XSS)
- [ ] Logging: Log all quote requests (audit trail)
- [ ] No sensitive internal data in response

### Dependencies

- Entity Framework Core (DbContext)
- Cutting module domain layer
- Email service (prepared for Track A — Email notification in MSG-BACKEND-031)

### Definition of Done

✅ Criteria:
1. POST endpoint returns 201 with quoteId
2. All validation tests passing (85%+ coverage)
3. DB migration applied, schema verified
4. Audit logging implemented
5. API documented in `/api/public/cutting/quote-request`
6. Error handling complete (no unhandled exceptions)
7. Ready for Frontend integration (MSG-FRONTEND-018)

### Success Indicators

- [ ] Zero TypeScript/C# compilation errors
- [ ] `npm run build` + `dotnet build` PASS
- [ ] Tests: `npm test` PASS (backend)
- [ ] API response time: <200ms
- [ ] DB queries logged (EF Core logging)

### Parallel Track Notes

**Frontend waits for:** This API endpoint (MSG-BACKEND-030) → Frontend MSG-FRONTEND-018 starts

**Next Backend task:** MSG-BACKEND-031 (Email notification) — can run in parallel

---

## Model Hint

**Model: `sonnet`** — standard implementation task

**Effort estimate:** 7 hours (4 implementation + 3 testing/fixes)

**Timeline:** Completion by end of working day (if parallel with MSG-BACKEND-031)

---

**Conductor dispatch:** 2026-06-29 19:35 UTC
**Q3 Target:** Track A complete by 2026-07-07 (Week 1 end)

### Session Context (Cold-Start Ready)

Use `/spaceos-root` terminal context from last session (ADR-046 cold-start enabled).

**Reference:**
- API contracts: `docs/knowledge/patterns/API_CONTRACTS.md`
- Testing patterns: `docs/knowledge/patterns/TESTING_PATTERNS.md`
- Domain patterns: `docs/knowledge/patterns/DATABASE_PATTERNS.md`
