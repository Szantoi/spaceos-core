// Identity.Infrastructure/RateLimiting/RedisRateLimitService.cs

using Identity.Application.Common;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Identity.Infrastructure.RateLimiting;

public sealed class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase? _redis;
    private readonly ILogger<RedisRateLimitService> _logger;

    public RedisRateLimitService(IConnectionMultiplexer? redis, ILogger<RedisRateLimitService> logger)
    {
        _redis = redis?.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(
        string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        if (_redis is null)
        {
            _logger.LogWarning("Redis unavailable for rate limiting key={Key}, allowing by default.", key);
            return true;
        }

        try
        {
            var current = await _redis.StringIncrementAsync(key).ConfigureAwait(false);

            // Set expiry only on first increment (sliding window reset)
            if (current == 1)
                await _redis.KeyExpireAsync(key, window).ConfigureAwait(false);

            return current <= limit;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit check failed for key={Key}, allowing by default.", key);
            return true;
        }
    }
}
