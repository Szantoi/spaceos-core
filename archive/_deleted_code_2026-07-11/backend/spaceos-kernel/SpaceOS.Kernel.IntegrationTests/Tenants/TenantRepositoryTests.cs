// SpaceOS.Kernel.IntegrationTests/Tenants/TenantRepositoryTests.cs
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Tenants;

/// <summary>
/// Integration tests that verify <see cref="TenantRepository"/> behaviour
/// against a real in-memory SQLite schema created by <see cref="RepositoryTestBase"/>.
/// </summary>
public sealed class TenantRepositoryTests : RepositoryTestBase
{
    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="TenantRepository.GetByIdAsync"/> returns the correct
    /// entity when the requested identifier exists in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingTenant_ReturnsTenant()
    {
        // Arrange
        var tenant = Tenant.Create("Acme Corp");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new TenantRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Acme Corp", result.Name.Value);
    }

    /// <summary>
    /// Verifies that <see cref="TenantRepository.GetByIdAsync"/> returns <see langword="null"/>
    /// when the requested identifier does not exist in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownTenantId_ReturnsNull()
    {
        // Arrange
        var unknownId = TenantId.New();
        var repository = new TenantRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(unknownId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that the entity returned by <see cref="TenantRepository.GetByIdAsync"/>
    /// is not tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
    /// (read-only path must use <c>AsNoTracking</c>).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Tenant_IsNotTracked()
    {
        // Arrange
        var tenant = Tenant.Create("Untracked Tenant");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new TenantRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(DbContext.ChangeTracker.Entries<Tenant>());
    }

    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="TenantRepository.AddAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the entity and it can subsequently be retrieved by id.
    /// </summary>
    [Fact]
    public async Task AddAsync_Tenant_PersistsToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("New Tenant");
        var repository = new TenantRepository(DbContext);

        // Act
        await repository.AddAsync(tenant, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(tenant.Id, retrieved.Id);
        Assert.Equal("New Tenant", retrieved.Name.Value);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="TenantRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the name change so that a subsequent read reflects the new value.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Tenant_PersistsNameChange()
    {
        // Arrange
        var tenant = Tenant.Create("Original Name");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new TenantRepository(DbContext);

        // Act
        tenant.UpdateName("Updated Name");
        await repository.UpdateAsync(tenant, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name.Value);
    }

    // -------------------------------------------------------------------------
    // ListAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="TenantRepository.ListAsync"/> with <see cref="AllTenantsSpec"/>
    /// returns all seeded tenants.
    /// </summary>
    [Fact]
    public async Task ListAsync_AllTenantsSpec_ReturnsAllSeededTenants()
    {
        // Arrange
        DbContext.Tenants.AddRange(
            Tenant.Create("Tenant Alpha"),
            Tenant.Create("Tenant Beta"),
            Tenant.Create("Tenant Gamma"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new TenantRepository(DbContext);
        var spec = new AllTenantsSpec();

        // Act
        var results = await repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(results.Count >= 3, $"Expected at least 3 tenants but got {results.Count}.");
    }
}
