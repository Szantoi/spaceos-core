// SpaceOS.Kernel.IntegrationTests/FlowEpics/FlowEpicRepositoryTests.cs
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.FlowEpics;

/// <summary>
/// Integration tests that verify <see cref="FlowEpicRepository"/> behaviour against a real
/// in-memory SQLite schema created by <see cref="RepositoryTestBase"/>.
/// Covers CRUD operations, the <see cref="B2BHandshake"/> owned-entity round-trip,
/// execution-state transitions, specification-filtered list queries, and <c>AsNoTracking</c>
/// read paths.
/// </summary>
public sealed class FlowEpicRepositoryTests : RepositoryTestBase
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seeds the minimal prerequisite chain (Tenant → Facility) and returns the seeded
    /// <see cref="FacilityId"/> and <see cref="TenantId"/> ready for use in <see cref="FlowEpic"/> creation.
    /// </summary>
    /// <param name="tenantName">Display name for the seeded tenant.</param>
    /// <param name="facilityName">Display name for the seeded facility.</param>
    /// <returns>A tuple of the <see cref="FacilityId"/> and <see cref="TenantId"/> of the persisted facility and tenant.</returns>
    private async Task<(FacilityId FacilityId, TenantId TenantId)> SeedFacilityChainAsync(
        string tenantName, string facilityName)
    {
        var tenant = Tenant.Create(tenantName);
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create(facilityName, tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return (facility.Id, tenant.Id);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.GetByIdAsync"/> returns the correct entity
    /// when the requested identifier exists in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingFlowEpic_ReturnsFlowEpic()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Acme Corp", "Main Hall");
        var epic = FlowEpic.Create("Baseline Epic", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FlowEpicRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(epic.Id, result.Id);
        Assert.Equal("Baseline Epic", result.Title.Value);
        Assert.Equal(facilityId, result.TargetFacilityId);
    }

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.GetByIdAsync"/> returns <see langword="null"/>
    /// when the requested identifier does not exist in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownFlowEpicId_ReturnsNull()
    {
        // Arrange
        var unknownId = FlowEpicId.New();
        var repository = new FlowEpicRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(unknownId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that the entity returned by <see cref="FlowEpicRepository.GetByIdAsync"/> is not
    /// tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
    /// (read-only path must use <c>AsNoTracking</c>).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_FlowEpic_IsNotTracked()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Untracked Tenant", "Untracked Facility");
        var epic = FlowEpic.Create("Untracked Epic", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new FlowEpicRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(DbContext.ChangeTracker.Entries<FlowEpic>());
    }

    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.AddAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists the entity
    /// so that a subsequent <see cref="FlowEpicRepository.GetByIdAsync"/> returns it with the
    /// correct title and <see cref="FlowEpic.TargetFacilityId"/>.
    /// </summary>
    [Fact]
    public async Task AddAsync_FlowEpic_PersistsToDatabase()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Seed Tenant", "Seed Facility");
        var epic = FlowEpic.Create("New Epic", facilityId, tenantId);
        var repository = new FlowEpicRepository(DbContext);

        // Act
        await repository.AddAsync(epic, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(epic.Id, retrieved.Id);
        Assert.Equal("New Epic", retrieved.Title.Value);
        Assert.Equal(facilityId, retrieved.TargetFacilityId);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — title change
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists the title
    /// change so that a subsequent read reflects the new value.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_FlowEpic_PersistsTitleChange()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Rename Tenant", "Rename Facility");
        var epic = FlowEpic.Create("Original Title", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FlowEpicRepository(DbContext);

        // Act
        epic.UpdateTitle("Updated Title");
        await repository.UpdateAsync(epic, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title.Value);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — execution state transition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.UpdateAsync"/> persists the execution state
    /// change caused by <see cref="FlowEpic.StartExecution"/> so that a subsequent read reflects
    /// the <see cref="SpaceOS.Kernel.Domain.Enums.WorkflowPhase.Delivery"/> phase.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_FlowEpic_PersistsExecutionStateChange()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Execution Tenant", "Execution Facility");
        var epic = FlowEpic.Create("Execution Epic", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FlowEpicRepository(DbContext);

        // Act
        epic.StartExecution();
        await repository.UpdateAsync(epic, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SpaceOS.Kernel.Domain.Enums.WorkflowPhase.Delivery, result.Phase);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — B2BHandshake owned-entity round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="B2BHandshake"/> owned entity is correctly persisted after
    /// <see cref="FlowEpic.DelegateTo"/> and that a subsequent read hydrates both
    /// <see cref="B2BHandshake.GuestTenantId"/> and <see cref="B2BHandshake.DelegatedOn"/>.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_FlowEpic_PersistsB2BHandshake()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Delegation Tenant", "Delegation Facility");
        var epic = FlowEpic.Create("Delegated Epic", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var guestTenantId = TenantId.New();
        var repository = new FlowEpicRepository(DbContext);

        // Act
        epic.DelegateTo(guestTenantId);
        await repository.UpdateAsync(epic, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Handshake);
        Assert.Equal(guestTenantId, result.Handshake.GuestTenantId);
        Assert.True(result.Handshake.DelegatedOn > DateTimeOffset.MinValue);
    }

    /// <summary>
    /// Verifies that after persisting a delegation, <see cref="FlowEpicRepository.GetByIdAsync"/>
    /// returns an entity with a non-null <see cref="B2BHandshake"/> (owned-entity hydration guard).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_DelegatedFlowEpic_ReturnsNonNullHandshake()
    {
        // Arrange
        var (facilityId, tenantId) = await SeedFacilityChainAsync("Handshake Tenant", "Handshake Facility");
        var epic = FlowEpic.Create("Handshake Epic", facilityId, tenantId);
        DbContext.FlowEpics.Add(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var guestTenantId = TenantId.New();
        epic.DelegateTo(guestTenantId);
        DbContext.FlowEpics.Update(epic);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new FlowEpicRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(epic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Handshake);
    }

    // -------------------------------------------------------------------------
    // ListAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FlowEpicRepository.ListAsync"/> with
    /// <see cref="FlowEpicsByFacilityIdSpec"/> returns only the epics belonging to the specified
    /// facility and excludes those belonging to another facility.
    /// </summary>
    [Fact]
    public async Task ListAsync_FlowEpicsByFacilityIdSpec_ReturnsOnlyFacilityEpics()
    {
        // Arrange
        var tenant = Tenant.Create("List Tenant");
        DbContext.Tenants.Add(tenant);
        var facilityA = Facility.Create("Facility A", tenant.Id);
        var facilityB = Facility.Create("Facility B", tenant.Id);
        DbContext.Facilities.AddRange(facilityA, facilityB);

        DbContext.FlowEpics.AddRange(
            FlowEpic.Create("Epic A1", facilityA.Id, tenant.Id),
            FlowEpic.Create("Epic A2", facilityA.Id, tenant.Id),
            FlowEpic.Create("Epic B1", facilityB.Id, tenant.Id));

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FlowEpicRepository(DbContext);
        var spec = new FlowEpicsByFacilityIdSpec(facilityA.Id);

        // Act
        var results = await repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal(facilityA.Id, e.TargetFacilityId));
    }
}
