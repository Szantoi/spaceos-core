---
id: MSG-ARCHITECT-002
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-INTEGRATION
estimated_nwt: 30
ref: MSG-CONDUCTOR-085
created: 2026-07-06
content_hash: cda275e33f97e56db7b528d416fa72d9b9ec0f95f1c840c3f10bf987b1cb164b
---

# QA → Production Integration Architecture

## Context

QA Week 1 Domain Layer complete (90 tests, InspectionBlockingService implemented).

**Current State:**
- ✅ QA Domain Layer: Checkpoint, Inspection, Ticket aggregates with FSM
- ✅ InspectionBlockingService: Domain service for blocking logic
- ✅ 90/90 tests PASS, build SUCCESS
- ✅ Production module exists (needs integration wiring)

**Need:** Integration spec for Production Order blocking when QA Inspection fails (Critical level).

**Why CRITICAL:**
- QA Inspection failures must block Production Order progression (FSM transition)
- Cross-module event handler wiring pattern needed (template for future integrations)
- Production capacity planning depends on QA blocking logic
- This pattern will be reused for: HR → Production (blocking absences), Maintenance → Production (downtime tracking)

---

## Task

Create comprehensive integration architecture specification:

### 1. Event Flow Diagram

Design the event flow from QA Inspection failure to Production Order blocked state:

```
InspectionCompletedEvent (Result=Fail, CriticalLevel=Critical)
    ↓
InspectionBlockingService.IsProductionBlocked()
    ↓
ProductionOrderBlockedEvent
    ↓
Order FSM transition (InProgress → Blocked)
```

**Deliverable:** Mermaid sequence diagram showing:
- QA module: Inspection aggregate raises InspectionCompletedEvent
- QA domain service: InspectionBlockingService evaluates blocking condition
- Production module: Event handler receives event → publishes ProductionOrderBlockedEvent
- Production aggregate: Order FSM transitions to Blocked state

### 2. Event Handler Pattern

Design the event handler implementation template:

```csharp
// Production Module — Application Layer
public class InspectionCompletedEventHandler : INotificationHandler<InspectionCompletedEvent>
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IInspectionBlockingService _blockingService; // from QA module

    public async Task Handle(InspectionCompletedEvent evt, CancellationToken ct)
    {
        // Step 1: Check if inspection blocks production
        if (_blockingService.IsProductionBlocked(evt.Result, evt.CriticalLevel))
        {
            // Step 2: Query Production Order by evt.OrderId
            var order = await _orderRepository.GetByIdAsync(evt.OrderId, ct);

            // Step 3: Invoke domain method (FSM transition)
            order.BlockDueToQualityIssue(evt.InspectionId, evt.FailureNotes);

            // Step 4: Persist + publish ProductionOrderBlockedEvent
            await _orderRepository.SaveChangesAsync(ct);
        }
    }
}
```

**Questions to Answer:**
1. Should InspectionBlockingService be injected into Production module? (Cross-module dependency)
2. OR should the event contain computed `BlocksProduction` flag? (Push logic to QA module)
3. MediatR pipeline handler or direct domain event handler?
4. How to handle event handler failures? (Retry policy, dead letter queue)

### 3. Unit Test Pattern

Provide unit test template for event handler:

```csharp
public class InspectionCompletedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenInspectionFailsCritical_ShouldBlockProductionOrder()
    {
        // Arrange
        var evt = new InspectionCompletedEvent(
            InspectionId: InspectionId.New(),
            OrderId: OrderId.New(),
            Result: InspectionResult.Fail,
            CriticalLevel: CriticalLevel.Critical,
            FailureNotes: "Critical defect detected"
        );

        // Mock dependencies
        var orderRepository = new Mock<IProductionOrderRepository>();
        var blockingService = new Mock<IInspectionBlockingService>();
        blockingService.Setup(s => s.IsProductionBlocked(evt.Result, evt.CriticalLevel))
                       .Returns(true);

        var handler = new InspectionCompletedEventHandler(orderRepository.Object, blockingService.Object);

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        orderRepository.Verify(r => r.GetByIdAsync(evt.OrderId, It.IsAny<CancellationToken>()), Times.Once);
        // Verify Order.BlockDueToQualityIssue() called
        // Verify SaveChangesAsync called (domain event published)
    }

    [Fact]
    public async Task Handle_WhenInspectionFailsMinor_ShouldNotBlockProductionOrder()
    {
        // Arrange: Result=Fail, CriticalLevel=Minor
        // Assert: Order NOT blocked (InspectionBlockingService returns false)
    }
}
```

### 4. Integration Test Pattern

Provide end-to-end integration test template:

```csharp
public class QAProductionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CompleteInspection_WhenFailCritical_ShouldBlockProductionOrder()
    {
        // Arrange: Production Order InProgress + QA Inspection Planned
        // 1. Create Production Order (POST /api/production/orders) → InProgress
        // 2. Create QA Checkpoint (POST /api/qa/checkpoints) → linked to OrderId
        // 3. Plan QA Inspection (POST /api/qa/inspections) → Planned status

        // Act: Complete Inspection with Fail + Critical
        // PUT /api/qa/inspections/{id}/complete
        // { "result": "Fail", "criticalLevel": "Critical", "failureNotes": "..." }

        // Assert: Order FSM transitions to Blocked state
        // GET /api/production/orders/{id} → status: "Blocked"
        // GET /api/production/orders/{id}/events → contains ProductionOrderBlockedEvent
    }
}
```

---

## Deliverables

1. **Architecture Diagram (Mermaid):**
   - Sequence diagram showing event flow (QA → Production)
   - Component diagram showing module dependencies

2. **Event Handler Implementation Template:**
   - Full C# code for `InspectionCompletedEventHandler`
   - Recommendation: MediatR pipeline or direct domain event handler?
   - Dependency injection pattern (how Production module accesses InspectionBlockingService)

3. **Test Patterns:**
   - Unit test template (event handler isolation)
   - Integration test template (end-to-end API test)
   - Coverage recommendation (what to test, what to skip)

4. **Recommendation:**
   - Should InspectionBlockingService be injected cross-module? (Pros/Cons)
   - OR event should contain computed `BlocksProduction` flag? (Pros/Cons)
   - Event handler error handling strategy (retry, dead letter, alert)

---

## Acceptance Criteria

- ✅ Clear event flow documented (QA → Production)
- ✅ Handler pattern reusable for future integrations (HR → Production, Maintenance → Production)
- ✅ Test coverage pattern established (unit + integration)
- ✅ Architectural decision on cross-module dependency (inject service OR event flag)
- ✅ Error handling strategy recommended

---

## References

**QA Domain Layer (Week 1 DONE):**
- `/opt/spaceos/terminals/backend/outbox/2026-07-06_151_qa-week1-compilation-fixes-done.md`
- QA Aggregates: Checkpoint, Inspection, Ticket (with FSM)
- InspectionBlockingService: `bool IsProductionBlocked(InspectionResult, CriticalLevel)`
- Domain Events: InspectionCompletedEvent, InspectionFailedEvent

**Production Module:**
- Exists but integration wiring not yet implemented
- Production Order FSM: Draft → InProgress → Completed | Blocked

**Related Integrations (Future):**
- HR → Production: Blocking absences (Approved/InProgress) block capacity
- Maintenance → Production: Downtime tracking (RequiresDowntime flag)

---

## Estimated Effort

**30 NWT (~1 hour)**

**Breakdown:**
- Architecture diagram: 10 NWT (20 minutes)
- Event handler pattern: 10 NWT (20 minutes)
- Test patterns: 5 NWT (10 minutes)
- Recommendation write-up: 5 NWT (10 minutes)

---

## Next Steps (After DONE)

**Conductor will dispatch:**
1. CRM Integration Testing → Backend (MSG-BACKEND-XXX, 60 NWT)
2. DMS Week 2 Application Layer → Backend (MSG-BACKEND-XXX, 120 NWT)
3. QA Week 2 Application Layer → Backend (using your integration spec, 150 NWT)

**Your output will be used for:**
- Backend QA Week 2 Application Layer implementation (event handler coding)
- Pattern template for HR → Production integration
- Pattern template for Maintenance → Production integration

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
