---
processed: 2026-07-06
id: MSG-BACKEND-151
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-CRM
estimated_nwt: 60
ref: MSG-CONDUCTOR-085
created: 2026-07-06
content_hash: a04bc5b8310f842d548a3f32f60eae2ddc765774a4f2d09aebd4b3e7451cab0d
---

# CRM Integration Testing — FSM, Repository, E2E

## Context

CRM Week 1 Domain Layer complete (CP-CRM-BACKEND ✅, build fix MSG-150).

**Current State:**
- ✅ CRM Domain Layer: Lead, Opportunity, Customer aggregates with FSM
- ✅ Build: 0 errors, 0 warnings (compilation issues resolved)
- ✅ Domain Events: LeadCreatedEvent, OpportunityCreatedEvent, CustomerCreatedEvent
- ✅ Repository contracts: ILeadRepository, IOpportunityRepository, ICustomerRepository

**Need:** Integration tests before Frontend UI dispatch (validate FSM transitions, EF Core persistence, E2E API functionality).

**Why NOW:**
- CRM Backend API complete, Frontend waiting for green light
- FSM validation needed to ensure Lead → Opportunity → Customer flow works correctly
- Repository integration tests establish EF Core + PostgreSQL + RLS pattern for other modules
- E2E API smoke tests validate end-to-end functionality

---

## Task

Implement comprehensive integration tests across 4 categories:

### 1. FSM Transition Tests

Test valid/invalid Lead/Opportunity/Customer FSM transitions:

**Valid Transitions:**
```csharp
[Fact]
public void ConvertToOpportunity_WhenLeadActive_ShouldSucceed()
{
    // Arrange: Create Lead aggregate
    var lead = Lead.Create(tenantId, companyName, contactName, email, phone, source);

    // Act: Convert to Opportunity
    var opportunity = lead.ConvertToOpportunity(estimatedValue, expectedCloseDate);

    // Assert
    opportunity.Should().NotBeNull();
    opportunity.TenantId.Should().Be(tenantId);
    opportunity.CompanyName.Should().Be(companyName);
    // Verify domain event: LeadConvertedToOpportunityEvent
}

[Fact]
public void ConvertToCustomer_WhenOpportunityWon_ShouldSucceed()
{
    // Arrange: Create Opportunity (from Lead conversion)
    // Act: Convert to Customer
    // Assert: Customer created + OpportunityConvertedToCustomerEvent
}
```

**Invalid Transitions:**
```csharp
[Fact]
public void ConvertToLead_WhenOpportunityExists_ShouldThrowDomainException()
{
    // Arrange: Opportunity already exists
    // Act: Try to convert back to Lead (no such method should exist)
    // Assert: DomainException OR method doesn't exist (compile-time safety)
}
```

**Test Coverage:** 5+ FSM tests (valid transitions, invalid transitions, immutability)

---

### 2. Repository Integration Tests (EF Core + Testcontainers)

Test EF Core persistence with PostgreSQL Testcontainers:

**Setup:**
```csharp
public class CRMRepositoryIntegrationTests : IClassFixture<PostgreSqlContainer>
{
    private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

    public CRMRepositoryIntegrationTests(PostgreSqlContainer container)
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(container.ConnectionString)
            .Options;

        // Apply migrations
        using var context = new ApplicationDbContext(_dbOptions);
        context.Database.Migrate();
    }
}
```

**Test Cases:**
```csharp
[Fact]
public async Task SaveLead_WithDomainEvents_ShouldPersistAndPublishEvents()
{
    // Arrange
    using var context = new ApplicationDbContext(_dbOptions);
    var repository = new LeadRepository(context);

    var lead = Lead.Create(tenantId, companyName, contactName, email, phone, source);

    // Act
    await repository.AddAsync(lead);
    await context.SaveChangesAsync();

    // Assert
    var savedLead = await repository.GetByIdAsync(lead.Id);
    savedLead.Should().NotBeNull();
    savedLead.CompanyName.Should().Be(companyName);

    // Verify domain event published
    lead.GetDomainEvents().Should().ContainSingle(e => e is LeadCreatedEvent);
}

[Fact]
public async Task UpdateOpportunity_ShouldPersistChanges()
{
    // Arrange: Create and save Opportunity
    // Act: Update estimated value
    // Assert: Changes persisted correctly
}

[Fact]
public async Task DeleteCustomer_ShouldRemoveFromDatabase()
{
    // Arrange: Create and save Customer
    // Act: Delete
    // Assert: Not found in DB
}
```

**Test Coverage:** 8+ repository tests (CRUD operations, domain events, concurrency)

---

### 3. E2E API Smoke Tests

Test API endpoints end-to-end with real HTTP calls:

**Setup:**
```csharp
public class CRMApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CRMApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
}
```

**Test Cases:**
```csharp
[Fact]
public async Task CreateLead_WithValidData_ShouldReturn201Created()
{
    // Arrange
    var request = new CreateLeadDto
    {
        TenantId = Guid.NewGuid(),
        CompanyName = "Acme Corp",
        ContactName = "John Doe",
        Email = "john@acme.com",
        Phone = "+36301234567",
        Source = "Website"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/crm/leads", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var lead = await response.Content.ReadFromJsonAsync<LeadDto>();
    lead.Should().NotBeNull();
    lead.CompanyName.Should().Be("Acme Corp");
}

[Fact]
public async Task ConvertLeadToOpportunity_WithValidId_ShouldReturn200OK()
{
    // Arrange: Create Lead first
    var leadId = await CreateTestLead();

    var request = new ConvertToOpportunityDto
    {
        EstimatedValue = 50000,
        ExpectedCloseDate = DateTime.UtcNow.AddMonths(3)
    };

    // Act
    var response = await _client.PutAsJsonAsync($"/api/crm/leads/{leadId}/convert-to-opportunity", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var opportunity = await response.Content.ReadFromJsonAsync<OpportunityDto>();
    opportunity.Should().NotBeNull();
    opportunity.EstimatedValue.Should().Be(50000);
}

[Fact]
public async Task GetCustomers_WithTenantFilter_ShouldReturnOnlyTenantData()
{
    // Arrange: Create 2 customers for Tenant A, 1 for Tenant B
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();

    await CreateTestCustomer(tenantA, "Customer A1");
    await CreateTestCustomer(tenantA, "Customer A2");
    await CreateTestCustomer(tenantB, "Customer B1");

    // Act: Query with Tenant A
    var response = await _client.GetAsync($"/api/crm/customers?tenantId={tenantA}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();
    customers.Should().HaveCount(2); // Only Tenant A data
    customers.Should().OnlyContain(c => c.TenantId == tenantA);
}
```

**Test Coverage:** 6+ E2E tests (Create Lead, Convert to Opportunity, Convert to Customer, GET filters)

---

### 4. RLS Policy Tests (Tenant Isolation)

Validate Row-Level Security (RLS) enforcement:

```csharp
[Fact]
public async Task GetLeads_WhenTenantAQueryTenantBData_ShouldReturnEmpty()
{
    // Arrange
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();

    // Create Lead for Tenant B
    await CreateTestLead(tenantB, "Tenant B Lead");

    // Act: Query with Tenant A credentials
    using var context = CreateDbContextWithTenant(tenantA);
    var repository = new LeadRepository(context);
    var leads = await repository.GetAllAsync();

    // Assert: Tenant A should NOT see Tenant B's lead
    leads.Should().BeEmpty();
}

[Fact]
public async Task UpdateLead_WhenDifferentTenant_ShouldThrowUnauthorizedException()
{
    // Arrange: Create Lead for Tenant A
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();
    var leadId = await CreateTestLead(tenantA, "Lead A");

    // Act: Try to update with Tenant B credentials
    using var context = CreateDbContextWithTenant(tenantB);
    var repository = new LeadRepository(context);
    var lead = await repository.GetByIdAsync(leadId);

    // Assert: RLS should block access
    lead.Should().BeNull(); // OR throw UnauthorizedException
}
```

**Test Coverage:** 3+ RLS tests (query isolation, update isolation, delete isolation)

---

## Acceptance Criteria

- ✅ **20+ integration tests** (5 FSM + 8 repository + 6 E2E + 3 RLS)
- ✅ **All tests PASS** (100% pass rate)
- ✅ **RLS policy verified** (tenant isolation works)
- ✅ **E2E API endpoints functional** (Lead → Opportunity → Customer flow works)
- ✅ **Build: 0 errors, 0 warnings**
- ✅ **Testcontainers setup** (PostgreSQL integration tests)

---

## Test Execution Command

```bash
cd /opt/spaceos/spaceos-modules-crm/tests
dotnet test --filter "Category=Integration" --verbosity normal
```

**Expected Output:**
```
Passed!  - Failed: 0, Passed: 20+, Skipped: 0, Total: 20+, Duration: ~30s
```

---

## Files to Create/Modify

**Create:**
- `tests/Integration/FSM/LeadConversionTests.cs` (5 tests)
- `tests/Integration/Repositories/LeadRepositoryTests.cs` (3 tests)
- `tests/Integration/Repositories/OpportunityRepositoryTests.cs` (3 tests)
- `tests/Integration/Repositories/CustomerRepositoryTests.cs` (2 tests)
- `tests/Integration/API/CRMApiTests.cs` (6 tests)
- `tests/Integration/Security/RLSPolicyTests.cs` (3 tests)

**Modify:**
- `tests/SpaceOS.Modules.CRM.Tests.csproj` (add Testcontainers.PostgreSql NuGet)

---

## References

**CRM Domain Layer (Week 1):**
- Build fix: MSG-BACKEND-150 (compilation errors resolved)
- Domain aggregates: Lead, Opportunity, Customer
- Repository contracts: ILeadRepository, IOpportunityRepository, ICustomerRepository

**Similar Patterns:**
- QA Module integration tests (90 tests, Testcontainers pattern)
- Kontrolling Module integration tests (RLS policy tests)

**Integration Spec (for QA module):**
- `/opt/spaceos/docs/joinerytech/integration/QA_PRODUCTION_INTEGRATION_SPEC.md` (Event-Flag Pattern reference)

---

## Estimated Effort

**60 NWT (~2 hours)**

**Breakdown:**
- FSM transition tests: 15 NWT (30 minutes)
- Repository integration tests: 15 NWT (30 minutes)
- E2E API smoke tests: 20 NWT (40 minutes)
- RLS policy tests: 10 NWT (20 minutes)

---

## Next Steps (After DONE)

**Conductor will dispatch:**
1. DMS Week 2 Application Layer → Backend (MSG-BACKEND-152, 120 NWT)
2. HR Week 2 Application Layer → Backend (MSG-BACKEND-153, 150 NWT)
3. Maintenance Week 2 Application Layer → Backend (MSG-BACKEND-154, 150 NWT)

**Frontend readiness:**
- After CRM Integration Testing PASS → Frontend can start CRM UI implementation
- Orval codegen + TanStack Query hooks + React components

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
