---
id: MSG-BACKEND-031
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-007,MSG-BACKEND-030
created: 2026-06-29
processed: 2026-06-29
result: BLOCKED
outbox: MSG-BACKEND-083
blocker: MSG-BACKEND-030 (PARTIAL - no domain events, no persistence)
content_hash: 011129f2c2cdc3f584e2d95a859092488c33ff00aada3ef3f9f755bc0af426d7
---

# Q3 Cutting Expansion — Track A: Email Notification Integration

## Context

**Q3 Cutting Expansion APPROVED** by Root (MSG-CONDUCTOR-007). This is Track A, Phase 2: Email notification when customer submits quote request.

**Depends on:** MSG-BACKEND-030 (Quote Request API) — must be implemented first to hook into `QuoteRequestCreated` event

## Task: Brevo SMTP Email Integration

### Requirements

When a quote request is received (via `QuoteRequestCreated` event), send automated emails:

1. **To Customer:** Confirmation email + tracking link
2. **To Sales Team:** New quote request notification

### Email Templates

#### Template 1: Customer Confirmation (HTML)

```html
Subject: Quote Request Received — Tracking #{quoteId}

Dear {customerName},

Thank you for your quote request! We have received your inquiry for:
- Material: {material}
- Dimensions: {length}x{width}x{thickness}mm
- Quantity: {quantity}

Your quote ID: {quoteId}
Track status: {trackingUrl}

We will review your request and send you an estimate within 2 business days.

Best regards,
Doorstar Cutting Services
```

#### Template 2: Internal Notification (Plain text)

```
New Quote Request:
ID: {quoteId}
Customer: {customerName} ({customerEmail})
Company: {companyName}
Material: {material}
Dimensions: {length}x{width}x{thickness}mm
Quantity: {quantity}
Urgency: {urgency}
Timestamp: {created_at}

Review at: /admin/cutting/quotes/{quoteId}
```

### Brevo Configuration

**Setup (one-time):**
1. Get API key from Brevo account
2. Store in `.env`:
   ```
   BREVO_API_KEY=sk-...
   BREVO_FROM_EMAIL=sales@doorstar.hu
   BREVO_FROM_NAME=Doorstar Cutting
   ```
3. Create/verify sender email in Brevo dashboard

**Email Service Integration:**

```csharp
// Services/EmailService.cs (new file or extend existing)
public interface IEmailService
{
    Task SendQuoteConfirmationAsync(PublicQuoteRequest quote, CancellationToken ct);
    Task SendQuoteNotificationAsync(PublicQuoteRequest quote, CancellationToken ct);
}

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BrevoEmailService> _logger;

    public async Task SendQuoteConfirmationAsync(PublicQuoteRequest quote, CancellationToken ct)
    {
        // POST to https://api.brevo.com/v3/smtp/email
        // Build customer email with template
        // Log success/failure
    }

    public async Task SendQuoteNotificationAsync(PublicQuoteRequest quote, CancellationToken ct)
    {
        // Send internal notification to sales team
        // Error handling + retry logic
    }
}
```

### Event Handler Integration

**In `QuoteRequestService` or via Domain Event subscribers:**

```csharp
public class QuoteRequestCreatedEventHandler : IDomainEventHandler<QuoteRequestCreated>
{
    private readonly IEmailService _emailService;

    public async Task Handle(QuoteRequestCreated @event, CancellationToken ct)
    {
        var quoteRequest = await _repository.GetByIdAsync(@event.QuoteId, ct);

        // Send customer confirmation
        await _emailService.SendQuoteConfirmationAsync(quoteRequest, ct);

        // Send internal notification
        await _emailService.SendQuoteNotificationAsync(quoteRequest, ct);

        _logger.LogInformation("Emails sent for quote {QuoteId}", quoteRequest.Id);
    }
}
```

### Implementation Checklist

#### Phase 1: Brevo Service Implementation (2 hours)

- [ ] Create `Services/EmailService.cs` with `IEmailService` interface
- [ ] Implement `BrevoEmailService` class:
  - [ ] HTTP client to Brevo API
  - [ ] Template rendering (customer + internal)
  - [ ] Error handling (timeout, 4xx, 5xx)
  - [ ] Retry logic (exponential backoff for transient errors)
  - [ ] Logging (INFO for success, WARN/ERROR for failures)
- [ ] DI registration in `Program.cs`

#### Phase 2: Event Handler (1 hour)

- [ ] Create `EventHandlers/QuoteRequestCreatedEventHandler.cs`
- [ ] Register in DI container
- [ ] Test integration with domain event bus
- [ ] Handle exceptions gracefully (log but don't crash)

#### Phase 3: Configuration & Secrets (1 hour)

- [ ] Add environment variables:
  - [ ] `BREVO_API_KEY`
  - [ ] `BREVO_FROM_EMAIL`
  - [ ] `BREVO_FROM_NAME`
- [ ] `.env.example` updated
- [ ] Configuration validation on startup

#### Phase 4: Testing (2 hours)

- [ ] Unit test: `BrevoEmailService.SendQuoteConfirmationAsync()`
  - [ ] Mocked HttpClient
  - [ ] Verify request payload
  - [ ] Test error scenarios
- [ ] Integration test: Full event flow
  - [ ] Create quote request (MSG-BACKEND-030 endpoint)
  - [ ] Verify event handler triggered
  - [ ] Mock Brevo API response
- [ ] Test coverage target: 80%+

### Error Handling Strategy

- **Brevo API timeouts:** Retry up to 3 times with exponential backoff
- **4xx errors (invalid email, rate limit):** Log warning, don't retry
- **5xx errors (server error):** Retry with backoff
- **Event handler failures:** Log error but don't block quote creation
- **Always:** Send alerts to ops if email fails 3+ times in 1 hour

### Dependencies

- Brevo SDK or HttpClient wrapper
- Domain event system (from MSG-BACKEND-030)
- Configuration/Options pattern (.NET)

### API Contracts (Frontend Reference)

Frontend needs tracking URL for customer confirmation email:
```
GET /api/public/cutting/quote/{quoteId}/status
Response: { status: "received", estimatedReplyTime: "..." }
```

**Frontend implements:** MSG-FRONTEND-018 (Status tracking page)

### Definition of Done

✅ Criteria:
1. Email service integrated with Brevo API
2. Event handler hooked to `QuoteRequestCreated`
3. Both email templates sending successfully (test)
4. Retry logic tested (simulate failures)
5. Configuration from `.env` working
6. Logging complete (INFO/WARN/ERROR)
7. No unhandled exceptions
8. Tests passing (80%+ coverage)

### Success Indicators

- [ ] Zero compilation errors
- [ ] `npm run build` + `dotnet build` PASS
- [ ] Tests: `npm test` PASS
- [ ] Brevo API integration verified (real API call in staging)
- [ ] Email delivery time: <5 seconds

### Parallel Execution

✅ **Can run in parallel with:** MSG-BACKEND-030 (they're separate layers)

❌ **Blocking:** Must complete before Frontend deployment (MSG-FRONTEND-018)

---

## Model Hint

**Model: `sonnet`** — standard integration task

**Effort estimate:** 6 hours (3 implementation + 2 testing + 1 Brevo setup)

**Timeline:** Parallel with MSG-BACKEND-030, both done by end of day

---

**Conductor dispatch:** 2026-06-29 19:35 UTC
**Q3 Target:** Track A complete by 2026-07-07

### Reference

- Brevo docs: https://developers.brevo.com/reference/sendtransacemail
- Event handling pattern: `docs/knowledge/patterns/EVENT_SOURCING_PATTERNS.md`
- Email error handling: `docs/knowledge/patterns/SECURITY_PATTERNS.md`
