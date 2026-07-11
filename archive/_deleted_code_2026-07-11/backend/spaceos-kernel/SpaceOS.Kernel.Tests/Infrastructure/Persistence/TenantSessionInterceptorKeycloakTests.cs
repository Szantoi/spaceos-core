// SpaceOS.Kernel.Tests/Infrastructure/Persistence/TenantSessionInterceptorKeycloakTests.cs
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Persistence;

/// <summary>
/// Unit tests for <see cref="TenantSessionInterceptor"/> covering the Keycloak
/// <c>spaceos_tenants</c> claim, <c>X-SpaceOS-Active-Tenant</c> header, malformed
/// claim fallback, and legacy backward-compat claim paths.
/// All tests use the <see cref="CapturingDbConnection"/> from the sibling test file.
/// </summary>
public sealed class TenantSessionInterceptorKeycloakTests
{
    // =========================================================================
    // TenantSessionInterceptor_Keycloak_Claims × 4
    // =========================================================================

    [Fact]
    public async Task ConnectionOpenedAsync_ValidSpaceosTenantsClaim_ResolvesFirstTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claimJson = BuildTenantsJson([tenantId.ToString()]);
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(claimJson);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — first tenant in the array must be used when no header is present
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_MultipleTenantsInClaim_UsesFirstTenantWhenNoHeader()
    {
        // Arrange — two tenants; without the X-SpaceOS-Active-Tenant header the first must be selected
        var firstTenantId  = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var claimJson = BuildTenantsJson([firstTenantId.ToString(), secondTenantId.ToString()]);
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(claimJson);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — soft-launch fallback: first entry wins
        Assert.Equal(firstTenantId.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_DoubleWrappedJsonClaim_ResolvesCorrectly()
    {
        // Arrange — Keycloak Script Mapper may JSON.stringify() the array, producing a
        // string-wrapped JSON value like "\"[{\\\"tenant_id\\\":\\\"...\\\"}]\"".
        // The interceptor must detect the outer wrapper and unwrap before parsing.
        var tenantId = Guid.NewGuid();
        var innerJson = BuildTenantsJson([tenantId.ToString()]);

        // Simulate Keycloak double-serialisation: the claim value is a JSON-encoded string
        // whose content is the actual array JSON.
        var doubleWrapped = JsonSerializer.Serialize(innerJson); // produces "\"[{...}]\""
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(doubleWrapped);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — unwrapped and parsed correctly
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_EmptyTenantsArray_FallsBackToLegacyTenantIdClaim()
    {
        // Arrange — spaceos_tenants claim is present but holds an empty array;
        // the interceptor must fall back to the flat legacy tenant_id claim.
        var legacyTenantId = Guid.NewGuid();
        var connection = new CapturingDbConnection();

        var claims = new List<Claim>
        {
            new("spaceos_tenants", "[]"),             // empty array — no tenants
            new("tenant_id", legacyTenantId.ToString())  // legacy fallback
        };
        var interceptor = BuildInterceptorFromClaims(claims, activeTenantHeader: null);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — legacy claim used because spaceos_tenants array was empty
        Assert.Equal(legacyTenantId.ToString(), connection.LastSetValue);
    }

    // =========================================================================
    // TenantSessionInterceptor_ActiveTenant_Validation × 3
    // =========================================================================

    [Fact]
    public async Task ConnectionOpenedAsync_ActiveTenantHeaderMatchesTenantList_UsesThatTenant()
    {
        // Arrange — multi-tenant user; header selects the second tenant explicitly
        var firstTenantId  = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var claimJson = BuildTenantsJson([firstTenantId.ToString(), secondTenantId.ToString()]);
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(claimJson, activeTenantHeader: secondTenantId.ToString());

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — header-selected tenant wins over positional first-entry fallback
        Assert.Equal(secondTenantId.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_ActiveTenantHeaderNotInList_ThrowsUnauthorizedAccessException()
    {
        // Arrange — header contains a GUID that does not appear in the tenant claim
        var tenantId = Guid.NewGuid();
        var claimJson = BuildTenantsJson([tenantId.ToString()]);
        var unknownTenantId = Guid.NewGuid();
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(claimJson, activeTenantHeader: unknownTenantId.ToString());

        // Act + Assert — must throw immediately, not fall back silently
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await interceptor.ConnectionOpenedAsync(
                connection,
                CreateOpenedEventData(),
                CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_NoHeader_SingleTenantInList_UsesFirstTenantAutoSelected()
    {
        // Arrange — single tenant in the claim, no header (typical single-tenant soft-launch)
        var tenantId = Guid.NewGuid();
        var claimJson = BuildTenantsJson([tenantId.ToString()]);
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithSpaceosTenants(claimJson, activeTenantHeader: null);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — auto-selected: first (and only) tenant is used
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    // =========================================================================
    // TenantSessionInterceptor_MalformedClaims × 3
    // =========================================================================

    [Fact]
    public async Task ConnectionOpenedAsync_MalformedSpaceosTenantsClaim_LogsWarningAndFallsBackToSentinel()
    {
        // Arrange — claim value is not valid JSON
        var connection = new CapturingDbConnection();
        var loggerMock = new Mock<ILogger<TenantSessionInterceptor>>();

        var claims = new List<Claim>
        {
            new("spaceos_tenants", "THIS IS NOT JSON")
        };
        var interceptor = BuildInterceptorFromClaims(claims, activeTenantHeader: null, loggerMock);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — falls back to sentinel (no tenant_id / tid claim present)
        Assert.Equal("00000000-0000-0000-0000-000000000001", connection.LastSetValue);

        // Logger must have been called with a warning level
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_SpaceosTenantsAbsent_LegacyTenantIdClaim_ResolvesCorrectly()
    {
        // Arrange — no spaceos_tenants claim; only the legacy tenant_id flat claim
        var tenantId = Guid.NewGuid();
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorFromClaims(
            [new Claim("tenant_id", tenantId.ToString())],
            activeTenantHeader: null);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — backward-compat: tenant_id used directly
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_SpaceosTenantsAbsent_TidClaim_ResolvesCorrectly()
    {
        // Arrange — no spaceos_tenants claim and no tenant_id; only the tid claim
        var tenantId = Guid.NewGuid();
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptorFromClaims(
            [new Claim("tid", tenantId.ToString())],
            activeTenantHeader: null);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(),
            CancellationToken.None).ConfigureAwait(false);

        // Assert — backward-compat: tid used as last resort
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Builds a <c>spaceos_tenants</c> JSON array with one <see cref="TenantEntry"/> per GUID.
    /// All non-ID fields use placeholder values so the required JSON properties are satisfied.
    /// </summary>
    private static string BuildTenantsJson(IEnumerable<string> tenantIds)
    {
        var entries = tenantIds.Select(id =>
            $$$"""{"tenant_id":"{{{id}}}","tenant_type":"Producer","enabled_modules":[],"brand_skin":"default"}""");
        return $"[{string.Join(",", entries)}]";
    }

    private static TenantSessionInterceptor BuildInterceptorWithSpaceosTenants(
        string spaceosTenantsClaimValue,
        string? activeTenantHeader = null)
    {
        var claims = new List<Claim>
        {
            new("spaceos_tenants", spaceosTenantsClaimValue)
        };
        return BuildInterceptorFromClaims(claims, activeTenantHeader);
    }

    private static TenantSessionInterceptor BuildInterceptorFromClaims(
        IEnumerable<Claim> claims,
        string? activeTenantHeader,
        Mock<ILogger<TenantSessionInterceptor>>? loggerMock = null)
    {
        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext { User = principal };
        if (activeTenantHeader is not null)
            context.Request.Headers["X-SpaceOS-Active-Tenant"] = activeTenantHeader;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);

        var logger = loggerMock ?? new Mock<ILogger<TenantSessionInterceptor>>();
        return new TenantSessionInterceptor(accessor.Object, logger.Object);
    }

    // Event data is not consumed by the interceptor logic under test — null is intentional.
    private static ConnectionEndEventData CreateOpenedEventData() => null!;
}
