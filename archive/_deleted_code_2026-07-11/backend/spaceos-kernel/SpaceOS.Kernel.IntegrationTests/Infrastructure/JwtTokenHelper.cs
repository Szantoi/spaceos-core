// SpaceOS.Kernel.IntegrationTests/Infrastructure/JwtTokenHelper.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>Generates RS256-signed JWT tokens for integration test scenarios.</summary>
public static class JwtTokenHelper
{
    private static readonly IConfiguration _config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.Testing.json", optional: false)
        .Build();

    // Ephemeral RSA key pair shared between token signing (JwtTokenHelper) and
    // token validation (SpaceOsApiFactory). Generated once per test process.
    internal static readonly RSA TestRsa = RSA.Create(2048);

    private static string Issuer =>
        _config["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer not found in appsettings.Testing.json");

    private static string Audience =>
        _config["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience not found in appsettings.Testing.json");

    /// <summary>Generates an RS256-signed JWT for the given role and tenant.</summary>
    /// <param name="role">The RBAC role claim value (e.g. "Admin", "Designer", "Joiner").</param>
    /// <param name="tenantId">The tenant identifier embedded in the <c>tid</c> claim.</param>
    /// <returns>A signed JWT Bearer token string.</returns>
    public static string GenerateToken(string role, Guid tenantId)
    {
        var key   = new RsaSecurityKey(TestRsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("tid", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Generates a token for the standard test tenant and given role.</summary>
    /// <param name="role">The RBAC role claim value.</param>
    /// <returns>A signed JWT Bearer token string scoped to <see cref="SpaceOsApiFactory.TestTenantId"/>.</returns>
    public static string ForRole(string role) =>
        GenerateToken(role, SpaceOsApiFactory.TestTenantId.Value);
}
