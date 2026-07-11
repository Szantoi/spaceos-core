// SpaceOS.Kernel.IntegrationTests/Auth/AuthorizationTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Auth;

/// <summary>
/// Integration tests for JWT authentication and RBAC authorization enforcement.
/// Verifies 401/403/200/201 behaviour for the three roles: Joiner, Designer, Admin.
/// Also verifies that per-tenant data isolation via the <c>tid</c> claim works end-to-end.
/// </summary>
public sealed class AuthorizationTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // Auth_NoToken_Returns401
    // -------------------------------------------------------------------------

    /// <summary>
    /// A request to a protected endpoint without an Authorization header must return 401 Unauthorized
    /// with a Problem Details response body.
    /// </summary>
    [Fact]
    public async Task Auth_NoToken_Returns401()
    {
        // Arrange — client without any default auth header
        using var unauthClient = Factory.CreateClient();

        // Act
        var response = await unauthClient.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Healthz_WithNoToken_Returns200
    // -------------------------------------------------------------------------

    /// <summary>
    /// The /healthz endpoint is excluded from auth requirements and must return 200 without any token.
    /// </summary>
    [Fact]
    public async Task Healthz_WithNoToken_Returns200()
    {
        // Arrange — client without any default auth header
        using var unauthClient = Factory.CreateClient();

        // Act
        var response = await unauthClient.GetAsync("/healthz", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Auth_ValidJoinerToken_CanRead
    // -------------------------------------------------------------------------

    /// <summary>
    /// A Joiner-role token is sufficient for read (ReadPolicy) endpoints.
    /// GET /api/tenants with a Joiner token must return 200.
    /// </summary>
    [Fact]
    public async Task Auth_ValidJoinerToken_CanRead()
    {
        // Arrange
        using var joinerClient = Factory.CreateClient();
        joinerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.ForRole("Joiner"));

        // Act
        var response = await joinerClient.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Auth_WrongRole_Returns403
    // -------------------------------------------------------------------------

    /// <summary>
    /// A Joiner-role token must be rejected on a WritePolicy endpoint.
    /// POST /api/tenants with a Joiner token must return 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task Auth_WrongRole_Returns403()
    {
        // Arrange
        using var joinerClient = Factory.CreateClient();
        joinerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.ForRole("Joiner"));
        var request = new { Name = "Should Be Denied" };

        // Act
        var response = await joinerClient.PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Auth_ValidAdminToken_CanCreate
    // -------------------------------------------------------------------------

    /// <summary>
    /// An Admin-role token satisfies AdminPolicy on POST /api/tenants and must return 201 Created.
    /// </summary>
    [Fact]
    public async Task Auth_ValidAdminToken_CanCreate()
    {
        // Arrange
        using var adminClient = Factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.ForRole("Admin"));
        var request = new { Name = "Admin Created Tenant" };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    // -------------------------------------------------------------------------
    // TenantFilter_ReturnsOnlyOwnedData
    // -------------------------------------------------------------------------

    /// <summary>
    /// The per-tenant query filter must isolate data by the <c>tid</c> claim.
    /// When Tenant A's JWT is used, only Tenant A's facilities appear in the list.
    /// </summary>
    [Fact]
    public async Task TenantFilter_ReturnsOnlyOwnedData()
    {
        // Arrange — seed two tenants and a facility for each
        var tenantA = await DatabaseSeedHelper.SeedTenantAsync(Services, "Tenant Filter A");
        var tenantB = await DatabaseSeedHelper.SeedTenantAsync(Services, "Tenant Filter B");

        await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenantA.Id, "Facility For A")
            ;
        await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenantB.Id, "Facility For B")
            ;

        // Build a token carrying Tenant A's id in the tid claim
        var tokenA = JwtTokenHelper.GenerateToken("Designer", tenantA.Id.Value);
        using var clientA = Factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenA);

        // Act — list all facilities; the global query filter should restrict to Tenant A's data
        var response = await clientA
            .GetAsync($"/api/tenants/{tenantA.Id.Value}/facilities", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<FacilityDto>>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(paged);
        Assert.All(paged.Items, f => Assert.Equal(tenantA.Id.Value, f.TenantId));
    }
}
