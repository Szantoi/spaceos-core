// SpaceOS.Kernel.Api.Tests/Endpoints/AuditEventEndpointTests.cs

using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the GET /api/audit-events endpoint.</summary>
public sealed class AuditEventEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public AuditEventEndpointTests()
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
    public async Task GetAuditEvents_ReturnsPagedResults()
    {
        // Arrange
        var tenantId = ApiFactory.TestTenantId.Value;
        var url = $"/api/audit-events?tenantId={tenantId}&page=1&pageSize=20";

        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<AuditEventDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.NotNull(paged.Items);
    }

    [Fact]
    public async Task GetAuditEvents_RequiresAuthentication()
    {
        // Arrange — unauthenticated client
        using var unauthClient = _factory.CreateClient();
        var tenantId = ApiFactory.TestTenantId.Value;
        var url = $"/api/audit-events?tenantId={tenantId}&page=1&pageSize=20";

        // Act
        var response = await unauthClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditEvents_InvalidDateRange_Returns422()
    {
        // Arrange — from > to
        var tenantId = ApiFactory.TestTenantId.Value;
        var from = "2026-01-10T00:00:00Z";
        var to = "2026-01-01T00:00:00Z";
        var url = $"/api/audit-events?tenantId={tenantId}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&page=1&pageSize=20";

        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
