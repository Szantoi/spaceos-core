// SpaceOS.Infrastructure/Health/JwksHealthCheck.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SpaceOS.Infrastructure.Health;

/// <summary>
/// Health check that verifies the Keycloak JWKS endpoint is reachable.
/// Registered with tag <c>ready</c> so it is included in the <c>/health/ready</c> probe.
/// </summary>
internal sealed class JwksHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    /// <summary>Initialises the health check.</summary>
    /// <param name="httpClientFactory">Factory used to create the HTTP client for JWKS requests.</param>
    /// <param name="config">Application configuration used to resolve <c>Jwt:Authority</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either parameter is <c>null</c>.</exception>
    public JwksHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(config);
        _httpClientFactory = httpClientFactory;
        _config            = config;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var authority = _config["Jwt:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
            return HealthCheckResult.Degraded("Jwt:Authority is not configured");

        var jwksUri = $"{authority.TrimEnd('/')}/protocol/openid-connect/certs";
        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(JwksHealthCheck));
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = await client.GetAsync(jwksUri, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("JWKS endpoint reachable")
                : HealthCheckResult.Degraded($"JWKS returned {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("JWKS endpoint unreachable", ex);
        }
    }
}
