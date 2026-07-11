// SpaceOS.Infrastructure/Data/TenantContextMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SpaceOS.Infrastructure.Data;

/// <summary>
/// ASP.NET Core middleware that sets the PostgreSQL session variable
/// <c>app.current_tenant_id</c> via a parameterised <c>set_config</c> call so that
/// PostgreSQL Row-Level Security policies can read the current tenant without
/// trusting application-layer string concatenation.
/// </summary>
/// <remarks>
/// On SQLite (development / integration tests) the middleware no-ops because
/// <c>IsNpgsql()</c> returns <see langword="false"/>.
/// </remarks>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware with the next delegate in the pipeline.</summary>
    /// <param name="next">The next middleware to invoke.</param>
    public TenantContextMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>Sets the tenant session variable then invokes the next middleware.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="dbContext">The application DB context (scoped).</param>
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // Only meaningful on PostgreSQL — no-op for SQLite dev/test environments.
        if (dbContext.Database.IsNpgsql())
        {
            var tidClaim = context.User.FindFirst("tid")?.Value;

            if (tidClaim is not null)
            {
                if (!Guid.TryParse(tidClaim, out var tenantGuid))
                {
                    // Malformed GUID in the JWT — reject before the handler sees it.
                    throw new System.Security.SecurityException(
                        $"Malformed tenant ID claim: value is not a valid GUID.");
                }

                // EF Core interpolated SQL uses parameterised queries — never string concatenation.
                var tenantIdString = tenantGuid.ToString();
                await dbContext.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"SELECT set_config('app.current_tenant_id', {tenantIdString}, true)")
                    .ConfigureAwait(false);
            }
        }

        await _next(context).ConfigureAwait(false);
    }
}
