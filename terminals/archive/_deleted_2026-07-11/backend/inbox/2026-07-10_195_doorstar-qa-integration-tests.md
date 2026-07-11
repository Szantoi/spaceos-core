---
processed: 2026-07-10
id: MSG-BACKEND-195
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-194
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 30
created: 2026-07-10
content_hash: f30b6335d48cba49a33a4df60384541fd8d3724fece111f05a5bfd106d5f3be4
---

# Doorstar Production QA — Integration Tests (Phase 2)

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Backend Spec:** MSG-BACKEND-194 (DONE 2026-07-08)
**Timeline:** 30 NWT (~1 óra)
**Scope:** E2E integration tests for Production Workflow module

---

## 🎯 OBJECTIVE

Implementáld a **Backend MSG-BACKEND-194 Section 2.5** integration test suite-ot:
- 4 E2E test cases (event-driven workflow)
- Testcontainers PostgreSQL + RabbitMQ
- ProductionTestBase infrastructure

---

## 📋 TEST CASES (Section 2.5 - Backend Spec)

### 1. E2E_OrderConfirmed_CreatesProductionJob

**Given:** OrderConfirmed event published
**When:** Event handler processes event
**Then:**
- [ ] ProductionJob created with 6 steps
- [ ] First step is "Szabászat/Előgyártás" (Queued)
- [ ] ProductionJob.status = "Queued"
- [ ] ProductionJob.orderId matches event.orderId

**Test Code:**
```csharp
[Fact]
public async Task E2E_OrderConfirmed_CreatesProductionJob()
{
    // Arrange
    var orderConfirmedEvent = new OrderConfirmedEvent
    {
        OrderId = "ORDER-123",
        ProjectName = "DSMR 26144",
        Deadline = DateTime.UtcNow.AddDays(30)
    };

    // Act
    await PublishEvent(orderConfirmedEvent);
    await WaitForEventProcessing(); // 500ms delay

    // Assert
    var productionJob = await _repository.GetByOrderIdAsync("ORDER-123");
    productionJob.Should().NotBeNull();
    productionJob.WorkflowSteps.Should().HaveCount(6);
    productionJob.WorkflowSteps[0].StepName.Should().Be("Szabászat/Előgyártás");
    productionJob.Status.Should().Be(ProductionStatus.Queued);
}
```

---

### 2. E2E_CuttingCompleted_AutoCompletesSzabaszat

**Given:** ProductionJob exists with OrderId correlation
**When:** CuttingCompleted event published
**Then:**
- [ ] "Szabászat/Előgyártás" step marked Done
- [ ] `completedBy` = "auto:CuttingCompleted"
- [ ] WorkflowStepCompleted event published
- [ ] ProductionJob.currentStepIndex = 1 (advanced to next step)

**Test Code:**
```csharp
[Fact]
public async Task E2E_CuttingCompleted_AutoCompletesSzabaszat()
{
    // Arrange
    var productionJob = await CreateTestProductionJob("ORDER-456");

    var cuttingCompletedEvent = new CuttingCompletedEvent
    {
        OrderId = "ORDER-456",
        CompletedAt = DateTime.UtcNow
    };

    // Act
    await PublishEvent(cuttingCompletedEvent);
    await WaitForEventProcessing();

    // Assert
    var updatedJob = await _repository.GetByIdAsync(productionJob.Id);
    var szabaszatStep = updatedJob.WorkflowSteps[0];
    szabaszatStep.Status.Should().Be(StepStatus.Done);
    szabaszatStep.CompletedBy.Should().Be("auto:CuttingCompleted");
    updatedJob.CurrentStepIndex.Should().Be(1);
}
```

---

### 3. E2E_6StageManualCompletion_PublishesShippingReady

**Given:** ProductionJob with 6 steps
**When:** Steps 1-6 completed manually
**Then:**
- [ ] After step 6 completion, `ProductionJobShippingReady` event published
- [ ] ProductionJob.status = "ShippingReady"
- [ ] All 6 steps marked Done

**Test Code:**
```csharp
[Fact]
public async Task E2E_6StageManualCompletion_PublishesShippingReady()
{
    // Arrange
    var productionJob = await CreateTestProductionJob("ORDER-789");
    var eventSpy = new EventSpy<ProductionJobShippingReadyEvent>();

    // Act
    for (int i = 0; i < 6; i++)
    {
        await CompleteStep(productionJob.Id, productionJob.WorkflowSteps[i].Id, $"user:test-{i}");
        await WaitForEventProcessing();
    }

    // Assert
    var updatedJob = await _repository.GetByIdAsync(productionJob.Id);
    updatedJob.Status.Should().Be(ProductionStatus.ShippingReady);
    updatedJob.WorkflowSteps.Should().AllSatisfy(step => step.Status.Should().Be(StepStatus.Done));

    eventSpy.PublishedEvents.Should().ContainSingle();
    eventSpy.PublishedEvents[0].ProductionJobId.Should().Be(productionJob.Id);
}
```

---

### 4. E2E_ShippingReady_SendsNotification

**Given:** ProductionJob completed
**When:** ProductionJobShippingReady event published
**Then:**
- [ ] Notification sent (Telegram/email mock verification)
- [ ] Notification contains project name (e.g., "🚀 DSMR 26144 kiszállítható!")

**Test Code:**
```csharp
[Fact]
public async Task E2E_ShippingReady_SendsNotification()
{
    // Arrange
    var productionJob = await CreateTestProductionJob("ORDER-999");
    var notificationSpy = new NotificationSpy();

    // Act
    await CompleteAllSteps(productionJob.Id);
    await WaitForEventProcessing();

    // Assert
    notificationSpy.SentNotifications.Should().ContainSingle();
    var notification = notificationSpy.SentNotifications[0];
    notification.Message.Should().Contain("kiszállítható");
    notification.Message.Should().Contain(productionJob.ProjectName);
}
```

---

## 🔧 TEST INFRASTRUCTURE

### ProductionTestBase Class

**File:** `SpaceOS.Modules.Production.Tests/Integration/ProductionTestBase.cs`

**Implementation:**
```csharp
public class ProductionTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected readonly RabbitMqContainer _rabbitMqContainer;
    protected readonly IProductionJobRepository _repository;
    protected readonly IEventBus _eventBus;

    public ProductionTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("production_test")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        // Setup DbContext, Repository, EventBus
        var connectionString = _postgresContainer.GetConnectionString();
        // ... (DI setup)
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    protected async Task PublishEvent<T>(T @event) where T : class
    {
        await _eventBus.PublishAsync(@event);
    }

    protected async Task WaitForEventProcessing(int milliseconds = 500)
    {
        await Task.Delay(milliseconds);
    }

    protected async Task<ProductionJob> CreateTestProductionJob(string orderId)
    {
        var job = new ProductionJob(
            orderId: orderId,
            projectName: "TEST-" + orderId,
            deadline: DateTime.UtcNow.AddDays(30),
            workflowSteps: ProductionJobFactory.CreateDefaultSteps()
        );
        await _repository.AddAsync(job);
        return job;
    }

    protected async Task CompleteStep(Guid jobId, Guid stepId, string completedBy)
    {
        var job = await _repository.GetByIdAsync(jobId);
        var step = job.WorkflowSteps.First(s => s.Id == stepId);
        step.MarkAsCompleted(completedBy);
        await _repository.UpdateAsync(job);
    }
}
```

---

### ProductionJobFactory

**File:** `SpaceOS.Modules.Production.Tests/Factories/ProductionJobFactory.cs`

**Implementation:**
```csharp
public static class ProductionJobFactory
{
    public static List<WorkflowStep> CreateDefaultSteps()
    {
        return new List<WorkflowStep>
        {
            new WorkflowStep("Szabászat/Előgyártás", estimatedDuration: TimeSpan.FromDays(1)),
            new WorkflowStep("Megmunkálás", estimatedDuration: TimeSpan.FromDays(2)),
            new WorkflowStep("Felületkezelés", estimatedDuration: TimeSpan.FromDays(1)),
            new WorkflowStep("Összeszerelés", estimatedDuration: TimeSpan.FromDays(2)),
            new WorkflowStep("Csomagolás", estimatedDuration: TimeSpan.FromHours(4)),
            new WorkflowStep("Kiszállítható", estimatedDuration: TimeSpan.FromHours(1))
        };
    }
}
```

---

## ✅ ACCEPTANCE CRITERIA

### Test Coverage
- [ ] 4 E2E test cases implemented (OrderConfirmed, CuttingCompleted, 6StageCompletion, ShippingReady)
- [ ] All tests pass (100% green)
- [ ] Testcontainers PostgreSQL + RabbitMQ working

### Infrastructure
- [ ] ProductionTestBase class (async lifetime, DI setup)
- [ ] ProductionJobFactory (test data generation)
- [ ] EventSpy, NotificationSpy (mock verification)

### Integration
- [ ] Event-driven workflow tested (OrderConfirmed → ProductionJob creation)
- [ ] Auto-complete tested (CuttingCompleted → Szabászat step Done)
- [ ] Notification tested (ShippingReady → Telegram/email mock)

---

## 📊 TIMELINE

**Estimated:** 30 NWT (~1 óra)

| Task | NWT |
|------|-----|
| ProductionTestBase infrastructure | 10 NWT |
| 4 E2E test cases implementation | 15 NWT |
| EventSpy, NotificationSpy mocks | 5 NWT |

---

## 🚧 BLOCKERS

**Dependencies:**
- Backend MSG-BACKEND-194 Domain/Application/Infrastructure layers must be implemented
- Testcontainers NuGet packages installed

**If blocked:** Wait for Backend implementation DONE, then proceed with QA tests.

---

## 📁 FILES TO CREATE

```
SpaceOS.Modules.Production.Tests/
  Integration/
    ProductionTestBase.cs
    E2E_OrderConfirmed_CreatesProductionJob.cs
    E2E_CuttingCompleted_AutoCompletesSzabaszat.cs
    E2E_6StageManualCompletion_PublishesShippingReady.cs
    E2E_ShippingReady_SendsNotification.cs
  Factories/
    ProductionJobFactory.cs
  Mocks/
    EventSpy.cs
    NotificationSpy.cs
```

---

## 🎯 SUCCESS METRICS

- ✅ 4 E2E tests pass (100% green)
- ✅ Testcontainers PostgreSQL + RabbitMQ working
- ✅ Event-driven workflow verified (OrderConfirmed → ShippingReady)
- ✅ Auto-complete logic tested (CuttingCompleted → Szabászat Done)
- ✅ No flaky tests (deterministic event processing)

---

**Parallel coordination:** QA integration tests validate Backend + Frontend integration.

---

📋 Generated by Conductor Terminal — Doorstar Phase 2 QA Integration Dispatch (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
