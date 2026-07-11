// SpaceOS.Kernel.Tests/Infrastructure/RedisExtensionsTests.cs
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Infrastructure.Extensions;
using StackExchange.Redis;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="RedisExtensions.AddSpaceOsRedis"/>.
/// Verifies the fallback-to-in-memory behaviour when Redis is not configured (BE-P2-01).
/// </summary>
public sealed class RedisExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddSpaceOsRedis_NoConnectionString_RegistersInMemoryCache
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that when <c>Redis:ConnectionString</c> is absent, the in-memory
    /// distributed cache is registered and no multiplexer is added to the container.
    /// </summary>
    [Fact]
    public void AddSpaceOsRedis_NoConnectionString_RegistersInMemoryCache()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddSpaceOsRedis(config);
        var sp = services.BuildServiceProvider();

        // Assert — IDistributedCache is registered (in-memory fallback)
        var cache = sp.GetService<IDistributedCache>();
        Assert.NotNull(cache);

        // IConnectionMultiplexer must NOT be registered (no Redis config)
        var multiplexer = sp.GetService<IConnectionMultiplexer>();
        Assert.Null(multiplexer);
    }

    // -------------------------------------------------------------------------
    // AddSpaceOsRedis_EmptyConnectionString_RegistersInMemoryCache
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an explicit empty <c>Redis:ConnectionString</c> value also
    /// falls back to the in-memory cache and omits the multiplexer registration.
    /// </summary>
    [Fact]
    public void AddSpaceOsRedis_EmptyConnectionString_RegistersInMemoryCache()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = ""
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddSpaceOsRedis(config);
        var sp = services.BuildServiceProvider();

        // Assert
        var cache = sp.GetService<IDistributedCache>();
        Assert.NotNull(cache);
        Assert.Null(sp.GetService<IConnectionMultiplexer>());
    }

    // -------------------------------------------------------------------------
    // AddSpaceOsRedis_WhitespaceConnectionString_RegistersInMemoryCache
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a whitespace-only <c>Redis:ConnectionString</c> also triggers
    /// the in-memory fallback — guards against config typos.
    /// </summary>
    [Fact]
    public void AddSpaceOsRedis_WhitespaceConnectionString_RegistersInMemoryCache()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "   "
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddSpaceOsRedis(config);
        var sp = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(sp.GetService<IDistributedCache>());
        Assert.Null(sp.GetService<IConnectionMultiplexer>());
    }
}
