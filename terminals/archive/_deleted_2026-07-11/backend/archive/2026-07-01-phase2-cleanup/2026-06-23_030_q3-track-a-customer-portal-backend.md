---
id: MSG-BACKEND-030
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-007
created: 2026-06-23
---

# Q3 Track A: Customer Self-Service Portal — Backend API

## Context

**Q3 Cutting Module Expansion:** ✅ ROOT APPROVED (MSG-CONDUCTOR-007)

**Status:** APPROVED (Root MSG-CONDUCTOR-007)
**Track:** A — Customer Self-Service Portal (Week 1-2)
**Target customer:** 2. ügyfél (Lapszabász KKV) — B2C quote workflow

## Task: Quote Request API (Unauthenticated Public Endpoint)

**Requirement:** External customers (non-tenant users) submit cutting quote requests online without login.

### 1. New Aggregate: QuoteRequest

**Location:** `spaceos-modules-cutting/Domain/QuoteRequest/`

**FSM States:**
```
Draft → PendingReview → [Approved | Rejected] → ConvertedToOrder
```

**Properties:**
- `QuoteRequestId` (Guid)
- `CustomerEmail` (required)
- `CustomerName` (required)
- `MaterialType` (enum: MDF, Chipboard, Plywood, etc.)
- `TotalArea` (decimal m²)
- `EdgebandingRequired` (bool)
- `ThicknessMm` (int)
- `Quantity` (int)
- `Notes` (string, optional)
- `Status` (FSM state)
- `CreatedAt`, `ReviewedAt`, `ReviewedBy` (User ID), `ConvertedOrderId` (nullable)

**Domain Events:**
- `QuoteRequestSubmitted`
- `QuoteRequestReviewed` (Approved/Rejected)
- `QuoteRequestConvertedToOrder`

### 2. API Endpoint

**POST `/public/cutting/quote-request`**

**Request body:**
```json
{
  "customerEmail": "example@company.com",
  "customerName": "John Doe",
  "materialType": "MDF",
  "totalArea": 12.5,
  "edgebandingRequired": true,
  "thicknessMm": 18,
  "quantity": 50,
  "notes": "Urgent order for next week"
}
```

**Response:**
```json
{
  "quoteRequestId": "uuid",
  "status": "PendingReview",
  "createdAt": "2026-06-23T10:00:00Z",
  "trackingToken": "short-token-for-status-check"
}
```

**Authorization:** NONE (public endpoint)
**Validation:** Email format, totalArea > 0, quantity > 0

### 3. Admin Endpoints

**GET `/api/cutting/quote-requests`** (admin only)
- List all quote requests (filter by status, date range)

**PUT `/api/cutting/quote-requests/{id}/review`** (admin only)
- Request body: `{ "approved": true/false, "notes": "..." }`
- Triggers `QuoteRequestReviewed` event
- If approved → create `QuoteRequestConvertedToOrder` event (integration point for future workflow)

**GET `/public/cutting/quote-requests/{id}/status?token={trackingToken}`** (public)
- Returns status without authentication (magic link style)

### 4. Email Notification Hook

**Integration:** Brevo SMTP (already configured in Kernel)

**Trigger:** `QuoteRequestSubmitted` event
**Action:** Send email to admin + confirmation email to customer

**Email templates:**
- Admin notification: "New quote request from {customerName}"
- Customer confirmation: "Your quote request #{quoteRequestId} has been received"

**Implementation:** Create email service in `spaceos-modules-cutting/Application/Services/QuoteRequestEmailService.cs`

### 5. Testing Requirements

**Domain tests:** (minimum 8)
- QuoteRequest FSM transitions (Draft→PendingReview→Approved→ConvertedToOrder)
- Validation (email format, area > 0, quantity > 0)
- Domain events raised correctly

**Integration tests:** (minimum 4)
- POST `/public/cutting/quote-request` → 201 Created
- GET `/api/cutting/quote-requests` → 200 OK (admin auth)
- PUT `/api/cutting/quote-requests/{id}/review` → 200 OK
- GET `/public/cutting/quote-requests/{id}/status?token=...` → 200 OK

**Coverage target:** 70%+

### 6. Definition of Done

- [ ] QuoteRequest aggregate implemented with FSM
- [ ] POST `/public/cutting/quote-request` endpoint (unauthenticated)
- [ ] Admin endpoints (list, review, status check)
- [ ] Email notification service integrated (Brevo SMTP)
- [ ] 12+ tests written (8 domain + 4 integration)
- [ ] All tests PASS
- [ ] Build 0 errors
- [ ] API tested with curl/Postman (manual verification)

### 7. Implementation Guidance

**Patterns:**
- Follow existing Cutting module patterns (`CuttingSheet`, `CuttingPlan`)
- Use `IEmailService` from Kernel (Brevo SMTP already configured)
- Public endpoint: no `[Authorize]` attribute, validate input carefully
- Tracking token: use short hash (8 chars) for status check

**API Contract:**
- OpenAPI spec update required
- Frontend will consume this API (MSG-FRONTEND-018)

**Estimated effort:** 1.5 days

---

**Next tasks:**
- MSG-BACKEND-031: Email notification integration (Track A)
- MSG-BACKEND-032: Pricing Rule Engine (Track B)

**Questions?** Ask Conductor via outbox (type: question)

---

**Conductor**
2026-06-23 07:30 UTC
