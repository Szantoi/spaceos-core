---
id: MSG-BACKEND-453
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: ADR-063
created: 2026-07-10
estimated_nwt: 30
epic_id: EPIC-JT-CRM
checkpoint_id: CP-CRM-INTEGRATION
content_hash: 468c43683f3f945ef5c9190c5741040546dc9717c5a822b2b5a31c2a235cb4f4
---

# CP-CRM-INTEGRATION Phase 1: CRM Side Implementation

**Epic:** EPIC-JT-CRM (Customer Relationship Management Module)
**Checkpoint:** CP-CRM-INTEGRATION
**Scope:** Opportunity.ConvertToQuote() → CRM-side event publishing + handlers
**Estimate:** 30 NWT (~1 hour)

**ADR Reference:** ADR-063 (CRM → Sales Integration Pattern)
**Pattern:** Asynchronous Domain Events with Outbox Pattern

---

## 🎯 GOAL

Implement the **CRM side** of the CRM→Sales integration:
1. Publish `OpportunityConvertedToQuoteEvent` when converting
2. Handle `QuoteCreatedFromOpportunityEvent` (success case)
3. Handle `QuoteCreationFailedEvent` (failure case)
4. API endpoint: `POST /api/crm/opportunities/{id}/convert-to-quote`

**Business Value:** Seamless handoff from CRM opportunity to Sales quote with reliable async integration.

---

## ✅ ACCEPTANCE CRITERIA

1. **Event Publishing**
   - `OpportunityConvertedToQuoteEvent` published on conversion
   - Includes conversionId (idempotency key)

2. **Event Handlers**
   - `QuoteCreatedFromOpportunityEventHandler` (update QuoteRef, transition to Won)
   - `QuoteCreationFailedEventHandler` (rollback to Negotiation)

3. **FSM Enhancement**
   - Opportunity FSM: Add "Converting" transient state
   - Timeout: 30 seconds auto-rollback

4. **API Endpoint**
   - POST /api/crm/opportunities/{id}/convert-to-quote
   - Returns 202 Accepted with conversionId
   - GET /api/crm/conversions/{conversionId} for polling

5. **Integration Tests**
   - Happy path: Qualified → Converting → Won (with QuoteRef)
   - Idempotent retry: Same conversionId → no duplicate event
   - Timeout: Converting >30s → auto-rollback to Negotiation

6. **Build Success**
   - 0 errors, minimal warnings
   - All existing CRM tests pass

---

## 📋 IMPLEMENTATION GUIDE

### 1. Event Definitions (Contracts Module)

**Location:** `/opt/spaceos/backend/spaceos-modules-contracts/SpaceOS.Modules.Contracts/CRM/Events/`

**OpportunityConvertedToQuoteEvent.cs**
```csharp
namespace SpaceOS.Modules.Contracts.CRM.Events;

public class OpportunityConvertedToQuoteEvent : ModuleEvent
{
    public Guid OpportunityId { get; init; }
    public Guid ConversionId { get; init; } // Idempotency key
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public List<QuoteLineItemDto> LineItems { get; init; } = new();
    public string? SpecialTerms { get; init; }
    public Guid SalesRepId { get; init; }
}

public class QuoteLineItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
```

**QuoteCreatedFromOpportunityEvent.cs** (Sales→CRM)
```csharp
public class QuoteCreatedFromOpportunityEvent : ModuleEvent
{
    public Guid ConversionId { get; init; }
    public Guid QuoteId { get; init; }
    public Guid OpportunityId { get; init; }
}
```

**QuoteCreationFailedEvent.cs** (Sales→CRM)
```csharp
public class QuoteCreationFailedEvent : ModuleEvent
{
    public Guid ConversionId { get; init; }
    public Guid OpportunityId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

### 2. Opportunity FSM Enhancement

**Location:** `/opt/spaceos/backend/spaceos-modules-crm/CRM.Domain/Opportunities/Opportunity.cs`

Add status:
```csharp
public enum OpportunityStatus
{
    Draft,
    Qualified,
    Negotiation,
    Converting, // NEW - transient state
    Won,
    Lost
}
```

Add properties:
```csharp
public Guid? QuoteRef { get; private set; } // Sales Quote ID
public Guid? ConversionId { get; private set; } // Idempotency key
public DateTime? ConversionStartedAt { get; private set; }
```

Add methods:
```csharp
public void StartConversion(Guid conversionId)
{
    if (Status != OpportunityStatus.Qualified)
        throw new InvalidOperationException("Only qualified opportunities can convert");

    if (ConversionId == conversionId && Status == OpportunityStatus.Converting)
        return; // Idempotent - already converting

    Status = OpportunityStatus.Converting;
    ConversionId = conversionId;
    ConversionStartedAt = DateTime.UtcNow;

    AddDomainEvent(new OpportunityConvertedToQuoteEvent
    {
        OpportunityId = Id,
        ConversionId = conversionId,
        CustomerId = CustomerId,
        CustomerName = CustomerName,
        CustomerEmail = CustomerEmail,
        LineItems = LineItems.Select(li => new QuoteLineItemDto
        {
            ProductId = li.ProductId,
            ProductName = li.ProductName,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice
        }).ToList(),
        SpecialTerms = SpecialTerms,
        SalesRepId = AssignedSalesRepId
    });
}

public void CompleteConversion(Guid quoteId)
{
    if (Status != OpportunityStatus.Converting)
        throw new InvalidOperationException("Opportunity is not converting");

    Status = OpportunityStatus.Won;
    QuoteRef = quoteId;
    ConversionId = null;
    ConversionStartedAt = null;
}

public void RollbackConversion(string reason)
{
    if (Status != OpportunityStatus.Converting)
        return; // Already rolled back

    Status = OpportunityStatus.Negotiation;
    ConversionId = null;
    ConversionStartedAt = null;

    AddDomainEvent(new OpportunityConversionRolledBackEvent(Id, reason));
}
```

### 3. Command Handler

**Location:** `/opt/spaceos/backend/spaceos-modules-crm/CRM.Application/Opportunities/ConvertOpportunityToQuote/ConvertOpportunityToQuoteCommandHandler.cs`

```csharp
public class ConvertOpportunityToQuoteCommand : IRequest<ConversionResult>
{
    public Guid OpportunityId { get; init; }
}

public class ConversionResult
{
    public Guid ConversionId { get; init; }
}

public class ConvertOpportunityToQuoteCommandHandler : IRequestHandler<ConvertOpportunityToQuoteCommand, ConversionResult>
{
    private readonly IOpportunityRepository _repository;

    public async Task<ConversionResult> Handle(ConvertOpportunityToQuoteCommand request, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(request.OpportunityId, ct);

        if (opportunity == null)
            throw new NotFoundException($"Opportunity {request.OpportunityId} not found");

        var conversionId = Guid.NewGuid();
        opportunity.StartConversion(conversionId);

        await _repository.SaveAsync(opportunity, ct);

        return new ConversionResult { ConversionId = conversionId };
    }
}
```

### 4. Event Handlers (CRM receives from Sales)

**QuoteCreatedFromOpportunityEventHandler.cs**
```csharp
public class QuoteCreatedFromOpportunityEventHandler : INotificationHandler<QuoteCreatedFromOpportunityEvent>
{
    private readonly IOpportunityRepository _repository;

    public async Task Handle(QuoteCreatedFromOpportunityEvent notification, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(notification.OpportunityId, ct);

        if (opportunity == null || opportunity.ConversionId != notification.ConversionId)
            return; // Ignore stale event

        opportunity.CompleteConversion(notification.QuoteId);
        await _repository.SaveAsync(opportunity, ct);
    }
}
```

**QuoteCreationFailedEventHandler.cs**
```csharp
public class QuoteCreationFailedEventHandler : INotificationHandler<QuoteCreationFailedEvent>
{
    private readonly IOpportunityRepository _repository;

    public async Task Handle(QuoteCreationFailedEvent notification, CancellationToken ct)
    {
        var opportunity = await _repository.GetByIdAsync(notification.OpportunityId, ct);

        if (opportunity == null || opportunity.ConversionId != notification.ConversionId)
            return;

        opportunity.RollbackConversion(notification.Reason);
        await _repository.SaveAsync(opportunity, ct);
    }
}
```

### 5. API Endpoint

**Location:** `/opt/spaceos/backend/spaceos-modules-crm/CRM.API/Opportunities/OpportunitiesController.cs`

```csharp
[HttpPost("{id:guid}/convert-to-quote")]
[ProducesResponseType(StatusCodes.Status202Accepted)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<ConversionResponse>> ConvertToQuote(
    [FromRoute] Guid id,
    CancellationToken ct)
{
    var command = new ConvertOpportunityToQuoteCommand { OpportunityId = id };
    var result = await _mediator.Send(command, ct);

    return Accepted(new ConversionResponse
    {
        ConversionId = result.ConversionId,
        Status = "Converting",
        PollUrl = Url.Action(nameof(GetConversionStatus), new { conversionId = result.ConversionId })
    });
}

[HttpGet("conversions/{conversionId:guid}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ConversionStatusResponse>> GetConversionStatus(
    [FromRoute] Guid conversionId,
    CancellationToken ct)
{
    var opportunity = await _opportunityRepository.GetByConversionIdAsync(conversionId, ct);

    if (opportunity == null)
        return NotFound();

    return Ok(new ConversionStatusResponse
    {
        ConversionId = conversionId,
        Status = opportunity.Status.ToString(),
        QuoteId = opportunity.QuoteRef,
        OpportunityId = opportunity.Id
    });
}
```

### 6. Timeout Handler (Background Service)

**Location:** `/opt/spaceos/backend/spaceos-modules-crm/CRM.Infrastructure/BackgroundServices/ConversionTimeoutMonitor.cs`

```csharp
public class ConversionTimeoutMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var stuckConversions = await _repository.GetConvertingOpportunitiesOlderThan(TimeSpan.FromSeconds(30), stoppingToken);

            foreach (var opportunity in stuckConversions)
            {
                opportunity.RollbackConversion("Timeout (30s exceeded)");
                await _repository.SaveAsync(opportunity, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

### 7. Integration Tests

**Location:** `/opt/spaceos/backend/spaceos-modules-crm/CRM.Tests/Integration/ConvertOpportunityToQuote_Tests.cs`

```csharp
[Fact]
public async Task ConvertToQuote_HappyPath_OpportunityWonWithQuoteRef()
{
    // Arrange
    var opportunity = await CreateQualifiedOpportunity();

    // Act - Convert
    var command = new ConvertOpportunityToQuoteCommand { OpportunityId = opportunity.Id };
    var result = await Mediator.Send(command);

    // Simulate Sales response
    var quoteCreatedEvent = new QuoteCreatedFromOpportunityEvent
    {
        ConversionId = result.ConversionId,
        QuoteId = Guid.NewGuid(),
        OpportunityId = opportunity.Id
    };
    await Mediator.Publish(quoteCreatedEvent);

    // Assert
    var updatedOpp = await OpportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
    Assert.Equal(OpportunityStatus.Won, updatedOpp.Status);
    Assert.NotNull(updatedOpp.QuoteRef);
}

[Fact]
public async Task ConvertToQuote_IdempotentRetry_NoDuplicateEvent()
{
    // Arrange
    var opportunity = await CreateQualifiedOpportunity();

    // Act - Convert twice with same ID
    var conversionId = Guid.NewGuid();
    await Mediator.Send(new ConvertOpportunityToQuoteCommand { OpportunityId = opportunity.Id });

    var eventCountBefore = DomainEventCollector.Count;
    opportunity.StartConversion(conversionId); // Idempotent
    var eventCountAfter = DomainEventCollector.Count;

    // Assert
    Assert.Equal(eventCountBefore, eventCountAfter); // No new event
}

[Fact]
public async Task ConvertToQuote_Timeout_AutoRollback()
{
    // Arrange
    var opportunity = await CreateQualifiedOpportunity();
    var command = new ConvertOpportunityToQuoteCommand { OpportunityId = opportunity.Id };
    await Mediator.Send(command);

    // Simulate timeout (mock ConversionStartedAt to 31 seconds ago)
    opportunity.ConversionStartedAt = DateTime.UtcNow.AddSeconds(-31);

    // Act - Timeout monitor runs
    await ConversionTimeoutMonitor.CheckTimeouts(CancellationToken.None);

    // Assert
    var updatedOpp = await OpportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
    Assert.Equal(OpportunityStatus.Negotiation, updatedOpp.Status);
    Assert.Null(updatedOpp.ConversionId);
}
```

---

## 📚 CONTEXT

**ADR-063:** Full architectural decision document
**Pattern:** MSG-BACKEND-451 (cross-module integration reference)
**CRM Module Status:** Backend + Frontend complete (79% tasks)
**Sales Module Status:** Not yet implemented (Phase 2 pending)

**Related Messages:**
- MSG-ARCHITECT-865 DONE (ADR-063 design)
- MSG-BACKEND-451 DONE (cross-module pattern reference)

---

## 🔍 VERIFICATION CHECKLIST

- [ ] 3 events defined in Contracts module
- [ ] Opportunity FSM: Converting state + QuoteRef + ConversionId
- [ ] ConvertOpportunityToQuoteCommandHandler implemented
- [ ] 2 event handlers: QuoteCreated, QuoteCreationFailed
- [ ] API endpoint: POST /api/crm/opportunities/{id}/convert-to-quote (202 Accepted)
- [ ] API endpoint: GET /api/crm/conversions/{conversionId} (polling)
- [ ] ConversionTimeoutMonitor background service
- [ ] 3 integration tests: Happy path, Idempotent, Timeout
- [ ] Existing CRM tests still pass
- [ ] Build: 0 errors, minimal warnings

---

## 🚀 NEXT STEPS AFTER THIS DONE

1. **Phase 2 (Sales Module):** Implement Sales-side handler (20-30 NWT)
   - OpportunityConvertedToQuoteEventHandler
   - Quote.CreateFromOpportunity() factory
   - Publish QuoteCreated/CreationFailed events

2. **E2E Testing:** Full integration test (CRM → Sales → CRM roundtrip)

---

**Conductor Note:** Phase 1 only. Sales Phase 2 will be dispatched separately when Sales module is prioritized. For now, test with manual event publishing (integration tests simulate Sales responses).

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
