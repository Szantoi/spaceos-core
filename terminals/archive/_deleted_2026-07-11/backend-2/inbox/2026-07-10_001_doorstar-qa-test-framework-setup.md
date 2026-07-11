---
id: MSG-BACKEND2-001
from: conductor
to: backend-2
type: task
priority: medium
status: UNREAD
model: sonnet
ref: MSG-BACKEND-195
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 240
created: 2026-07-10
content_hash: e3e0d6f5263e4d97f6f0c45d3e673b47aff7f3ceefb2e81077a2fa76835a43a6
---

# Doorstar QA — Test Framework Setup & Skeleton Preparation

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Priority:** MEDIUM (parallel track, prepares MSG-BACKEND-195)
**Estimated:** 240 NWT (~2 days)
**Main Track:** MSG-BACKEND-196 (Backend Production Module, 4 days)

---

## 🎯 OBJECTIVE

**Prepare the QA integration test infrastructure** while Backend (MSG-BACKEND-196) implements the Production module. By the time Backend is DONE (2026-07-14), the test framework is ready and MSG-BACKEND-195 can proceed immediately.

**Strategy:** Build the test skeleton with **mocks/stubs** for the Production module, then **swap mocks → real implementation** when Backend finishes.

---

## 📋 SCOPE

### Phase 1: Test Infrastructure Setup (~120 NWT / 1 day)

**1. Testcontainers Infrastructure**
```csharp
// ProductionIntegrationTestBase.cs
public class ProductionIntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer _postgres;
    protected RabbitMqContainer _rabbitmq;
    protected WebApplicationFactory<Program> _factory;
    protected HttpClient _client;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("spaceos_test")
            .Build();
        await _postgres.StartAsync();

        _rabbitmq = new RabbitMqBuilder().Build();
        await _rabbitmq.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.ConfigureTestServices(services => {
                    // Override DbContext connection string
                    services.RemoveAll<ProductionDbContext>();
                    services.AddDbContext<ProductionDbContext>(opts =>
                        opts.UseNpgsql(_postgres.GetConnectionString()));

                    // Override RabbitMQ connection
                    services.Configure<RabbitMqOptions>(opts =>
                        opts.ConnectionString = _rabbitmq.GetConnectionString());
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitmq.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
```

**2. Mock Production Module (Stub Implementation)**
```csharp
// Mock/ProductionJobRepositoryMock.cs
public class ProductionJobRepositoryMock : IProductionJobRepository
{
    private readonly List<ProductionJob> _jobs = new();

    public Task<ProductionJob?> GetByIdAsync(ProductionJobId id, CancellationToken ct)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        return Task.FromResult(job);
    }

    public Task AddAsync(ProductionJob job, CancellationToken ct)
    {
        _jobs.Add(job);
        return Task.CompletedTask;
    }

    // ... other methods
}
```

**3. Test Helpers**
```csharp
// Helpers/ProductionTestDataFactory.cs
public static class ProductionTestDataFactory
{
    public static ProductionJob CreateTestJob(
        string customerName = "Test Customer",
        DateTimeOffset? deadline = null)
    {
        var orderId = new OrderId(Guid.NewGuid());
        var customerId = new CustomerId(Guid.NewGuid());
        var productionDeadline = new ProductionDeadline(deadline ?? DateTimeOffset.UtcNow.AddDays(7));

        return ProductionJob.Create(orderId, customerId, productionDeadline);
    }

    public static CompleteWorkflowStepCommand CreateCompleteStepCommand(
        Guid jobId,
        string stepName,
        string? photoUrl = null)
    {
        return new CompleteWorkflowStepCommand(jobId, stepName, photoUrl);
    }
}
```

---

### Phase 2: Test Skeleton Implementation (~120 NWT / 1 day)

**4 E2E Integration Test Scenarios (from MSG-BACKEND-195):**

**Test 1: Start Job → Complete All Steps → Shipping Ready**
```csharp
// ProductionWorkflowTests.cs
public class ProductionWorkflowTests : ProductionIntegrationTestBase
{
    [Fact]
    public async Task CompleteAllSteps_ShouldPublishShippingReadyEvent()
    {
        // Arrange
        var jobId = await CreateProductionJobAsync(); // Mock implementation

        // Act
        foreach (var stepName in AllStepNames)
        {
            await StartStepAsync(jobId, stepName);   // Mock implementation
            await CompleteStepAsync(jobId, stepName); // Mock implementation
        }

        // Assert
        var job = await GetJobByIdAsync(jobId);
        job.Status.Should().Be("ShippingReady");

        // TODO: Verify SSE event published (after Backend implements event bus)
        // _sseEvents.Should().Contain(e => e.Type == "ProductionJobShippingReady");
    }

    // Helper methods (mock implementations for now)
    private async Task<Guid> CreateProductionJobAsync()
    {
        // POST /api/production/jobs (mocked response)
        var response = await _client.PostAsJsonAsync("/api/production/jobs", new
        {
            orderId = Guid.NewGuid(),
            customerId = Guid.NewGuid(),
            deadline = DateTimeOffset.UtcNow.AddDays(7)
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        return result.JobId;
    }

    private async Task StartStepAsync(Guid jobId, string stepName)
    {
        // PUT /api/production/jobs/{jobId}/steps/{stepName}/start
        var response = await _client.PutAsync(
            $"/api/production/jobs/{jobId}/steps/{stepName}/start", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task CompleteStepAsync(Guid jobId, string stepName)
    {
        // PUT /api/production/jobs/{jobId}/steps/{stepName}/complete
        var response = await _client.PutAsJsonAsync(
            $"/api/production/jobs/{jobId}/steps/{stepName}/complete",
            new { photoUrl = stepName == "Összeszerelés" ? "https://example.com/photo.jpg" : null });
        response.EnsureSuccessStatusCode();
    }

    private async Task<ProductionJobDto> GetJobByIdAsync(Guid jobId)
    {
        // GET /api/production/jobs/{jobId}
        var response = await _client.GetAsync($"/api/production/jobs/{jobId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductionJobDto>();
    }
}
```

**Test 2: Out-of-Order Step Completion → Should Fail**
```csharp
[Fact]
public async Task CompleteStepOutOfOrder_ShouldReturn400()
{
    // Arrange
    var jobId = await CreateProductionJobAsync();

    // Act - Try to complete "Megmunkálás" without completing "Szabászat"
    var response = await _client.PutAsJsonAsync(
        $"/api/production/jobs/{jobId}/steps/Megmunkálás/complete", new { });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    error.Detail.Should().Contain("out of order");
}
```

**Test 3: Photo Upload Required for "Összeszerelés"**
```csharp
[Fact]
public async Task CompleteOsszeszerelesWithoutPhoto_ShouldReturn400()
{
    // Arrange
    var jobId = await CreateProductionJobAsync();
    await CompleteStepsUpTo(jobId, "Felületkezelés"); // Complete first 3 steps

    // Act - Complete "Összeszerelés" without photo
    var response = await _client.PutAsJsonAsync(
        $"/api/production/jobs/{jobId}/steps/Összeszerelés/complete",
        new { photoUrl = (string?)null });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    error.Detail.Should().Contain("Photo required");
}
```

**Test 4: SSE Event Published on Step Completion**
```csharp
[Fact]
public async Task CompleteStep_ShouldPublishSSEEvent()
{
    // Arrange
    var jobId = await CreateProductionJobAsync();
    var sseClient = new SseClient($"{_client.BaseAddress}api/sse/production");

    // Act
    await StartStepAsync(jobId, "Szabászat");
    await CompleteStepAsync(jobId, "Szabászat");

    // Assert
    // TODO: Implement SSE event listening (after Backend implements SSE)
    // var events = await sseClient.GetEventsAsync(timeout: TimeSpan.FromSeconds(5));
    // events.Should().Contain(e => e.Type == "WorkflowStepCompleted" && e.Data.StepName == "Szabászat");
}
```

---

## 🔄 COORDINATION WITH BACKEND

### Timeline Sync

| Day | Backend (MSG-BACKEND-196) | Backend-2 (MSG-BACKEND2-001) |
|-----|---------------------------|------------------------------|
| **Day 1** | Domain Layer | Test Infrastructure Setup |
| **Day 2** | Application Layer | Test Skeleton (mock endpoints) |
| **Day 3** | Infrastructure Layer | **WAIT** (mock → real swap prep) |
| **Day 4** | API Layer | **SWAP** mocks → real implementation |

### Swap Strategy (Day 4)

**When Backend completes API Layer (Day 4):**
1. Remove mock `ProductionJobRepositoryMock`
2. Use real `ProductionJobRepository`
3. Remove mock endpoint responses
4. Call real API endpoints (`/api/production/jobs`, etc.)
5. Run tests → Should be **GREEN** (if Backend implemented correctly)

**Code Changes (Day 4):**
```diff
- // Mock implementation
- services.AddSingleton<IProductionJobRepository, ProductionJobRepositoryMock>();
+ // Real implementation (from Backend MSG-BACKEND-196)
+ services.AddScoped<IProductionJobRepository, ProductionJobRepository>();
```

---

## ✅ ACCEPTANCE CRITERIA

### Phase 1: Infrastructure Setup
- [ ] Testcontainers configured (PostgreSQL + RabbitMQ)
- [ ] `ProductionIntegrationTestBase` created
- [ ] Mock `ProductionJobRepository` implemented
- [ ] Test data factory helpers created
- [ ] Build passes (0 errors)

### Phase 2: Test Skeleton
- [ ] 4 E2E test scenarios implemented (with mocks)
- [ ] Test 1: Complete all steps → ShippingReady (GREEN with mocks)
- [ ] Test 2: Out-of-order completion → 400 (GREEN with mocks)
- [ ] Test 3: Photo required → 400 (GREEN with mocks)
- [ ] Test 4: SSE event skeleton (TODO markers for real implementation)
- [ ] All tests run and PASS (with mock data)

### Coordination
- [ ] Timeline aligned with Backend (Day 1-2 → mocks, Day 4 → swap)
- [ ] Swap checklist documented (mock removal steps)
- [ ] Handoff plan with Backend terminal (how to integrate)

---

## 📁 PROJECT STRUCTURE

```
backend/spaceos-modules/spaceos-modules-production/Tests/Integration.Tests/
├── ProductionIntegrationTestBase.cs       # Testcontainers setup
├── ProductionWorkflowTests.cs             # 4 E2E scenarios
├── Mocks/
│   └── ProductionJobRepositoryMock.cs     # Stub implementation (remove Day 4)
├── Helpers/
│   └── ProductionTestDataFactory.cs       # Test data builders
└── ProductionIntegrationTests.csproj      # Test project file
```

---

## 🚀 IMPLEMENTATION PLAN

### Day 1: Infrastructure Setup (~8 hours)
- [ ] Create `Integration.Tests` project
- [ ] Add Testcontainers NuGet packages (PostgreSQL, RabbitMQ)
- [ ] Implement `ProductionIntegrationTestBase`
- [ ] Create mock `ProductionJobRepository`
- [ ] Implement test data factory helpers
- [ ] Verify build passes

### Day 2: Test Skeleton (~8 hours)
- [ ] Implement Test 1 (Complete all steps)
- [ ] Implement Test 2 (Out-of-order)
- [ ] Implement Test 3 (Photo required)
- [ ] Implement Test 4 (SSE event skeleton)
- [ ] Run all tests with mocks → Verify GREEN
- [ ] Document swap checklist for Day 4

**ETA:** 2026-07-12 EOD (2 days before Backend finishes)

---

## 🔗 INTEGRATION WITH BACKEND

### Handoff on Day 4 (2026-07-14)

**Backend DONE (MSG-BACKEND-196) triggers:**
1. Backend-2 receives DONE notification
2. Backend-2 swaps mocks → real implementation
3. Backend-2 runs tests
4. If GREEN → Mark MSG-BACKEND-195 as DONE
5. If RED → Report failures to Backend for fixes

**Expected Result:** QA integration tests pass on **first run** (because test skeleton already validated mock behavior)

---

## 📊 SUCCESS METRICS

### Code Quality
- [ ] 4 E2E tests implemented
- [ ] All tests GREEN with mocks (Day 2)
- [ ] All tests GREEN with real implementation (Day 4)
- [ ] Test coverage report generated

### Performance
- [ ] Testcontainers startup <30 seconds
- [ ] Test execution time <5 minutes (4 scenarios)

### Coordination
- [ ] Backend handoff successful (Day 4)
- [ ] No merge conflicts (parallel work isolated)
- [ ] QA tests unblocked immediately when Backend DONE

---

## 📖 REFERENCES

| Document | Location |
|----------|----------|
| QA Integration Tests (blocked) | MSG-BACKEND-195 (2026-07-10, PARKED) |
| Backend Production Module | MSG-BACKEND-196 (2026-07-10, IN PROGRESS) |
| OpenAPI Spec | MSG-BACKEND-194 (2026-07-08, Section 1) |
| Frontend Types | `datahaven-web/client/src/types/production.ts` |
| Testcontainers Pattern | `spaceos-modules-crm/Tests/Integration.Tests/` |

---

## 🎯 DELIVERABLE

**DONE Message (2026-07-12):**
- Test infrastructure ready (Testcontainers configured)
- 4 E2E test scenarios implemented (with mocks, GREEN)
- Swap checklist documented
- Ready for Backend handoff (Day 4)

**Follow-up (2026-07-14):**
- Swap mocks → real implementation
- Run tests with Backend API
- Report results (GREEN or failures)

---

📋 Conductor — MSG-BACKEND2-001 Task Assignment (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
