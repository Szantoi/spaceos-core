// SpaceOS.Kernel.Api.Tests/Infrastructure/JwtTestHelper.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SpaceOS.Kernel.Api.Tests.Infrastructure;

/// <summary>Generates RS256-signed JWT tokens for Api.Tests endpoint test scenarios.</summary>
internal static class JwtTestHelper
{
    private static readonly IConfiguration _config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.Testing.json", optional: false)
        .Build();

    // Ephemeral RSA key pair shared between token signing (JwtTestHelper) and
    // token validation (ApiFactory). Generated once per test process.
    internal static readonly RSA TestRsa = RSA.Create(2048);

    private static string Issuer =>
        _config["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer not found in appsettings.Testing.json");

    private static string Audience =>
        _config["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience not found in appsettings.Testing.json");

    /// <summary>
    /// Generates an RS256-signed JWT for the given role with <see cref="ApiFactory.TestTenantId"/> as the <c>tid</c> claim.
    /// </summary>
    /// <param name="role">The RBAC role claim value (e.g. "Admin", "Designer", "Joiner").</param>
    /// <returns>A signed JWT Bearer token string.</returns>
    internal static string ForRole(string role)
    {
        var key   = new RsaSecurityKey(TestRsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("tid", ApiFactory.TestTenantId.Value.ToString()),
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

    /// <summary>
    /// Generates an RS256-signed JWT that carries <c>spaceos_tenants</c> claims
    /// <em>instead of</em> a flat <c>tid</c> claim — simulating the Keycloak KC-T2 token
    /// format where ASP.NET <see cref="Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler"/>
    /// has already split the JSON array into individual <see cref="Claim"/> objects
    /// (each value is a JSON object string like <c>{"tenant_id":"..."}</c>).
    /// </summary>
    /// <param name="role">The RBAC role claim value.</param>
    /// <param name="tenantIds">
    /// Tenant GUIDs to embed as separate <c>spaceos_tenants</c> claims.
    /// Defaults to a single-element array containing <see cref="ApiFactory.TestTenantId"/>.
    /// </param>
    /// <returns>A signed JWT Bearer token string.</returns>
    internal static string ForRoleWithSpaceosTenantsOnly(string role, Guid[]? tenantIds = null)
    {
        tenantIds ??= [ApiFactory.TestTenantId.Value];

        var key   = new RsaSecurityKey(TestRsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        // Each array element becomes an individual Claim — mirroring ASP.NET's behaviour
        foreach (var id in tenantIds)
            claims.Add(new Claim("spaceos_tenants", $"{{\"tenant_id\":\"{id}\"}}"));

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
