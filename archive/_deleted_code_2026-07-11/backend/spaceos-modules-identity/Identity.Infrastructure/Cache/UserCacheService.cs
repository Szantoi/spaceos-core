// Identity.Infrastructure/Cache/UserCacheService.cs

using System.Text.Json;
using Identity.Application.Common.DTOs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Identity.Infrastructure.Cache;

public sealed class UserCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private readonly IDatabase? _redis;
    private readonly ILogger<UserCacheService> _logger;

    public UserCacheService(IConnectionMultiplexer? redis, ILogger<UserCacheService> logger)
    {
        _redis = redis?.GetDatabase();
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserDto>?> GetCachedUsersAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        if (_redis is null) return null;

        try
        {
            var value = await _redis.StringGetAsync(CacheKey(tenantId)).ConfigureAwait(false);
            if (!value.HasValue) return null;

            return JsonSerializer.Deserialize<List<UserDto>>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task SetCachedUsersAsync(
        Guid tenantId, IReadOnlyList<UserDto> users, CancellationToken ct = default)
    {
        if (_redis is null) return;

        try
        {
            var json = JsonSerializer.Serialize(users);
            await _redis.StringSetAsync(CacheKey(tenantId), json, Ttl).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for tenant {TenantId}", tenantId);
        }
    }

    public async Task InvalidateAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (_redis is null) return;

        try
        {
            await _redis.KeyDeleteAsync(CacheKey(tenantId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache invalidation failed for tenant {TenantId}", tenantId);
        }
    }

    private static string CacheKey(Guid tenantId) => $"identity:users:{tenantId}";
}
