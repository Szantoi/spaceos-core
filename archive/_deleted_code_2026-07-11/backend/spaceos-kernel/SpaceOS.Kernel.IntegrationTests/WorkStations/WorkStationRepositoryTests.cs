// SpaceOS.Kernel.IntegrationTests/WorkStations/WorkStationRepositoryTests.cs
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.WorkStations;

/// <summary>
/// Integration tests that verify <see cref="WorkStationRepository"/> behaviour
/// against a real in-memory SQLite schema created by <see cref="RepositoryTestBase"/>.
/// </summary>
public sealed class WorkStationRepositoryTests : RepositoryTestBase
{
    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.GetByIdAsync"/> returns the correct
    /// entity when the requested identifier exists in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingWorkStation_ReturnsWorkStation()
    {
        // Arrange
        var tenant = Tenant.Create("Acme Corp");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Main Hall", tenant.Id);
        DbContext.Facilities.Add(facility);
        var workStation = WorkStation.Create("Desk Alpha", "Desk", facility.Id, tenant.Id);
        DbContext.WorkStations.Add(workStation);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new WorkStationRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(workStation.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workStation.Id, result.Id);
        Assert.Equal("Desk Alpha", result.Name.Value);
        Assert.Equal(facility.Id, result.FacilityId);
    }

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.GetByIdAsync"/> returns <see langword="null"/>
    /// when the requested identifier does not exist in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownWorkStationId_ReturnsNull()
    {
        // Arrange
        var unknownId = WorkStationId.New();
        var repository = new WorkStationRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(unknownId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that the entity returned by <see cref="WorkStationRepository.GetByIdAsync"/>
    /// is not tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
    /// (read-only path must use <c>AsNoTracking</c>).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WorkStation_IsNotTracked()
    {
        // Arrange
        var tenant = Tenant.Create("Untracked Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Untracked Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        var workStation = WorkStation.Create("Untracked Desk", "Desk", facility.Id, tenant.Id);
        DbContext.WorkStations.Add(workStation);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new WorkStationRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(workStation.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(DbContext.ChangeTracker.Entries<WorkStation>());
    }

    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.AddAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the entity and it can subsequently be retrieved by id.
    /// </summary>
    [Fact]
    public async Task AddAsync_WorkStation_PersistsToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Seed Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Seed Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var workStation = WorkStation.Create("New Desk", "Desk", facility.Id, tenant.Id);
        var repository = new WorkStationRepository(DbContext);

        // Act
        await repository.AddAsync(workStation, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(workStation.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(workStation.Id, retrieved.Id);
        Assert.Equal("New Desk", retrieved.Name.Value);
        Assert.Equal("Desk", retrieved.Type.Value);
        Assert.Equal(facility.Id, retrieved.FacilityId);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the name change so that a subsequent read reflects the new value.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WorkStation_PersistsNameChange()
    {
        // Arrange
        var tenant = Tenant.Create("Rename Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Rename Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        var workStation = WorkStation.Create("Original Desk", "Desk", facility.Id, tenant.Id);
        DbContext.WorkStations.Add(workStation);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new WorkStationRepository(DbContext);

        // Act
        workStation.UpdateName("Updated Desk");
        await repository.UpdateAsync(workStation, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(workStation.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Desk", result.Name.Value);
    }

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the status change so that a subsequent read reflects the new status value.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WorkStation_PersistsStatusChange()
    {
        // Arrange
        var tenant = Tenant.Create("Status Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Status Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        var workStation = WorkStation.Create("Status Desk", "Desk", facility.Id, tenant.Id);
        DbContext.WorkStations.Add(workStation);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new WorkStationRepository(DbContext);

        // Act
        workStation.ChangeStatus(WorkStationStatus.Occupied);
        await repository.UpdateAsync(workStation, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(workStation.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkStationStatus.Occupied, result.Status);
    }

    // -------------------------------------------------------------------------
    // ListAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="WorkStationRepository.ListAsync"/> with
    /// <see cref="WorkStationsByFacilityIdSpec"/> returns only the workstations belonging
    /// to the specified facility and not those belonging to another facility.
    /// </summary>
    [Fact]
    public async Task ListAsync_WorkStationsByFacilityIdSpec_ReturnsOnlyFacilityWorkStations()
    {
        // Arrange
        var tenant = Tenant.Create("List Tenant");
        DbContext.Tenants.Add(tenant);

        var facilityA = Facility.Create("Facility A", tenant.Id);
        var facilityB = Facility.Create("Facility B", tenant.Id);
        DbContext.Facilities.AddRange(facilityA, facilityB);

        DbContext.WorkStations.AddRange(
            WorkStation.Create("Desk A1", "Desk", facilityA.Id, tenant.Id),
            WorkStation.Create("Desk A2", "Desk", facilityA.Id, tenant.Id),
            WorkStation.Create("Desk B1", "Desk", facilityB.Id, tenant.Id));

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new WorkStationRepository(DbContext);
        var spec = new WorkStationsByFacilityIdSpec(facilityA.Id);

        // Act
        var results = await repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, ws => Assert.Equal(facilityA.Id, ws.FacilityId));
    }
}
