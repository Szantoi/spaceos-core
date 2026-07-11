// SpaceOS.Kernel.IntegrationTests/Facilities/FacilityRepositoryTests.cs
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Facilities;

/// <summary>
/// Integration tests that verify <see cref="FacilityRepository"/> behaviour
/// against a real in-memory SQLite schema created by <see cref="RepositoryTestBase"/>.
/// </summary>
public sealed class FacilityRepositoryTests : RepositoryTestBase
{
    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.GetByIdAsync"/> returns the correct
    /// entity when the requested identifier exists in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingFacility_ReturnsFacility()
    {
        // Arrange
        var tenant = Tenant.Create("Acme Corp");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Main Hall", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilityRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(facility.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(facility.Id, result.Id);
        Assert.Equal("Main Hall", result.Name.Value);
        Assert.Equal(tenant.Id, result.TenantId);
    }

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.GetByIdAsync"/> returns <see langword="null"/>
    /// when the requested identifier does not exist in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownFacilityId_ReturnsNull()
    {
        // Arrange
        var unknownId = FacilityId.New();
        var repository = new FacilityRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(unknownId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that the entity returned by <see cref="FacilityRepository.GetByIdAsync"/>
    /// is not tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
    /// (read-only path must use <c>AsNoTracking</c>).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Facility_IsNotTracked()
    {
        // Arrange
        var tenant = Tenant.Create("Untracked Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Untracked Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new FacilityRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(facility.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(DbContext.ChangeTracker.Entries<Facility>());
    }

    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.AddAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the entity and it can subsequently be retrieved by id.
    /// </summary>
    [Fact]
    public async Task AddAsync_Facility_PersistsToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Seed Tenant");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var facility = Facility.Create("New Facility", tenant.Id);
        var repository = new FacilityRepository(DbContext);

        // Act
        await repository.AddAsync(facility, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(facility.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(facility.Id, retrieved.Id);
        Assert.Equal("New Facility", retrieved.Name.Value);
        Assert.Equal(tenant.Id, retrieved.TenantId);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the name change so that a subsequent read reflects the new value.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Facility_PersistsNameChange()
    {
        // Arrange
        var tenant = Tenant.Create("Rename Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Original Name", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilityRepository(DbContext);

        // Act
        facility.Rename("Updated Name");
        await repository.UpdateAsync(facility, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(facility.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name.Value);
    }

    // -------------------------------------------------------------------------
    // ListAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.ListAsync"/> with
    /// <see cref="FacilitiesByTenantIdSpec"/> returns only the facilities belonging
    /// to the specified tenant and not those belonging to another tenant.
    /// </summary>
    [Fact]
    public async Task ListAsync_FacilitiesByTenantIdSpec_ReturnsOnlyTenantFacilities()
    {
        // Arrange
        var tenantA = Tenant.Create("Tenant A");
        var tenantB = Tenant.Create("Tenant B");
        DbContext.Tenants.AddRange(tenantA, tenantB);

        DbContext.Facilities.AddRange(
            Facility.Create("Facility A1", tenantA.Id),
            Facility.Create("Facility A2", tenantA.Id),
            Facility.Create("Facility B1", tenantB.Id));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilityRepository(DbContext);
        var spec = new FacilitiesByTenantIdSpec(tenantA.Id);

        // Act
        var results = await repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, f => Assert.Equal(tenantA.Id, f.TenantId));
    }

    // -------------------------------------------------------------------------
    // ExistsByNameAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.ExistsByNameAsync"/> returns
    /// <see langword="true"/> when a facility with the given name already exists
    /// under the specified tenant.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_ExistingFacilityName_ReturnsTrue()
    {
        // Arrange
        var tenant = Tenant.Create("Exists Tenant");
        DbContext.Tenants.Add(tenant);
        DbContext.Facilities.Add(Facility.Create("Test Facility", tenant.Id));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilityRepository(DbContext);

        // Act
        var result = await repository.ExistsByNameAsync(tenant.Id, "Test Facility", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that <see cref="FacilityRepository.ExistsByNameAsync"/> returns
    /// <see langword="false"/> when no facility with the given name exists
    /// under the specified tenant.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_UnknownFacilityName_ReturnsFalse()
    {
        // Arrange
        var tenant = Tenant.Create("Empty Tenant");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilityRepository(DbContext);

        // Act
        var result = await repository.ExistsByNameAsync(tenant.Id, "NonExistent Facility", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }
}
