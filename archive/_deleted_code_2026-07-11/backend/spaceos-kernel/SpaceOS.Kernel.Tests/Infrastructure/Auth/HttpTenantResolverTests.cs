// SpaceOS.Kernel.Tests/Infrastructure/Auth/HttpTenantResolverTests.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using SpaceOS.Infrastructure.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Auth;

/// <summary>
/// Unit tests for <see cref="ClaimsTenantResolver"/>.
/// </summary>
public sealed class HttpTenantResolverTests
{
    private readonly Mock<IHttpContextAccessor> _accessor = new();

    [Fact]
    public void TryResolve_Returns_Null_When_HttpContext_Is_Null()
    {
        // Arrange
        _accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var resolver = new ClaimsTenantResolver(_accessor.Object);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_Returns_DenySentinel_When_Claim_Absent()
    {
        // Arrange — HTTP context present, but no tid claim → deny sentinel, not null
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        _accessor.Setup(a => a.HttpContext).Returns(httpContext);
        var resolver = new ClaimsTenantResolver(_accessor.Object);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ClaimsTenantResolver.DenyWebRequestSentinel, result);
    }

    [Fact]
    public void TryResolve_Returns_DenySentinel_When_Claim_Is_Not_A_Guid()
    {
        // Arrange — HTTP context present, malformed tid → deny sentinel, not null
        var claims = new[] { new Claim("tid", "not-a-guid") };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };
        _accessor.Setup(a => a.HttpContext).Returns(httpContext);
        var resolver = new ClaimsTenantResolver(_accessor.Object);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ClaimsTenantResolver.DenyWebRequestSentinel, result);
    }

    [Fact]
    public void TryResolve_Returns_TenantId_When_Claim_Is_Valid_Guid()
    {
        // Arrange
        var tenantGuid = Guid.NewGuid();
        var claims = new[] { new Claim("tid", tenantGuid.ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };
        _accessor.Setup(a => a.HttpContext).Returns(httpContext);
        var resolver = new ClaimsTenantResolver(_accessor.Object);

        // Act
        var result = resolver.TryResolve();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TenantId.From(tenantGuid), result);
    }
}
