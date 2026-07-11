// SpaceOS.Infrastructure/Auth/HttpContextCurrentRequestContext.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Infrastructure implementation of <see cref="ICurrentRequestContext"/> that extracts
/// actor identity and source IP from the current <see cref="HttpContext"/>.
/// </summary>
internal sealed class HttpContextCurrentRequestContext : ICurrentRequestContext
{
    private static readonly HashSet<string> AllowedBrands =
        new(StringComparer.OrdinalIgnoreCase) { "joinerytech", "asztalostech" };

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initialises a new <see cref="HttpContextCurrentRequestContext"/>.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    public HttpContextCurrentRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public string? ActorId =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <inheritdoc/>
    public string? SourceIp
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            // Respect X-Forwarded-For when behind a reverse proxy.
            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    /// <inheritdoc/>
    public string? SourceBrand
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            var raw = ctx.Request.Headers["X-SpaceOS-Brand"]
                .FirstOrDefault()?.Trim().ToLowerInvariant();

            return raw is not null && AllowedBrands.Contains(raw) ? raw : null;
        }
    }
}
