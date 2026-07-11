// SpaceOS.Kernel.Tests/Infrastructure/Auth/ClaimsTenantResolverTests.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using SpaceOS.Infrastructure.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Auth;

/// <summary>
/// Unit tests for <see cref="ClaimsTenantResolver"/>.
/// Verifies correct extraction of the <c>tid</c> JWT claim and all error paths.
/// </summary>
public sealed class ClaimsTenantResolverTests
{
    private static ClaimsTenantResolver BuildResolver(HttpContext? context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return new ClaimsTenantResolver(accessor.Object);
    }

    private static HttpContext BuildContext(IEnumerable<Claim> claims)
    {
        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new DefaultHttpContext { User = principal };
    }

    // -------------------------------------------------------------------------
    // Valid tid claim → resolves to correct TenantId
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolve_ValidTidClaim_ReturnsCorrectTenantId()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var context  = BuildContext([new Claim("tid", expected.ToString())]);
        var resolver = BuildResolver(context);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TenantId.From(expected), result);
    }

    // -------------------------------------------------------------------------
    // No tid claim in web context → returns DenyWebRequestSentinel (not null)
    // Sentinel ensures AppDbContext filter returns empty, not bypass
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolve_NoTidClaim_InWebContext_ReturnsDenySentinel()
    {
        // Arrange
        var context  = BuildContext([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]);
        var resolver = BuildResolver(context);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ClaimsTenantResolver.DenyWebRequestSentinel, result);
    }

    // -------------------------------------------------------------------------
    // Malformed tid in web context → returns DenyWebRequestSentinel (not null)
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolve_MalformedTidClaim_InWebContext_ReturnsDenySentinel()
    {
        // Arrange
        var context  = BuildContext([new Claim("tid", "not-a-guid")]);
        var resolver = BuildResolver(context);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ClaimsTenantResolver.DenyWebRequestSentinel, result);
    }

    // -------------------------------------------------------------------------
    // Guid.Empty tid in web context → returns DenyWebRequestSentinel (not null)
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolve_EmptyGuidTidClaim_InWebContext_ReturnsDenySentinel()
    {
        // Arrange
        var context  = BuildContext([new Claim("tid", Guid.Empty.ToString())]);
        var resolver = BuildResolver(context);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ClaimsTenantResolver.DenyWebRequestSentinel, result);
    }

    // -------------------------------------------------------------------------
    // No HttpContext (background job) → returns null
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolve_NoHttpContext_ReturnsNull()
    {
        // Arrange
        var resolver = BuildResolver(context: null);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.Null(result);
    }
}
