using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SpaceOS.Infrastructure.Security;

/// <summary>
/// Guards /api/internal/* routes with a shared secret header (SEC-S-09).
/// Applied via UseWhen — does NOT affect JWT-authenticated /api/* routes.
/// </summary>
public sealed class InternalHeaderMiddleware(RequestDelegate next, IConfiguration config)
{
    /// <summary>Validates the <c>X-SpaceOS-Internal</c> header and either short-circuits with 401 or calls the next delegate.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var expectedSecret = config["SpaceOS:InternalSecret"];

        // Deny all access when the secret is not configured (returns 401, does not crash startup).
        // Production deployments MUST set SpaceOS:InternalSecret via environment variable.
        if (expectedSecret is null
            || !context.Request.Headers.TryGetValue("X-SpaceOS-Internal", out var header)
            || !string.Equals(header.FirstOrDefault(), expectedSecret, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"Unauthorized","detail":"Missing or invalid X-SpaceOS-Internal header."}""",
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
