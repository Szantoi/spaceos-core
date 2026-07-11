// SpaceOS.Kernel.IntegrationTests/Tenants/TenantSmokeTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tenants;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Tenants;

/// <summary>
/// Smoke tests validating the shared <see cref="SpaceOsApiFactory"/> infrastructure
/// and basic Tenant endpoint behaviour (T5 — Integration Test Scaffold).
/// </summary>
public sealed class TenantSmokeTests : ApiTestBase
{
    /// <summary>
    /// Verifies that the <see cref="SpaceOsApiFactory"/> starts successfully
    /// and the health endpoint returns 200.
    /// </summary>
    [Fact]
    public async Task SpaceOsApiFactory_Boots_WithoutException()
    {
        // Act — factory is created in ApiTestBase constructor;
        // a successful HTTP response proves the host started
        var response = await Client.GetAsync("/healthz", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that <see cref="DatabaseSeedHelper.SeedTenantAsync"/> persists a Tenant
    /// that can subsequently be retrieved via the GET endpoint.
    /// </summary>
    [Fact]
    public async Task SeedTenantAsync_Creates_PersistentRecord()
    {
        // Arrange
        var tenant = await DatabaseSeedHelper.SeedTenantAsync(Services, "Persistent Tenant")
            ;

        // Act
        var response = await Client.GetAsync($"/api/tenants/{tenant.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TenantDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(tenant.Id.Value, dto.Id);
        Assert.Equal("Persistent Tenant", dto.Name);
    }

    /// <summary>
    /// Verifies that the full GET /api/tenants endpoint returns all seeded tenants.
    /// </summary>
    [Fact]
    public async Task GetAllTenants_AfterSeed_ReturnsSeededTenants()
    {
        // Arrange
        await DatabaseSeedHelper.SeedTenantAsync(Services, "Smoke Tenant A");
        await DatabaseSeedHelper.SeedTenantAsync(Services, "Smoke Tenant B");

        // Act
        var response = await Client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedList<TenantDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(paged);
        Assert.True(paged.Items.Count >= 2, "Expected at least 2 seeded tenants in the response");
    }
}
