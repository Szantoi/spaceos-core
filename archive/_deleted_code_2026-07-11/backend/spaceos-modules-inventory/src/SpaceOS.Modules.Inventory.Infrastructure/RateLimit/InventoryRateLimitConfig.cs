using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace SpaceOS.Modules.Inventory.Infrastructure.RateLimit;

/// <summary>
/// Configures per-tenant sliding-window rate limits for the Inventory Reservation API.
/// Reserve and Release share a 100 req/min limit; Get uses a 60 req/min limit.
/// </summary>
public static class InventoryRateLimitConfig
{
    /// <summary>Rate-limit policy name for the Reserve endpoint.</summary>
    public const string ReservePolicy = "inventory-reserve";

    /// <summary>Rate-limit policy name for the Release endpoint.</summary>
    public const string ReleasePolicy = "inventory-release";

    /// <summary>Rate-limit policy name for the Get endpoint.</summary>
    public const string GetPolicy = "inventory-get";

    /// <summary>
    /// Registers the per-tenant sliding-window rate limiter with the DI container.
    /// </summary>
    public static IServiceCollection AddInventoryRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.AddPolicy<string, TenantRateLimitPolicy>(ReservePolicy);
            opts.AddPolicy<string, TenantRateLimitPolicy>(ReleasePolicy);
            opts.AddPolicy<string, TenantRateLimitPolicy>(GetPolicy);

            opts.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                ctx.HttpContext.Response.Headers["Retry-After"] = "60";
                await ctx.HttpContext.Response.WriteAsync("Rate limit exceeded", ct)
                    .ConfigureAwait(false);
            };
        });

        return services;
    }
}

/// <summary>
/// Per-tenant sliding-window rate limiter policy.
/// Reserve/Release: 100 requests per minute. Get: 60 requests per minute.
/// Partition key is the <c>tid</c> JWT claim; falls back to <c>"anonymous"</c>.
/// </summary>
internal sealed class TenantRateLimitPolicy : IRateLimiterPolicy<string>
{
    /// <inheritdoc/>
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var tenantId = httpContext.User?.FindFirst("tid")?.Value ?? "anonymous";

        // Get endpoint gets a lower limit (60/min vs 100/min for write operations)
        var isGetPath = httpContext.Request.Path.Value?.Contains("reservations/get",
            StringComparison.OrdinalIgnoreCase) == true;
        var permitLimit = isGetPath ? 60 : 100;

        return RateLimitPartition.GetSlidingWindowLimiter(tenantId, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                AutoReplenishment = true
            });
    }

    /// <inheritdoc/>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;
}
