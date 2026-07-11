// SpaceOS.Kernel.Tests/Infrastructure/JwksHealthCheckTests.cs
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SpaceOS.Infrastructure.Health;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="JwksHealthCheck"/>.
/// The HTTP client factory is mocked so no real network calls are made.
/// </summary>
public sealed class JwksHealthCheckTests
{
    // =========================================================================
    // JwksHealthCheck × 4
    // =========================================================================

    [Fact]
    public async Task CheckHealthAsync_JwksEndpointReturns200_ReturnsHealthy()
    {
        // Arrange
        var (sut, _) = BuildSut(
            authority: "https://keycloak.example.com/realms/spaceos",
            responseStatusCode: HttpStatusCode.OK);

        var ctx = BuildContext();

        // Act
        var result = await sut.CheckHealthAsync(ctx, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("reachable", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_JwksEndpointReturns503_ReturnsDegraded()
    {
        // Arrange — JWKS endpoint responds but with a server error status
        var (sut, _) = BuildSut(
            authority: "https://keycloak.example.com/realms/spaceos",
            responseStatusCode: HttpStatusCode.ServiceUnavailable);

        var ctx = BuildContext();

        // Act
        var result = await sut.CheckHealthAsync(ctx, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("503", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_HttpClientThrowsException_ReturnsDegraded()
    {
        // Arrange — simulate network unreachability (HttpRequestException)
        var (sut, _) = BuildSutWithException(
            authority: "https://keycloak.example.com/realms/spaceos",
            exception: new HttpRequestException("Connection refused"));

        var ctx = BuildContext();

        // Act
        var result = await sut.CheckHealthAsync(ctx, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.NotNull(result.Exception);
        Assert.Contains("unreachable", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_JwtAuthorityNotConfigured_ReturnsDegraded()
    {
        // Arrange — Jwt:Authority key is missing from configuration
        var config = new ConfigurationBuilder().Build(); // no keys at all

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(nameof(JwksHealthCheck)))
            .Returns(httpClient);

        var sut = new JwksHealthCheck(factoryMock.Object, config);
        var ctx = BuildContext();

        // Act
        var result = await sut.CheckHealthAsync(ctx, CancellationToken.None).ConfigureAwait(false);

        // Assert — must report degraded before ever making an HTTP call
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Jwt:Authority", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static (JwksHealthCheck sut, Mock<IHttpClientFactory> factoryMock) BuildSut(
        string authority,
        HttpStatusCode responseStatusCode)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = authority
            })
            .Build();

        var response = new HttpResponseMessage(responseStatusCode);

        var handlerMock = new Mock<DelegatingTestHandler> { CallBase = false };
        var httpClient = new HttpClient(new StubHttpMessageHandler(response));

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(nameof(JwksHealthCheck)))
            .Returns(httpClient);

        return (new JwksHealthCheck(factoryMock.Object, config), factoryMock);
    }

    private static (JwksHealthCheck sut, Mock<IHttpClientFactory> factoryMock) BuildSutWithException(
        string authority,
        Exception exception)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = authority
            })
            .Build();

        var httpClient = new HttpClient(new ThrowingHttpMessageHandler(exception));

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(nameof(JwksHealthCheck)))
            .Returns(httpClient);

        return (new JwksHealthCheck(factoryMock.Object, config), factoryMock);
    }

    private static HealthCheckContext BuildContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "jwks",
                Mock.Of<IHealthCheck>(),
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"])
        };
}

// ---------------------------------------------------------------------------
// Stub HTTP message handlers used only in JwksHealthCheckTests
// ---------------------------------------------------------------------------

/// <summary>Abstract base for Moq-compatible <see cref="HttpMessageHandler"/> stubs.</summary>
public abstract class DelegatingTestHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
}

/// <summary>Always returns a pre-configured <see cref="HttpResponseMessage"/>.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    internal StubHttpMessageHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}

/// <summary>Always throws the configured exception, simulating network failure.</summary>
internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    internal ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => throw _exception;
}
