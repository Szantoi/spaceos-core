// SpaceOS.Kernel.Api.Tests/Endpoints/ToolEndpointTests.cs
using System.Net;
using System.Net.Http.Headers;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /api/tools/summary</c> and related Tool Registry endpoints,
/// focusing on the <c>GetTenantId()</c> claim resolution logic (KC-T2 fix, MSG-KERNEL-059).
/// </summary>
/// <remarks>
/// Verifies that the <c>spaceos_tenants</c> claim (Keycloak KC-T2 format) is resolved
/// correctly. ASP.NET <c>JsonWebTokenHandler</c> splits JSON arrays into individual
/// <see cref="System.Security.Claims.Claim"/> objects with the same name; each value is
/// a JSON object string like <c>{"tenant_id":"..."}</c> — NOT the original array.
/// </remarks>
public sealed class ToolEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;

    /// <summary>Initialises the factory for this test class.</summary>
    public ToolEndpointTests()
    {
        _factory = new ApiFactory();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _factory.SeedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // spaceos_tenants claim (KC-T2 format) — MSG-KERNEL-059 fix
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTenantSummary_SpaceosTenantsClaimOnly_ReturnsOk()
    {
        // Arrange — JWT carries spaceos_tenants claim (no flat tid).
        // ASP.NET splits the original JSON array into individual Claim objects,
        // each with value {"tenant_id":"<GUID>"} — this is what the fixed
        // FindAll("spaceos_tenants") loop must handle.
        var token  = JwtTestHelper.ForRoleWithSpaceosTenantsOnly("Admin");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync(
            "/api/tools/summary", TestContext.Current.CancellationToken);

        // Assert — 200: GetTenantId() resolved the GUID via spaceos_tenants claim
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTenantSummary_MultipleSpaceosTenantsClaimsReturnFirst_ReturnsOk()
    {
        // Arrange — JWT carries two spaceos_tenants claims (two tenants).
        // The first valid tenant_id must be returned.
        var token = JwtTestHelper.ForRoleWithSpaceosTenantsOnly(
            "Admin",
            tenantIds: [ApiFactory.TestTenantId.Value, Guid.NewGuid()]);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync(
            "/api/tools/summary", TestContext.Current.CancellationToken);

        // Assert — 200: first spaceos_tenants entry resolved successfully
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Legacy tid claim still works (backward compat)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTenantSummary_LegacyTidClaim_ReturnsOk()
    {
        // Arrange — JWT carries only the flat "tid" claim (legacy format before KC-T2).
        // IMPORTANT: JwtSecurityTokenHandler.DefaultInboundClaimTypeMap maps the JWT "tid"
        // claim → "http://schemas.microsoft.com/identity/claims/tenantid" in the
        // ClaimsPrincipal.  GetTenantId() must check the mapped URI, not the raw "tid" key.
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(
            "/api/tools/summary", TestContext.Current.CancellationToken);

        // Assert — 200: GetTenantId() resolves via MicrosoftTenantIdClaimType fallback
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // No tenant claim → 401
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTenantSummary_NoTenantClaim_Returns401()
    {
        // Arrange — unauthenticated request (no Bearer token at all)
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/tools/summary", TestContext.Current.CancellationToken);

        // Assert — 401: no authentication
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Other tool endpoints — smoke tests with KC-T2 token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ListFlowEpics_SpaceosTenantsClaimOnly_ReturnsOk()
    {
        // Arrange
        var token  = JwtTestHelper.ForRoleWithSpaceosTenantsOnly("Admin");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync(
            "/api/tools/flow-epics?page=1&pageSize=20",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListWorkStations_SpaceosTenantsClaimOnly_ReturnsOk()
    {
        // Arrange
        var token  = JwtTestHelper.ForRoleWithSpaceosTenantsOnly("Admin");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync(
            "/api/tools/workstations?page=1&pageSize=20",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListFacilities_SpaceosTenantsClaimOnly_ReturnsOk()
    {
        // Arrange
        var token  = JwtTestHelper.ForRoleWithSpaceosTenantsOnly("Admin");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync(
            "/api/tools/facilities?page=1&pageSize=20",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
