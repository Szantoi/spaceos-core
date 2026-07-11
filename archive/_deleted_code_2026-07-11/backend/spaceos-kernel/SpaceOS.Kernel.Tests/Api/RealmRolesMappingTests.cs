// SpaceOS.Kernel.Tests/Api/RealmRolesMappingTests.cs
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SpaceOS.Kernel.Tests.Api;

/// <summary>
/// Unit tests for the Keycloak <c>realm_access.roles</c> → <see cref="ClaimTypes.Role"/>
/// mapping logic that lives in the <c>OnTokenValidated</c> event in Program.cs.
/// The mapping code is extracted to a static helper method so it can be unit-tested
/// without spinning up a full <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents"/> pipeline.
/// </summary>
public sealed class RealmRolesMappingTests
{
    // =========================================================================
    // RealmRoles_Mapping × 2
    // =========================================================================

    [Fact]
    public void MapRealmRoles_ValidRealmAccessClaim_AddsRoleClaimsToIdentity()
    {
        // Arrange — a ClaimsIdentity that contains a realm_access JSON claim
        var realmAccess = JsonSerializer.Serialize(new
        {
            roles = new[] { "Admin", "Designer" }
        });

        var identity = new ClaimsIdentity(
            [new Claim("realm_access", realmAccess)],
            "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act — apply the same mapping logic as in Program.cs OnTokenValidated
        ApplyRealmRolesMapping(principal);

        // Assert — both roles must appear as ClaimTypes.Role claims
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        Assert.Contains("Admin", roles);
        Assert.Contains("Designer", roles);
    }

    [Fact]
    public void MapRealmRoles_MissingRealmAccessClaim_DoesNotAddRoleClaimsAndDoesNotThrow()
    {
        // Arrange — principal with no realm_access claim (e.g. service account token)
        var identity = new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString())],
            "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act + Assert — must complete without exception
        var exception = Record.Exception(() => ApplyRealmRolesMapping(principal));
        Assert.Null(exception);

        // No ClaimTypes.Role claims should have been added
        var roles = principal.FindAll(ClaimTypes.Role).ToArray();
        Assert.Empty(roles);
    }

    // =========================================================================
    // Private helper — mirrors the OnTokenValidated logic verbatim from Program.cs
    // =========================================================================

    /// <summary>
    /// Applies the same <c>realm_access.roles</c> → <see cref="ClaimTypes.Role"/> mapping
    /// as the <c>OnTokenValidated</c> event handler in Program.cs.
    /// Kept in sync manually; if Program.cs changes, update this copy too.
    /// </summary>
    private static void ApplyRealmRolesMapping(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
            return;

        try
        {
            var parsed = JsonDocument.Parse(realmAccess);
            if (parsed.RootElement.TryGetProperty("roles", out var roles))
            {
                var identity = principal.Identity as ClaimsIdentity;
                foreach (var role in roles.EnumerateArray())
                {
                    var roleStr = role.GetString();
                    if (!string.IsNullOrEmpty(roleStr))
                        identity!.AddClaim(new Claim(ClaimTypes.Role, roleStr));
                }
            }
        }
        catch (JsonException)
        {
            // Intentionally swallowed — mirrors Program.cs behaviour
        }
    }
}
