// SpaceOS.Infrastructure/Extensions/RedisExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SpaceOS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Redis services with the DI container.
/// </summary>
public static class RedisExtensions
{
    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer"/> as a singleton and configures
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> backed by Redis
    /// (or in-memory fallback when <c>Redis:ConnectionString</c> is absent or the server is unreachable).
    /// </summary>
    /// <remarks>
    /// BE-P2-01: <see cref="IConnectionMultiplexer"/> is registered directly via
    /// <see cref="ServiceCollectionServiceExtensions.AddSingleton{TService}(IServiceCollection, TService)"/>
    /// — no <c>BuildServiceProvider()</c> anti-pattern.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="config">The application configuration used to resolve Redis settings.</param>
    /// <param name="startupLogger">
    /// Optional logger for startup-time warnings (fallback events). Pass <see langword="null"/>
    /// when a logger is not yet available (e.g., inside <see cref="DependencyInjection"/>).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSpaceOsRedis(
        this IServiceCollection services,
        IConfiguration config,
        ILogger? startupLogger = null)
    {
        var redisUrl = config["Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            startupLogger?.LogWarning(
                "Redis:ConnectionString is not configured. " +
                "Falling back to in-memory distributed cache. " +
                "Rate limiting state will not persist across restarts.");
            services.AddDistributedMemoryCache();
            return services;
        }

        // BE-P2-01 FIX: IConnectionMultiplexer registered as a singleton instance —
        // no BuildServiceProvider() call required.
        var redisOptions = ConfigurationOptions.Parse(redisUrl);

        var password = config["Redis:Password"];
        if (!string.IsNullOrWhiteSpace(password))
            redisOptions.Password = password;

        redisOptions.Ssl                = config.GetValue<bool>("Redis:UseTls", false);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout     = 3_000;
        redisOptions.SyncTimeout        = 2_000;
        redisOptions.KeepAlive          = 60;
        // ExponentialRetry(deltaBackoffMs, maxDeltaBackoffMs) — positional parameters in SE.Redis 2.8
        redisOptions.ReconnectRetryPolicy = new ExponentialRetry(500, 10_000);

        // Connect once — the multiplexer is thread-safe and designed for long-lived use.
        IConnectionMultiplexer multiplexer;
        try
        {
            multiplexer = ConnectionMultiplexer.Connect(redisOptions);
        }
        catch (Exception ex)
        {
            startupLogger?.LogWarning(ex,
                "Failed to connect to Redis at startup. " +
                "Falling back to in-memory distributed cache.");
            services.AddDistributedMemoryCache();
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        // Wire the already-created multiplexer into the Redis-backed IDistributedCache.
        // ConnectionMultiplexerFactory avoids a second parse/connect cycle.
        services.AddStackExchangeRedisCache(opts =>
            opts.ConnectionMultiplexerFactory = () =>
                Task.FromResult<IConnectionMultiplexer>(multiplexer));

        return services;
    }
}
