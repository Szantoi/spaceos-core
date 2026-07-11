// SpaceOS.Infrastructure/Auth/ClaimsTenantResolver.cs
using Microsoft.AspNetCore.Http;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Resolves the current tenant by reading the <c>tid</c> JWT claim from the active
/// <see cref="HttpContext"/>.
/// <list type="bullet">
///   <item><description>
///     Returns <c>null</c> when no HTTP context is available (background jobs, migrations) —
///     the caller (AppDbContext) treats null as admin bypass (all tenants visible).
///   </description></item>
///   <item><description>
///     Returns <see cref="DenyWebRequestSentinel"/> when an HTTP context is present but the
///     <c>tid</c> claim is absent or malformed — the sentinel matches no real tenant so
///     AppDbContext global query filters return empty result sets (not a bypass).
///   </description></item>
/// </list>
/// </summary>
internal sealed class ClaimsTenantResolver : ITenantResolver
{
    private const string TenantIdClaim = "tid";

    /// <summary>
    /// Sentinel value returned for authenticated web requests that carry no valid <c>tid</c> claim.
    /// Because no real tenant will ever have this GUID, EF Core global query filters produce
    /// empty result sets — preventing unintended cross-tenant data exposure.
    /// </summary>
    internal static readonly TenantId DenyWebRequestSentinel =
        TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"));

    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Initialises a new <see cref="ClaimsTenantResolver"/>.
    /// </summary>
    /// <param name="accessor">The HTTP context accessor used to read the current request context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="accessor"/> is <c>null</c>.</exception>
    public ClaimsTenantResolver(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    /// <inheritdoc/>
    public TenantId? TryResolve()
    {
        var context = _accessor.HttpContext;

        // No HTTP context → background job / migration → null signals bypass to AppDbContext
        if (context is null)
            return null;

        var claim = context.User.Claims.FirstOrDefault(c => c.Type == TenantIdClaim);

        // HTTP context present but no valid tid claim → deny: return sentinel that matches no tenant
        if (claim is null)
            return DenyWebRequestSentinel;

        if (!Guid.TryParse(claim.Value, out var guid))
            return DenyWebRequestSentinel;

        if (guid == Guid.Empty)
            return DenyWebRequestSentinel;

        return TenantId.From(guid);
    }
}
