using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SpaceOS.Infrastructure.Security;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

public sealed class InternalHeaderMiddlewareTests
{
    private static InternalHeaderMiddleware CreateSut(
        RequestDelegate next, string secret = "test-secret")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SpaceOS:InternalSecret"] = secret
            })
            .Build();
        return new InternalHeaderMiddleware(next, config);
    }

    [Fact]
    public async Task InvokeAsync_MissingHeader_Returns401()
    {
        // Arrange
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();

        // Act
        await sut.InvokeAsync(ctx);

        // Assert
        Assert.Equal(401, ctx.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WrongSecret_Returns401()
    {
        // Arrange
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        ctx.Request.Headers["X-SpaceOS-Internal"] = "wrong-secret";

        // Act
        await sut.InvokeAsync(ctx);

        // Assert
        Assert.Equal(401, ctx.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_CorrectSecret_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-SpaceOS-Internal"] = "test-secret";

        // Act
        await sut.InvokeAsync(ctx);

        // Assert
        Assert.True(nextCalled);
    }
}
