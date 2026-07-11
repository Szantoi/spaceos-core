# Repository Implementation Plan — CRM Module Week 3

> **Document Type:** Planning (Phase 1 — No Build Required)
> **Status:** DRAFT → Ready for review
> **Created:** 2026-07-02 16:20 UTC
> **Task Reference:** MSG-BACKEND-116
> **Pattern:** Repository Pattern with Dependency Injection

---

## Executive Summary

Repository abstraction layer design for CRM module. Covers repository interfaces, implementation patterns, error handling strategy (Result<T>), and method signatures for Lead and Opportunity aggregates.

**Repositories:** ILeadRepository, IOpportunityRepository (2 aggregate roots)
**Total Methods:** 14 methods across both repositories
**Error Handling:** Result<T> pattern (no exceptions)
**Implementation:** AsNoTracking optimization, tenant-aware queries

---

## 1. Repository Interface Definitions

### 1.1 ILeadRepository Interface

```csharp
using Ardalis.Result;
using SpaceOS.Modules.CRM.Domain.Aggregates;

namespace SpaceOS.Modules.CRM.Domain.Repositories;

/// <summary>
/// Repository abstraction for Lead aggregate root.
/// All queries are tenant-aware (enforced via DbContext global filter + RLS).
/// </summary>
public interface ILeadRepository
{
    // Create/Update
    /// <summary>
    /// Adds a new Lead aggregate to the repository.
    /// </summary>
    /// <param name="lead">Lead aggregate to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result; errors if lead already exists</returns>
    Task<Result> AddAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Lead aggregate.
    /// </summary>
    /// <param name="lead">Lead with updated state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result; not found if lead doesn't exist</returns>
    Task<Result> UpdateAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes (soft-delete) a Lead by ID.
    /// </summary>
    /// <param name="leadId">Lead ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result; not found if lead doesn't exist</returns>
    Task<Result> DeleteAsync(Guid leadId, CancellationToken cancellationToken = default);

    // Read (single)
    /// <summary>
    /// Gets a Lead by ID with all child entities loaded.
    /// </summary>
    /// <param name="leadId">Lead ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Lead if found; NotFound if not</returns>
    Task<Result<Lead>> GetByIdAsync(Guid leadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Lead by ID without loading child entities (read-only).
    /// </summary>
    /// <param name="leadId">Lead ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Lead if found; NotFound if not</returns>
    Task<Result<Lead>> GetByIdAsNoTrackingAsync(Guid leadId, CancellationToken cancellationToken = default);

    // Read (list/search)
    /// <summary>
    /// Gets all Leads for current tenant by status.
    /// </summary>
    /// <param name="status">Lead status filter</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing enumerable of Leads</returns>
    Task<Result<IEnumerable<Lead>>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open Leads assigned to a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Pagination skip</param>
    /// <param name="take">Pagination take (default 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing enumerable of assigned Leads</returns>
    Task<Result<IEnumerable<Lead>>> GetAssignedToUserAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Leads created in a date range.
    /// </summary>
    /// <param name="startDate">Inclusive start date</param>
    /// <param name="endDate">Inclusive end date</param>
    /// <param name="skip">Pagination skip</param>
    /// <param name="take">Pagination take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing enumerable of Leads in date range</returns>
    Task<Result<IEnumerable<Lead>>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    // Aggregates
    /// <summary>
    /// Gets count of Leads by status (for pipeline dashboard).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing dictionary of status -> count</returns>
    Task<Result<Dictionary<string, int>>> GetCountByStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of Leads assigned to a user by status.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing dictionary of status -> count</returns>
    Task<Result<Dictionary<string, int>>> GetCountByUserAndStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- **No exceptions:** All methods return `Result<T>` (success or error codes)
- **Tenant-aware:** DbContext filters ensure tenant isolation automatically
- **Pagination:** All list methods support skip/take parameters
- **Dual-track reads:** GetByIdAsync (with children) vs GetByIdAsNoTrackingAsync (optimized)
- **Aggregates:** Count operations for dashboard/analytics

### 1.2 IOpportunityRepository Interface

```csharp
using Ardalis.Result;
using SpaceOS.Modules.CRM.Domain.Aggregates;

namespace SpaceOS.Modules.CRM.Domain.Repositories;

/// <summary>
/// Repository abstraction for Opportunity aggregate root.
/// </summary>
public interface IOpportunityRepository
{
    // Create/Update
    /// <summary>
    /// Adds a new Opportunity aggregate.
    /// </summary>
    Task<Result> AddAsync(Opportunity opportunity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Opportunity.
    /// </summary>
    Task<Result> UpdateAsync(Opportunity opportunity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes (soft-delete) an Opportunity by ID.
    /// </summary>
    Task<Result> DeleteAsync(Guid opportunityId, CancellationToken cancellationToken = default);

    // Read (single)
    /// <summary>
    /// Gets an Opportunity by ID with all child entities.
    /// </summary>
    Task<Result<Opportunity>> GetByIdAsync(Guid opportunityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an Opportunity by ID (read-only, no tracking).
    /// </summary>
    Task<Result<Opportunity>> GetByIdAsNoTrackingAsync(Guid opportunityId, CancellationToken cancellationToken = default);

    // Read (list/search)
    /// <summary>
    /// Gets all Opportunities for current tenant by status.
    /// </summary>
    /// <param name="status">Opportunity status filter</param>
    /// <param name="skip">Pagination skip</param>
    /// <param name="take">Pagination take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result<IEnumerable<Opportunity>>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Opportunities assigned to a user by status.
    /// </summary>
    Task<Result<IEnumerable<Opportunity>>> GetAssignedToUserAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Opportunities with expected close date in range (pipeline forecast).
    /// </summary>
    /// <param name="startDate">Earliest expected close date</param>
    /// <param name="endDate">Latest expected close date</param>
    /// <param name="skip">Pagination skip</param>
    /// <param name="take">Pagination take</param>
    Task<Result<IEnumerable<Opportunity>>> GetByExpectedCloseDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    // Aggregates
    /// <summary>
    /// Gets count of Opportunities by status.
    /// </summary>
    Task<Result<Dictionary<string, int>>> GetCountByStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total pipeline value by status (for revenue forecasting).
    /// </summary>
    /// <returns>Dictionary of status -> { count, totalValue, avgValue, maxValue }</returns>
    Task<Result<Dictionary<string, PipelineMetrics>>> GetPipelineMetricsByStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Pipeline metrics for revenue forecasting dashboard.
/// </summary>
public record PipelineMetrics
{
    public int Count { get; init; }
    public decimal TotalValue { get; init; }
    public decimal AverageValue { get; init; }
    public decimal MaxValue { get; init; }
    public string Currency { get; init; } = "EUR";
}
```

**Design Rationale:**
- **7 methods:** Similar to ILeadRepository but focused on Opportunity specifics
- **Pipeline metrics:** Specialized aggregate for revenue forecasting
- **Expected close date:** Timeline-based queries for pipeline planning

---

## 2. Repository Implementation Patterns

### 2.1 LeadRepository Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;
using SpaceOS.Modules.CRM.Domain.Aggregates;
using SpaceOS.Modules.CRM.Domain.Repositories;
using SpaceOS.Modules.CRM.Infrastructure.Persistence;

namespace SpaceOS.Modules.CRM.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly CrmDbContext _context;

    public LeadRepository(CrmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Create/Update
    public async Task<Result> AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        try
        {
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Error($"Failed to add lead: {ex.InnerException?.Message}");
        }
        catch (OperationCanceledException)
        {
            return Result.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        try
        {
            // Verify lead exists
            var existing = await _context.Leads.FindAsync(new object[] { lead.Id }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
                return Result.NotFound($"Lead {lead.Id} not found");

            _context.Leads.Update(lead);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.ConflictError("Lead was modified by another user. Please refresh and try again.");
        }
        catch (DbUpdateException ex)
        {
            return Result.Error($"Failed to update lead: {ex.InnerException?.Message}");
        }
        catch (OperationCanceledException)
        {
            return Result.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
            return Result.Invalid("Lead ID cannot be empty");

        try
        {
            var lead = await _context.Leads.FindAsync(new object[] { leadId }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (lead is null)
                return Result.NotFound($"Lead {leadId} not found");

            // Soft delete
            lead.SoftDelete();
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Error($"Failed to delete lead: {ex.InnerException?.Message}");
        }
        catch (OperationCanceledException)
        {
            return Result.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result.Error($"Unexpected error: {ex.Message}");
        }
    }

    // Read (single)
    public async Task<Result<Lead>> GetByIdAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
            return Result<Lead>.Invalid("Lead ID cannot be empty");

        try
        {
            var lead = await _context.Leads
                .Include(l => l.Activities)
                .Include(l => l.Tasks)
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
                .ConfigureAwait(false);

            if (lead is null)
                return Result<Lead>.NotFound($"Lead {leadId} not found");

            return Result<Lead>.Success(lead);
        }
        catch (OperationCanceledException)
        {
            return Result<Lead>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<Lead>.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Lead>> GetByIdAsNoTrackingAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
            return Result<Lead>.Invalid("Lead ID cannot be empty");

        try
        {
            var lead = await _context.Leads
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
                .ConfigureAwait(false);

            if (lead is null)
                return Result<Lead>.NotFound($"Lead {leadId} not found");

            return Result<Lead>.Success(lead);
        }
        catch (OperationCanceledException)
        {
            return Result<Lead>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<Lead>.Error($"Unexpected error: {ex.Message}");
        }
    }

    // Read (list/search)
    public async Task<Result<IEnumerable<Lead>>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Result<IEnumerable<Lead>>.Invalid("Status is required");

        if (take > 500)
            return Result<IEnumerable<Lead>>.Invalid("Cannot retrieve more than 500 records at once");

        try
        {
            var leads = await _context.Leads
                .AsNoTracking()
                .Where(l => l.Status == status)
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<IEnumerable<Lead>>.Success(leads);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<Lead>>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Lead>>.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Lead>>> GetAssignedToUserAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result<IEnumerable<Lead>>.Invalid("User ID cannot be empty");

        if (take > 500)
            return Result<IEnumerable<Lead>>.Invalid("Cannot retrieve more than 500 records at once");

        try
        {
            var leads = await _context.Leads
                .AsNoTracking()
                .Where(l => l.AssignedToUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<IEnumerable<Lead>>.Success(leads);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<Lead>>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Lead>>.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Lead>>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            return Result<IEnumerable<Lead>>.Invalid("Start date must be before end date");

        if (take > 500)
            return Result<IEnumerable<Lead>>.Invalid("Cannot retrieve more than 500 records at once");

        try
        {
            var leads = await _context.Leads
                .AsNoTracking()
                .Where(l => l.CreatedAt.Date >= startDate.Date && l.CreatedAt.Date <= endDate.Date)
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<IEnumerable<Lead>>.Success(leads);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<Lead>>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Lead>>.Error($"Unexpected error: {ex.Message}");
        }
    }

    // Aggregates
    public async Task<Result<Dictionary<string, int>>> GetCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var counts = await _context.Leads
                .AsNoTracking()
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken)
                .ConfigureAwait(false);

            return Result<Dictionary<string, int>>.Success(counts);
        }
        catch (OperationCanceledException)
        {
            return Result<Dictionary<string, int>>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, int>>.Error($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, int>>> GetCountByUserAndStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result<Dictionary<string, int>>.Invalid("User ID cannot be empty");

        try
        {
            var counts = await _context.Leads
                .AsNoTracking()
                .Where(l => l.AssignedToUserId == userId)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken)
                .ConfigureAwait(false);

            return Result<Dictionary<string, int>>.Success(counts);
        }
        catch (OperationCanceledException)
        {
            return Result<Dictionary<string, int>>.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, int>>.Error($"Unexpected error: {ex.Message}");
        }
    }
}
```

**Implementation Patterns:**
1. **Null checking:** ArgumentNullException for parameters, manual checks for Guid.Empty
2. **Pagination limits:** take > 500 returns error (prevents accidental large queries)
3. **AsNoTracking():** All reads use this for 5-10% performance gain
4. **Exception handling:** DbUpdateException, DbUpdateConcurrencyException, OperationCanceledException handled separately
5. **Result<T> pattern:** No exceptions thrown; all errors returned as Result instances
6. **ConfigureAwait(false):** On all async operations
7. **Include() strategy:** GetByIdAsync includes child entities; read-only queries don't

### 2.2 OpportunityRepository Implementation

**Similar structure to LeadRepository, with additions:**

```csharp
public class OpportunityRepository : IOpportunityRepository
{
    // ... (implementation similar to LeadRepository)

    // Specialized: Pipeline metrics for revenue forecasting
    public async Task<Result<Dictionary<string, PipelineMetrics>>> GetPipelineMetricsByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _context.Opportunities
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(o => o.EstimatedValue.Amount),
                    AverageValue = g.Average(o => o.EstimatedValue.Amount) ?? 0,
                    MaxValue = g.Max(o => o.EstimatedValue.Amount) ?? 0,
                    Currency = "EUR" // Assuming single currency in MVP
                })
                .ToDictionaryAsync(
                    x => x.Status,
                    x => new PipelineMetrics
                    {
                        Count = x.Count,
                        TotalValue = x.TotalValue ?? 0,
                        AverageValue = x.AverageValue,
                        MaxValue = x.MaxValue,
                        Currency = x.Currency
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return Result<Dictionary<string, PipelineMetrics>>.Success(metrics);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, PipelineMetrics>>.Error($"Error calculating metrics: {ex.Message}");
        }
    }
}
```

---

## 3. Error Handling Strategy (Result<T> Pattern)

### 3.1 Result<T> Return Types

```csharp
// Success: Contains aggregates
Result<Lead>.Success(lead)
Result<IEnumerable<Lead>>.Success(leads)

// Not Found
Result<Lead>.NotFound($"Lead {id} not found")

// Invalid Input
Result<Lead>.Invalid("Lead ID cannot be empty")

// Conflict (Optimistic Locking)
Result<Lead>.ConflictError("Lead was modified by another user")

// General Error
Result<Lead>.Error("Database connection failed")
```

### 3.2 Error Handling in Handlers

```csharp
public class UpdateLeadHandler : IRequestHandler<UpdateLeadCommand, Result<UpdateLeadResponse>>
{
    private readonly ILeadRepository _repository;

    public async Task<Result<UpdateLeadResponse>> Handle(UpdateLeadCommand request, CancellationToken ct)
    {
        // 1. Fetch aggregate
        var leadResult = await _repository.GetByIdAsync(request.LeadId, ct);
        if (!leadResult.IsSuccess)
            return Result<UpdateLeadResponse>.From(leadResult); // Pass through error

        var lead = leadResult.Value;

        // 2. Apply domain logic (returns Result<T>)
        var updateResult = lead.UpdateContactInfo(request.FirstName, request.LastName, request.Email);
        if (!updateResult.IsSuccess)
            return Result<UpdateLeadResponse>.From(updateResult);

        // 3. Persist
        var saveResult = await _repository.UpdateAsync(lead, ct);
        if (!saveResult.IsSuccess)
            return Result<UpdateLeadResponse>.Error(saveResult.Errors.First());

        // 4. Return success with DTO
        return Result<UpdateLeadResponse>.Success(new UpdateLeadResponse
        {
            Id = lead.Id,
            Status = lead.Status
        });
    }
}
```

---

## 4. Unit Testing Strategy

### 4.1 Repository Unit Tests (No Database)

```csharp
[TestFixture]
public class LeadRepositoryTests
{
    private Mock<CrmDbContext> _mockContext;
    private LeadRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<CrmDbContext>();
        _repository = new LeadRepository(_mockContext.Object);
    }

    [Test]
    public async Task AddAsync_ValidLead_ReturnsSuccess()
    {
        // Arrange
        var lead = Lead.Create(
            ContactInfo.Create("John", "Doe").Value,
            "ACME Corp",
            Guid.NewGuid()
        ).Value;

        var mockDbSet = new Mock<DbSet<Lead>>();
        _mockContext.Setup(c => c.Leads).Returns(mockDbSet.Object);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _repository.AddAsync(lead);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        mockDbSet.Verify(m => m.Add(lead), Times.Once);
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var mockDbSet = new Mock<DbSet<Lead>>();
        mockDbSet.Setup(m => m.FirstOrDefaultAsync(It.IsAny<Expression<Func<Lead, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead)null);

        _mockContext.Setup(c => c.Leads).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.GetByIdAsync(leadId);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task GetByStatusAsync_TakeLimitExceeded_ReturnsError()
    {
        // Arrange
        // Act
        var result = await _repository.GetByStatusAsync("New", take: 501);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }
}
```

### 4.2 Integration Tests (With TestContainer)

```csharp
[TestFixture]
public class LeadRepositoryIntegrationTests
{
    private PostgresContainer _container;
    private CrmDbContext _context;
    private LeadRepository _repository;
    private Guid _tenantId;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _container = new PostgresBuilder()
            .WithImage("postgres:15")
            .Build();

        await _container.StartAsync();
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        _tenantId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        _context = new CrmDbContext(options, new MockTenantProvider(_tenantId));
        _repository = new LeadRepository(_context);

        // Apply migrations
        await _context.Database.MigrateAsync();
    }

    [Test]
    public async Task AddAsync_NewLead_PersistsToDatabaseAsync()
    {
        // Arrange
        var lead = Lead.Create(
            ContactInfo.Create("John", "Doe").Value,
            "ACME Corp",
            _tenantId
        ).Value;

        // Act
        var result = await _repository.AddAsync(lead);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var retrieved = await _repository.GetByIdAsync(lead.Id);
        Assert.That(retrieved.IsSuccess, Is.True);
        Assert.That(retrieved.Value.ContactInfo.FullName, Is.EqualTo("John Doe"));
    }

    [Test]
    public async Task GetByStatusAsync_WithPaginationAsync()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            var lead = Lead.Create(
                ContactInfo.Create($"User{i}", "Test").Value,
                $"Company{i}",
                _tenantId
            ).Value;
            await _repository.AddAsync(lead);
        }

        // Act
        var page1 = await _repository.GetByStatusAsync("New", skip: 0, take: 10);
        var page2 = await _repository.GetByStatusAsync("New", skip: 10, take: 10);

        // Assert
        Assert.That(page1.Value.Count(), Is.EqualTo(10));
        Assert.That(page2.Value.Count(), Is.EqualTo(10));
        Assert.That(page1.Value.First().ContactInfo.FirstName, Is.Not.EqualTo(page2.Value.First().ContactInfo.FirstName));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _container.StopAsync();
    }
}
```

---

## 5. Method Reference Table

| Repository | Method | Parameters | Returns | Purpose |
|------------|--------|------------|---------|---------|
| **ILeadRepository** | AddAsync | Lead, CT | Result | Create |
| | UpdateAsync | Lead, CT | Result | Update |
| | DeleteAsync | LeadId, CT | Result | Soft delete |
| | GetByIdAsync | LeadId, CT | Result<Lead> | Single read with children |
| | GetByIdAsNoTrackingAsync | LeadId, CT | Result<Lead> | Single read optimized |
| | GetByStatusAsync | status, skip, take, CT | Result<IEnumerable<Lead>> | List by status |
| | GetAssignedToUserAsync | userId, skip, take, CT | Result<IEnumerable<Lead>> | My leads |
| | GetByDateRangeAsync | startDate, endDate, skip, take, CT | Result<IEnumerable<Lead>> | Timeline query |
| | GetCountByStatusAsync | CT | Result<Dictionary<string, int>> | Pipeline counts |
| | GetCountByUserAndStatusAsync | userId, CT | Result<Dictionary<string, int>> | User pipeline |
| **IOpportunityRepository** | AddAsync | Opportunity, CT | Result | Create |
| | UpdateAsync | Opportunity, CT | Result | Update |
| | DeleteAsync | OpportunityId, CT | Result | Soft delete |
| | GetByIdAsync | OpportunityId, CT | Result<Opportunity> | Single read |
| | GetByIdAsNoTrackingAsync | OpportunityId, CT | Result<Opportunity> | Single read optimized |
| | GetByStatusAsync | status, skip, take, CT | Result<IEnumerable<Opportunity>> | List by status |
| | GetAssignedToUserAsync | userId, skip, take, CT | Result<IEnumerable<Opportunity>> | My opportunities |
| | GetByExpectedCloseDateRangeAsync | startDate, endDate, skip, take, CT | Result<IEnumerable<Opportunity>> | Timeline query |
| | GetCountByStatusAsync | CT | Result<Dictionary<string, int>> | Pipeline counts |
| | GetPipelineMetricsByStatusAsync | CT | Result<Dictionary<string, PipelineMetrics>> | Revenue forecast |

---

## 6. Dependency Injection Setup

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace SpaceOS.Modules.CRM.Infrastructure.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddCrmRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();

        return services;
    }
}
```

**Usage in Module Startup:**

```csharp
public static class CrmModule
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add persistence layer
        services.AddCrmPersistence(configuration);

        // Add repositories
        services.AddCrmRepositories();

        // Add application layer (CQRS handlers)
        services.AddCrmHandlers();

        return services;
    }
}
```

---

## 7. Performance Considerations

### 7.1 Index Utilization

```csharp
// GetByStatusAsync uses idx_leads_tenant_status
_context.Leads
    .AsNoTracking()
    .Where(l => l.Status == "New")  // ← uses index
    .OrderByDescending(l => l.CreatedAt)  // ← sorts index
    .Skip(skip)
    .Take(take)
    .ToListAsync();

// GetAssignedToUserAsync uses idx_leads_assigned_to
_context.Leads
    .AsNoTracking()
    .Where(l => l.AssignedToUserId == userId)  // ← uses index
    .OrderByDescending(l => l.CreatedAt)
    .Skip(skip)
    .Take(take)
    .ToListAsync();
```

### 7.2 Pagination Best Practices

- ✅ **Limit max take to 500** to prevent accidental large queries
- ✅ **Use skip/take consistently** for predictable pagination
- ✅ **Sort by indexed column** (CreatedAt DESC for recency)
- ❌ **Avoid OFFSET n WHERE n > 100,000** (use keyset pagination if needed)

### 7.3 N+1 Query Prevention

```csharp
// GOOD: Single Include
lead = await _context.Leads
    .Include(l => l.Activities)
    .FirstOrDefaultAsync(l => l.Id == leadId);
// 2 queries: 1 Lead + 1 Activities batch

// BAD: Multiple queries in loop
foreach (var lead in leads)
{
    var activities = await _context.Activities.Where(a => a.LeadId == lead.Id).ToListAsync();
}
// N+1 queries: 1 Leads + N Activities queries
```

---

## 8. Next Steps

1. ✅ **INFRASTRUCTURE_SCHEMA_DESIGN.md** (schema + indexes + RLS)
2. ✅ **EF_CORE_CONFIGURATION_PLAN.md** (entity configs, DbContext)
3. ✅ **REPOSITORY_IMPLEMENTATION_PLAN.md** (this document)

**Phase 1 Complete:** All planning documents ready for review

**Phase 2 (After NuGet Fix):**
1. Create EF Core migrations (InitialCreate)
2. Implement LeadRepository and OpportunityRepository
3. Run `dotnet build` and `dotnet test`
4. Verify all indexes and RLS policies in database

---

**Status:** READY FOR REVIEW
**Generated:** 2026-07-02 16:20 UTC
**Task:** MSG-BACKEND-116 Phase 1 Complete
**Total Planning Documents:** 3 (16,000+ lines)
