---
id: MSG-BACKEND-013
from: mcp-server
to: backend
type: task
priority: high
status: READ
created: 2026-06-22
completed: 2026-06-22
content_hash: 10ed570c4f5e893af800bbd52d0223f60912d6e2fd1862c7d6fd1fc4dd108d9f
---

# Cutting Module Q3 — Customer Self-Service Portal (Backend)

## Context

A **Cutting modul backend PRODUCTION READY** (939 teszt). Q3 cél: 2. ügyfél fogadása (lapszabász KKV).

**Hiányzó feature:** Külső megrendelők (B2C) online szabás-ajánlatkérés.

Teljes terv: `/opt/spaceos/terminals/conductor/PLANNING_PROPOSAL_Q3_Cutting.md`

---

## Feladat 1: Quote Request API (1.5 nap)

### Endpoint Spec

**POST /public/cutting/quote-request** (unauthenticated)

Request:
```json
{
  "customerEmail": "janos@example.com",
  "customerName": "Nagy János",
  "customerPhone": "+36301234567",
  "items": [
    {
      "materialType": "MDF_18MM",
      "widthMm": 2800,
      "heightMm": 2070,
      "quantity": 5,
      "edgingType": "ABS_2MM_WHITE",
      "notes": "4 oldalon élzárás"
    }
  ],
  "deliveryAddress": "Budapest, Kossuth utca 10.",
  "requestedDeliveryDate": "2026-07-15"
}
```

Response:
```json
{
  "quoteId": "QT-2026-001234",
  "trackingToken": "a8f3d9e2b1c4",
  "status": "PENDING_REVIEW",
  "estimatedResponseTime": "24 hours",
  "trackingUrl": "https://joinerytech.hu/quote/a8f3d9e2b1c4"
}
```

### Domain Model

**Aggregate Root:** `CuttingQuoteRequest`

```csharp
public sealed class CuttingQuoteRequest : AggregateRoot
{
    public string QuoteNumber { get; private set; }  // QT-2026-NNNNNN
    public string TrackingToken { get; private set; } // random 12-char hex
    public ContactInfo CustomerContact { get; private set; }
    public List<QuoteLineItem> Items { get; private set; }
    public DeliveryDetails Delivery { get; private set; }
    public QuoteStatus Status { get; private set; } // FSM
    public Money? QuotedPrice { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ConvertedToOrderAt { get; private set; }
    public Guid? CuttingSheetId { get; private set; }
    
    public static CuttingQuoteRequest CreatePublic(...)
    public void ApproveAndQuote(Money price, Guid userId)
    public void Reject(string reason, Guid userId)
    public void ConvertToOrder() → creates CuttingSheet
}
```

**FSM:**
```
PendingReview → (Approve) → Quoted → (Customer Accepts) → ConvertedToOrder
             ↘ (Reject) → Rejected
```

### Value Objects

```csharp
public record ContactInfo(string Email, string Name, string Phone);
public record QuoteLineItem(MaterialType Material, int WidthMm, int HeightMm, int Quantity, EdgingType Edging, string? Notes);
public record DeliveryDetails(string Address, DateTime RequestedDate);
```

### Database Schema

```sql
CREATE TABLE spaceos_cutting.quote_requests (
    id UUID PRIMARY KEY,
    quote_number TEXT NOT NULL UNIQUE,
    tracking_token TEXT NOT NULL UNIQUE,
    customer_email TEXT NOT NULL,
    customer_name TEXT NOT NULL,
    customer_phone TEXT,
    items JSONB NOT NULL,
    delivery_address TEXT NOT NULL,
    requested_delivery_date TIMESTAMP,
    status TEXT NOT NULL,
    quoted_price_amount DECIMAL(10,2),
    quoted_price_currency TEXT,
    reviewed_at TIMESTAMP,
    reviewed_by_user_id UUID,
    converted_to_order_at TIMESTAMP,
    cutting_sheet_id UUID REFERENCES spaceos_cutting.cutting_sheets(id),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    version INT NOT NULL
);

CREATE INDEX idx_quote_requests_tracking_token ON spaceos_cutting.quote_requests(tracking_token);
CREATE INDEX idx_quote_requests_status ON spaceos_cutting.quote_requests(status);
```

---

## Feladat 2: Quote Admin API (0.5 nap)

**GET /api/cutting/quotes** (tenant admin)
- Lista: pending review quotes
- Filter: status, date range

**PUT /api/cutting/quotes/{quoteId}/approve** (tenant admin)
- Input: quotedPrice
- Action: Status → Quoted, email notification

**PUT /api/cutting/quotes/{quoteId}/reject** (tenant admin)
- Input: reason
- Action: Status → Rejected, email notification

---

## Feladat 3: Email Notification (0.5 nap)

**Brevo SMTP integration** (már konfigurálva ✅)

**3 email template:**
1. Quote received (to customer) — "Ajánlatkérését fogadtuk, 24 órán belül válaszolunk"
2. Quote approved (to customer) — "Ajánlatunk: {price} HUF, kattintson az elfogadáshoz: {trackingUrl}"
3. Quote rejected (to customer) — "Sajnáljuk, a kért munka nem vállalható: {reason}"

**Implementation:**
- `IEmailSender` interfész (már van Kernel-ben?)
- VAGY új `IQuoteNotificationService` Cutting module-ban
- Brevo API v3 (smtp-relay.brevo.com:587 ✅)

---

## Feladat 4: Tracking Endpoint (0.5 nap)

**GET /public/cutting/quotes/track/{trackingToken}** (unauthenticated)

Response:
```json
{
  "quoteNumber": "QT-2026-001234",
  "status": "QUOTED",
  "submittedAt": "2026-06-22T10:30:00Z",
  "quotedPrice": {
    "amount": 45000,
    "currency": "HUF"
  },
  "estimatedDelivery": "2026-07-15",
  "actionRequired": {
    "type": "CUSTOMER_ACCEPTANCE",
    "description": "Kattintson az 'Elfogadom' gombra az ajánlat véglegesítéséhez",
    "acceptUrl": "/public/cutting/quotes/{trackingToken}/accept"
  }
}
```

**POST /public/cutting/quotes/track/{trackingToken}/accept** (unauthenticated)
- Action: ConvertToOrder() → creates CuttingSheet
- Response: "Rendelés rögzítve, hamarosan felvesszük Önnel a kapcsolatot"

---

## Acceptance Criteria

- [ ] `CuttingQuoteRequest` aggregate + FSM implementálva
- [ ] POST /public/cutting/quote-request endpoint működik
- [ ] Quote admin endpoints (GET /quotes, PUT /approve, PUT /reject) működnek
- [ ] Email notifications kiküldve (3 template)
- [ ] Tracking endpoint (GET/POST /track/{token}) működik
- [ ] Migration + DbContext frissítve
- [ ] Tesztek: Unit (Domain FSM) + Integration (API endpoints) — minimum 50+ teszt
- [ ] Swagger docs frissítve
- [ ] Build 0 error, 0 warning

---

## Kapcsolódó dokumentáció

- `/opt/spaceos/terminals/conductor/PLANNING_PROPOSAL_Q3_Cutting.md` — teljes Q3 terv
- `/opt/spaceos/backend/spaceos-modules-cutting/` — modul forráskód
- `/opt/spaceos/docs/Codebase_Status.md` — jelenlegi állapot

---

**Assignee:** Backend
**Priority:** HIGH
**Effort:** 2.5 nap
**Blocking:** —
**Expected completion:** 2026-06-25

