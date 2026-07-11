---
processed: 2026-07-11
id: MSG-BACKEND-456
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-453-DONE
epic_id: EPIC-JT-CRM
checkpoint_id: CP-CRM-INTEGRATION
created: 2026-07-10
estimated_nwt: 15
content_hash: 37c2fb416725bada8099334505c702563197717841edf9e0346c7fa86cb942d4
---

# CRM Phase 1 Completion — Application/API Layer

**Predecessor:** MSG-BACKEND-453 (CRM Phase 1 - 75% complete)
**Status:** Domain + Events DONE, Application/API pending

---

## Context

MSG-453 completed the foundation:
- ✅ Opportunity FSM with Converting state (pre-existing)
- ✅ Contract events (OpportunityConvertedToQuoteEvent, QuoteCreatedFromOpportunityEvent, QuoteCreationFailedEvent)

**This task completes the remaining 25%:** Command/Event handlers, API endpoints, tests.

---

## Scope (15 NWT)

### 1. Command Handler (5 NWT)
**File:** `spaceos-modules-crm/src/Application/Opportunities/ConvertOpportunityToQuote/ConvertOpportunityToQuoteCommandHandler.cs`

```csharp
public record ConvertOpportunityToQuoteCommand(Guid OpportunityId, Guid ConversionId);
public record ConversionResult(Guid ConversionId);

public class ConvertOpportunityToQuoteCommandHandler : IRequestHandler<ConvertOpportunityToQuoteCommand, ConversionResult>
{
    public async Task<ConversionResult> Handle(ConvertOpportunityToQuoteCommand request, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(request.OpportunityId, ct);

        // Idempotent: Same conversionId → no duplicate event
        opportunity.StartConversion(request.ConversionId);

        await _repository.SaveAsync(opportunity, ct);

        return new ConversionResult(request.ConversionId);
    }
}
```

### 2. Event Handlers - Sales→CRM (5 NWT)

**File:** `spaceos-modules-crm/src/Application/EventHandlers/QuoteCreatedFromOpportunityEventHandler.cs`
```csharp
public class QuoteCreatedFromOpportunityEventHandler : INotificationHandler<QuoteCreatedFromOpportunityEvent>
{
    public async Task Handle(QuoteCreatedFromOpportunityEvent notification, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(notification.OpportunityId, ct);
        opportunity.CompleteConversion(notification.QuoteId); // Converting → Won
        await _repository.SaveAsync(opportunity, ct);
    }
}
```

**File:** `spaceos-modules-crm/src/Application/EventHandlers/QuoteCreationFailedEventHandler.cs`
```csharp
public class QuoteCreationFailedEventHandler : INotificationHandler<QuoteCreationFailedEvent>
{
    public async Task Handle(QuoteCreationFailedEvent notification, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(notification.OpportunityId, ct);
        opportunity.RollbackConversion(); // Converting → Negotiation
        await _repository.SaveAsync(opportunity, ct);
    }
}
```

### 3. API Endpoints (3 NWT)

**File:** `spaceos-modules-crm/src/API/Opportunities/OpportunitiesController.cs`

```csharp
[HttpPost("{id:guid}/convert-to-quote")]
[Authorize]
public async Task<ActionResult<ConversionResult>> ConvertToQuote(Guid id)
{
    var conversionId = Guid.NewGuid();
    var command = new ConvertOpportunityToQuoteCommand(id, conversionId);
    var result = await _mediator.Send(command);

    return Accepted($"/api/crm/conversions/{conversionId}", result); // 202 Accepted
}

[HttpGet("conversions/{conversionId:guid}")]
[Authorize]
public async Task<ActionResult<ConversionStatusDto>> GetConversionStatus(Guid conversionId)
{
    // Poll endpoint for conversion status (pending/completed/failed)
    var opportunity = await _repository.GetByConversionIdAsync(conversionId);

    return Ok(new ConversionStatusDto
    {
        ConversionId = conversionId,
        Status = opportunity.Status == OpportunityStatus.Won ? "completed" :
                 opportunity.Status == OpportunityStatus.Converting ? "pending" : "failed",
        QuoteId = opportunity.QuoteRef
    });
}
```

### 4. Integration Tests (2 NWT)

**File:** `spaceos-modules-crm/tests/Integration/ConvertOpportunityToQuote_Tests.cs`

```csharp
[Fact]
public async Task ConvertOpportunity_HappyPath_TransitionsToWon()
{
    // Arrange: Opportunity in Negotiation status
    var opportunity = await CreateOpportunity(OpportunityStatus.Negotiation);

    // Act: Start conversion
    var conversionId = Guid.NewGuid();
    await SendCommand(new ConvertOpportunityToQuoteCommand(opportunity.Id, conversionId));

    // Assert: Status = Converting
    var updated = await GetOpportunity(opportunity.Id);
    Assert.Equal(OpportunityStatus.Converting, updated.Status);
    Assert.Equal(conversionId, updated.ConversionId);

    // Simulate Sales module success
    await PublishEvent(new QuoteCreatedFromOpportunityEvent
    {
        OpportunityId = opportunity.Id,
        ConversionId = conversionId,
        QuoteId = Guid.NewGuid()
    });

    // Assert: Status = Won
    var final = await GetOpportunity(opportunity.Id);
    Assert.Equal(OpportunityStatus.Won, final.Status);
    Assert.NotNull(final.QuoteRef);
}

[Fact]
public async Task ConvertOpportunity_Idempotent_SameConversionId_NoError()
{
    // Same conversionId sent twice → no duplicate event
    var conversionId = Guid.NewGuid();
    await SendCommand(new ConvertOpportunityToQuoteCommand(opportunityId, conversionId));
    await SendCommand(new ConvertOpportunityToQuoteCommand(opportunityId, conversionId)); // Idempotent

    // Only 1 event published
    Assert.Single(GetPublishedEvents<OpportunityConvertedToQuoteEvent>());
}
```

---

## Acceptance Criteria

- [ ] Command handler implemented and registered in DI
- [ ] Event handlers (QuoteCreated, QuoteCreationFailed) implemented
- [ ] API endpoints (POST convert, GET conversion status) implemented with [Authorize]
- [ ] Integration tests PASS (happy path + idempotent retry)
- [ ] `dotnet build spaceos-modules-crm/` succeeds (0 errors)
- [ ] OpenAPI spec updated (Swagger endpoint visible)

---

## Security Checklist

- [ ] [Authorize] attribute on API endpoints
- [ ] Tenant filtering in repository queries (RLS)
- [ ] Input validation (Guid not empty)
- [ ] No sensitive data in logs

---

## Build & Test Commands

```bash
cd /opt/spaceos/backend

# Build CRM module
dotnet build spaceos-modules/spaceos-modules-crm/

# Run integration tests
dotnet test spaceos-modules/spaceos-modules-crm/tests/ --filter ConvertOpportunityToQuote

# Verify OpenAPI spec
curl http://localhost:5000/swagger/v1/swagger.json | jq '.paths["/api/crm/opportunities/{id}/convert-to-quote"]'
```

---

## Files to Create/Modify

1. `ConvertOpportunityToQuoteCommandHandler.cs` (NEW)
2. `QuoteCreatedFromOpportunityEventHandler.cs` (NEW)
3. `QuoteCreationFailedEventHandler.cs` (NEW)
4. `OpportunitiesController.cs` (APPEND - 2 new endpoints)
5. `ConvertOpportunityToQuote_Tests.cs` (NEW)
6. `ApplicationServiceCollectionExtensions.cs` (MODIFY - register handlers)

---

## Dependencies

**Pre-existing (from MSG-453):**
- Opportunity.StartConversion(), CompleteConversion(), RollbackConversion() methods ✅
- OpportunityConvertedToQuoteEvent contract ✅
- QuoteCreatedFromOpportunityEvent contract ✅
- QuoteCreationFailedEvent contract ✅

**Sales Module:** Phase 2 (not implemented yet) - use mock event publishing for tests

---

## Estimated Timeline

- **5 NWT:** Command handler
- **5 NWT:** Event handlers
- **3 NWT:** API endpoints
- **2 NWT:** Integration tests
- **Total:** 15 NWT (~30 minutes)

---

## Next Steps After Completion

1. Update CP-CRM-INTEGRATION checkpoint status → DONE
2. Dispatch CRM Phase 2 (Sales module - QuoteAggregate implementation)

---

**Priority:** HIGH (completes CRM integration checkpoint)
**Estimate:** 15 NWT
**Complexity:** LOW (foundation already exists)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
