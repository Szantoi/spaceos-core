// SpaceOS.Kernel.Api.Tests/Middleware/SipVersionMiddlewareTests.cs
using System.Net;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Modules.Abstractions.Sync;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Middleware;

/// <summary>
/// Integration tests for <see cref="SpaceOS.Kernel.Api.Middleware.SipVersionMiddleware"/>.
/// Verifies that <c>/api/sync/*</c> and <c>/api/nodes/*</c> paths require the
/// <c>SpaceOS-SIP-Version</c> header with a supported value.
/// </summary>
public sealed class SipVersionMiddlewareTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync() =>
        await _factory.SeedAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() =>
        await _factory.DisposeAsync();

    // --- /api/sync/* path ---

    [Fact]
    public async Task SyncPath_MissingSipVersionHeader_Returns400()
    {
        var client = _factory.CreateAuthorizedClient();

        var response = await client.GetAsync("/api/sync/signals?tenantId=00000000-0000-0000-0000-000000000001&epicId=00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("SIP", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncPath_UnsupportedSipVersion_Returns400()
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add("SpaceOS-SIP-Version", "99.0");

        var response = await client.GetAsync("/api/sync/signals?tenantId=00000000-0000-0000-0000-000000000001&epicId=00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SyncPath_ValidSipVersion_PassesThrough()
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add("SpaceOS-SIP-Version", SyncConstants.SipVersion);

        var response = await client.GetAsync("/api/sync/signals?tenantId=00000000-0000-0000-0000-000000000001&epicId=00000000-0000-0000-0000-000000000001");

        // Should pass middleware (may return 200/404 depending on data, but NOT 400)
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- /api/nodes/* path ---

    [Fact]
    public async Task NodesPath_MissingSipVersionHeader_Returns400()
    {
        var client = _factory.CreateAuthorizedClient();

        var response = await client.GetAsync("/api/nodes/manifest?tenantId=00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("SIP", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NodesPath_ValidSipVersion_PassesThrough()
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add("SpaceOS-SIP-Version", SyncConstants.SipVersion);

        var response = await client.GetAsync("/api/nodes/manifest?tenantId=00000000-0000-0000-0000-000000000001");

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Non-SIP paths unaffected ---

    [Fact]
    public async Task NonSipPath_NoHeader_PassesThrough()
    {
        var client = _factory.CreateAuthorizedClient();

        var response = await client.GetAsync("/api/tenants");

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_NoHeader_PassesThrough()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
