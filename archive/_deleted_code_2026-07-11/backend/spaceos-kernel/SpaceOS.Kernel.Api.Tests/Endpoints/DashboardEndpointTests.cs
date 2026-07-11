// SpaceOS.Kernel.Api.Tests/Endpoints/DashboardEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Dashboard;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the GET /api/dashboard/stats endpoint.</summary>
public sealed class DashboardEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public DashboardEndpointTests()
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
    public async Task GetDashboardStats_AuthorizedRequest_Returns200WithDashboardStatsDto()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/dashboard/stats", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content
            .ReadFromJsonAsync<DashboardStatsDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.True(dto.TenantCount >= 0);
        Assert.True(dto.FacilityCount >= 0);
        Assert.True(dto.WorkStationCount >= 0);
        Assert.True(dto.ActiveWorkStationCount >= 0);
        Assert.True(dto.FlowEpicCount >= 0);
        Assert.True(dto.AuditEventCount >= 0);
    }

    [Fact]
    public async Task GetDashboardStats_UnauthenticatedRequest_Returns401()
    {
        // Arrange — use an unauthenticated client (no Bearer token)
        using var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync(
            "/api/dashboard/stats", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
