// SpaceOS.Kernel.IntegrationTests/SpaceLayers/SpaceLayerRepositoryTests.cs
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.SpaceLayers;

/// <summary>
/// Integration tests that verify <see cref="SpaceLayerRepository"/> behaviour
/// against a real in-memory SQLite schema created by <see cref="RepositoryTestBase"/>.
/// </summary>
public sealed class SpaceLayerRepositoryTests : RepositoryTestBase
{
    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="SpaceLayerRepository.GetByIdAsync"/> returns the correct
    /// entity when the requested identifier exists in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingSpaceLayer_ReturnsSpaceLayer()
    {
        // Arrange
        var tenant = Tenant.Create("Acme Corp");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Main Hall", tenant.Id);
        DbContext.Facilities.Add(facility);
        var layer = SpaceLayer.CreateLocalLayer("{\"version\":1}", facility.Id, TradeType.Architecture, tenant.Id);
        DbContext.SpaceLayers.Add(layer);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SpaceLayerRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(layer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(layer.Id, result.Id);
        Assert.Equal(facility.Id, result.FacilityId);
        Assert.Equal(TradeType.Architecture, result.TradeType);
    }

    /// <summary>
    /// Verifies that <see cref="SpaceLayerRepository.GetByIdAsync"/> returns <see langword="null"/>
    /// when the requested identifier does not exist in the database.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownSpaceLayerId_ReturnsNull()
    {
        // Arrange
        var unknownId = SpaceLayerId.New();
        var repository = new SpaceLayerRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(unknownId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that the entity returned by <see cref="SpaceLayerRepository.GetByIdAsync"/>
    /// is not tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
    /// (read-only path must use <c>AsNoTracking</c>).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_SpaceLayer_IsNotTracked()
    {
        // Arrange
        var tenant = Tenant.Create("Untracked Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Untracked Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        var layer = SpaceLayer.CreateLocalLayer("{\"v\":1}", facility.Id, TradeType.Electrical, tenant.Id);
        DbContext.SpaceLayers.Add(layer);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var repository = new SpaceLayerRepository(DbContext);

        // Act
        var result = await repository.GetByIdAsync(layer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(DbContext.ChangeTracker.Entries<SpaceLayer>());
    }

    // -------------------------------------------------------------------------
    // AddAsync — local layer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a local <see cref="SpaceLayer"/> created via
    /// <see cref="SpaceLayer.CreateLocalLayer"/> is persisted and round-trips correctly,
    /// retaining its intent JSON and trade type.
    /// </summary>
    [Fact]
    public async Task AddAsync_LocalSpaceLayer_PersistsToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Local Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Local Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        const string intentJson = "{\"floors\":3,\"area\":1200}";
        var layer = SpaceLayer.CreateLocalLayer(intentJson, facility.Id, TradeType.Joinery, tenant.Id);
        var repository = new SpaceLayerRepository(DbContext);

        // Act
        await repository.AddAsync(layer, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(layer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(layer.Id, retrieved.Id);
        Assert.Equal(intentJson, retrieved.IntentDataJson);
        Assert.Equal(TradeType.Joinery, retrieved.TradeType);
        Assert.False(retrieved.IsExternalNode);
        Assert.Null(retrieved.ExternalSourceUrl);
    }

    // -------------------------------------------------------------------------
    // AddAsync — federated (external) layer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an external (federated) <see cref="SpaceLayer"/> created via
    /// <see cref="SpaceLayer.CreateExternalLayer"/> is persisted and round-trips correctly,
    /// retaining its external URL and trade type.
    /// </summary>
    [Fact]
    public async Task AddAsync_FederatedSpaceLayer_PersistsToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Federated Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Federated Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        const string externalUrl = "https://node.spaceos.io/layers/42";
        var layer = SpaceLayer.CreateExternalLayer(externalUrl, facility.Id, TradeType.Plumbing, tenant.Id);
        var repository = new SpaceLayerRepository(DbContext);

        // Act
        await repository.AddAsync(layer, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(layer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(layer.Id, retrieved.Id);
        Assert.True(retrieved.IsExternalNode);
        Assert.Equal(externalUrl, retrieved.ExternalSourceUrl);
        Assert.Null(retrieved.IntentDataJson);
        Assert.Equal(TradeType.Plumbing, retrieved.TradeType);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="SpaceLayerRepository.UpdateAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the intent data change so that a subsequent read reflects the updated JSON.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_SpaceLayer_PersistsIntentDataChange()
    {
        // Arrange
        var tenant = Tenant.Create("Update Tenant");
        DbContext.Tenants.Add(tenant);
        var facility = Facility.Create("Update Facility", tenant.Id);
        DbContext.Facilities.Add(facility);
        var layer = SpaceLayer.CreateLocalLayer("{\"version\":1}", facility.Id, TradeType.Mep, tenant.Id);
        DbContext.SpaceLayers.Add(layer);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SpaceLayerRepository(DbContext);

        // Act
        layer.UpdateIntentData("{\"version\":2,\"updated\":true}");
        await repository.UpdateAsync(layer, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(layer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("{\"version\":2,\"updated\":true}", result.IntentDataJson);
    }

    // -------------------------------------------------------------------------
    // ListAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="SpaceLayerRepository.ListAsync"/> with
    /// <see cref="SpaceLayersByFacilityIdSpec"/> returns only the layers belonging
    /// to the specified facility and not those belonging to another facility.
    /// </summary>
    [Fact]
    public async Task ListAsync_SpaceLayersByFacilityIdSpec_ReturnsOnlyFacilityLayers()
    {
        // Arrange
        var tenant = Tenant.Create("List Tenant");
        DbContext.Tenants.Add(tenant);

        var facilityA = Facility.Create("Facility A", tenant.Id);
        var facilityB = Facility.Create("Facility B", tenant.Id);
        DbContext.Facilities.AddRange(facilityA, facilityB);

        DbContext.SpaceLayers.AddRange(
            SpaceLayer.CreateLocalLayer("{\"a\":1}", facilityA.Id, TradeType.Architecture, tenant.Id),
            SpaceLayer.CreateLocalLayer("{\"a\":2}", facilityA.Id, TradeType.Electrical, tenant.Id),
            SpaceLayer.CreateLocalLayer("{\"b\":1}", facilityB.Id, TradeType.Joinery, tenant.Id));

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SpaceLayerRepository(DbContext);
        var spec = new SpaceLayersByFacilityIdSpec(facilityA.Id);

        // Act
        var results = await repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, sl => Assert.Equal(facilityA.Id, sl.FacilityId));
    }
}
