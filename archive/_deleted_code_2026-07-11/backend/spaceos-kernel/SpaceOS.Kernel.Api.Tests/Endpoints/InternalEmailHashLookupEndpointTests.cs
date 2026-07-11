// SpaceOS.Kernel.Api.Tests/Endpoints/InternalEmailHashLookupEndpointTests.cs

using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /internal/tenants/by-email-hash</c> (KERNEL-104).
/// Validates the X-SpaceOS-Internal gate, the found-200, and the not-found-404 paths.
/// </summary>
public sealed class InternalEmailHashLookupEndpointTests : IAsyncLifetime
{
    // A valid SHA-256 hex string (64 lowercase hex chars)
    private const string KnownHash   = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
    private const string UnknownHash = "b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05a15c";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public InternalEmailHashLookupEndpointTests()
    {
        _factory = new ApiFactory();
        _client  = _factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        // Seed one tenant whose EmailHash is KnownHash.
        await _factory.SeedAsync(async db =>
        {
            var tenant = Tenant.Create("Hash Test Tenant");
            tenant.SetEmailHash(KnownHash);
            await db.Tenants.AddAsync(tenant).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    // ── 1. Valid hash → 200 + tenantId ───────────────────────────────────────

    [Fact]
    public async Task GetByEmailHash_ValidHash_Returns200WithTenantId()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/internal/tenants/by-email-hash?hash={KnownHash}");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<TenantLookupResponse>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.TenantId);
    }

    // ── 2. Unknown hash → 404 ────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailHash_UnknownHash_Returns404()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/internal/tenants/by-email-hash?hash={UnknownHash}");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── 3. Missing X-SpaceOS-Internal → 403 ──────────────────────────────────

    [Fact]
    public async Task GetByEmailHash_MissingInternalHeader_Returns403()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/internal/tenants/by-email-hash?hash={KnownHash}");
        // No X-SpaceOS-Internal header

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Response DTO ──────────────────────────────────────────────────────────

    private sealed record TenantLookupResponse(Guid TenantId);
}
