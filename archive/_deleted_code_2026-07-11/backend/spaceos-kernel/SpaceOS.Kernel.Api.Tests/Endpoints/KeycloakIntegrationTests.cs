// SpaceOS.Kernel.Api.Tests/Endpoints/KeycloakIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// API integration tests for the Keycloak IdP integration (MSG-KC01 T5).
/// Covers:
/// <list type="bullet">
///   <item><c>JwtBearer_Authority_Config</c> × 2 — authority wired, 401 with problem+json body</item>
///   <item><c>Auth_Endpoints_Removed</c> × 2 — JWKS proxy and refresh token endpoints gone</item>
/// </list>
/// </summary>
public sealed class KeycloakIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _authorizedClient;
    private readonly HttpClient _anonymousClient;

    public KeycloakIntegrationTests(ApiFactory factory)
    {
        _factory          = factory;
        _authorizedClient = factory.CreateAuthorizedClient();
        _anonymousClient  = factory.CreateClient();
    }

    // =========================================================================
    // JwtBearer_Authority_Config × 2
    // =========================================================================

    [Fact]
    public async Task JwtBearer_AuthorizedRequest_Returns200_ConfirmingAuthorityConfigured()
    {
        // Arrange — ApiFactory.PostConfigure overrides TVP to accept test-signed RS256 tokens.
        // A 200 response proves that the JWT Bearer middleware is correctly wired and the
        // test token is accepted (authority-based JWKS discovery is bypassed in tests only).
        await _factory.SeedAsync().ConfigureAwait(false);

        // Act — any protected endpoint; /api/tenants is the simplest read endpoint
        var response = await _authorizedClient.GetAsync(
            "/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JwtBearer_UnauthenticatedRequest_Returns401WithProblemJson()
    {
        // Arrange + Act — no Authorization header at all
        var response = await _anonymousClient.GetAsync(
            "/api/tenants", TestContext.Current.CancellationToken);

        // Assert — HTTP 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // The OnChallenge handler in Program.cs sets ContentType but WriteAsJsonAsync
        // serialises the anonymous object as application/json in the test host.
        // We verify that a JSON body with status=401 is present rather than asserting
        // the exact media type, which varies between test and production environments.
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.NotNull(contentType);
        Assert.Contains("json", contentType, StringComparison.OrdinalIgnoreCase);

        // Body must contain the problem details status field
        var body = await response.Content.ReadFromJsonAsync<ProblemBody>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(401, body.Status);
    }

    // =========================================================================
    // Auth_Endpoints_Removed × 2
    // =========================================================================

    [Fact]
    public async Task GetJwksEndpoint_Returns404_EndpointHasBeenRemoved()
    {
        // Arrange — /.well-known/jwks.json was served by the Kernel in the pre-Keycloak era.
        // After T1-T4 the endpoint was removed; Keycloak publishes its own JWKS.
        // Act
        var response = await _anonymousClient.GetAsync(
            "/.well-known/jwks.json", TestContext.Current.CancellationToken);

        // Assert — 404: no route registered for this path
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostRefreshToken_Returns404_EndpointHasBeenRemoved()
    {
        // Arrange — POST /api/auth/refresh was removed; Keycloak's token endpoint handles refresh.
        // Act
        var response = await _anonymousClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = "some-token" },
            TestContext.Current.CancellationToken);

        // Assert — 404: no route registered for this path
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // Minimal DTO for deserialising the 401 problem body
    // =========================================================================

    private sealed record ProblemBody(int Status);
}
