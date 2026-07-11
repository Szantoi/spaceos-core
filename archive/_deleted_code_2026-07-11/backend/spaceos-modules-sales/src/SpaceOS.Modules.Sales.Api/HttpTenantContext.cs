using Microsoft.AspNetCore.Http;
using SpaceOS.Modules.Sales.Application.Common;

namespace SpaceOS.Modules.Sales.Api;

/// <summary>
/// ITenantContext implementation backed by the JWT claims from the current HTTP request.
/// SEC-S-07: extracts tenant_id and sub claims.
/// </summary>
internal sealed class HttpTenantContext(IHttpContextAccessor http) : ITenantContext
{
    /// <inheritdoc/>
    public Guid TenantId
    {
        get
        {
            var val = http.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(val, out var g) ? g : Guid.Empty;
        }
    }

    /// <inheritdoc/>
    public string ActorSub
        => http.HttpContext?.User.FindFirst("sub")?.Value ?? "unknown";
}
