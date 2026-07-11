// SpaceOS.Kernel.Tests/Infrastructure/Auth/HttpContextCurrentRequestContextTests.cs

using Microsoft.AspNetCore.Http;
using Moq;
using SpaceOS.Infrastructure.Auth;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Auth;

/// <summary>
/// Unit tests for <see cref="HttpContextCurrentRequestContext"/>.
/// Focuses on <c>SourceBrand</c> allowlist validation from the <c>X-SpaceOS-Brand</c> header.
/// </summary>
public sealed class HttpContextCurrentRequestContextTests
{
    private static HttpContextCurrentRequestContext BuildContext(string? brandHeaderValue)
    {
        var httpContext = new DefaultHttpContext();
        if (brandHeaderValue is not null)
            httpContext.Request.Headers["X-SpaceOS-Brand"] = brandHeaderValue;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        return new HttpContextCurrentRequestContext(accessor.Object);
    }

    [Fact]
    public void SourceBrand_Joinerytech_ReturnsJoinerytech()
    {
        var ctx = BuildContext("joinerytech");
        Assert.Equal("joinerytech", ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_Asztalostech_ReturnsAsztalostech()
    {
        var ctx = BuildContext("asztalostech");
        Assert.Equal("asztalostech", ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_CaseInsensitive_ReturnsLowercase()
    {
        var ctx = BuildContext("JoineryTech");
        Assert.Equal("joinerytech", ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_Unknown_ReturnsNull()
    {
        var ctx = BuildContext("hacker");
        Assert.Null(ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_Missing_ReturnsNull()
    {
        var ctx = BuildContext(null);
        Assert.Null(ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_Empty_ReturnsNull()
    {
        var ctx = BuildContext("");
        Assert.Null(ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_Whitespace_ReturnsNull()
    {
        var ctx = BuildContext("   ");
        Assert.Null(ctx.SourceBrand);
    }

    [Fact]
    public void SourceBrand_NullHttpContext_ReturnsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var ctx = new HttpContextCurrentRequestContext(accessor.Object);
        Assert.Null(ctx.SourceBrand);
    }
}
