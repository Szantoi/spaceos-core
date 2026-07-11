// SpaceOS.Kernel.Api.Tests/Endpoints/FacilityEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the Facility GET endpoints.</summary>
public sealed class FacilityEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public FacilityEndpointTests()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateAuthorizedClient();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _factory.SeedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task GetFacilityById_Returns200()
    {
        // Arrange — facility TenantId must match TestTenantId so the EF Core query filter passes.
        var tenant = Tenant.Create("Owner Tenant");
        var facility = Facility.Create("Building A", ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync($"/api/facilities/{facility.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<FacilityDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(facility.Id.Value, dto.Id);
        Assert.Equal("Building A", dto.Name);
    }

    [Fact]
    public async Task GetFacilityById_UnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/facilities/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}