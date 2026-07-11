// SpaceOS.Kernel.Api.Tests/Endpoints/AuthEndpointTests.cs

using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using Xunit;


namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// API integration tests for <c>POST /api/auth/logout</c>.
/// Refresh token rotation and JWKS endpoints have been removed — Keycloak handles
/// token lifecycle and publishes its own JWKS via the authority discovery document.
/// </summary>
public sealed class AuthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // --- POST /api/auth/logout ---

    [Fact]
    public async Task PostLogout_NonExistentToken_Returns200()
    {
        // Arrange — idempotent: non-existent token should return 200 OK (BE-P15-11)
        var token = new string('b', 43);
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = token }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostLogout_MissingBody_Returns422()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostLogout_TooShortToken_Returns422()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = "short" }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

}
