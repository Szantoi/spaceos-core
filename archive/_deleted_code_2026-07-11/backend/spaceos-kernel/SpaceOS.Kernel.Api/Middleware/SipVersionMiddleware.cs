// SpaceOS.Kernel.Api/Middleware/SipVersionMiddleware.cs
using SpaceOS.Modules.Abstractions.Sync;

namespace SpaceOS.Kernel.Api.Middleware;

/// <summary>
/// BE-06 — SpaceOS Inter-node Protocol (SIP) versioning middleware.
/// Enforces the presence and value of the <c>SpaceOS-SIP-Version</c> header on all
/// <c>/api/sync/*</c> and <c>/api/nodes/*</c> requests.
/// Requests with a missing or unsupported version receive a 400 Problem Details response.
/// </summary>
internal sealed class SipVersionMiddleware
{
    private const string SipVersionHeader = "SpaceOS-SIP-Version";

    private static readonly string[] SupportedVersions = [SyncConstants.SipVersion];

    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware with the next delegate in the pipeline.</summary>
    /// <param name="next">The next request delegate.</param>
    public SipVersionMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>Inspects the SIP version header for relevant paths and short-circuits with 400 on violation.</summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsSipPath(path))
        {
            if (!context.Request.Headers.TryGetValue(SipVersionHeader, out var versions)
                || !SupportedVersions.Contains(versions.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type      = "https://httpstatuses.io/400",
                    title     = "Unsupported or missing SIP version",
                    status    = 400,
                    detail    = $"The '{SipVersionHeader}' header is required and must be one of the supported versions.",
                    supported = SupportedVersions
                };

                await context.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
                return;
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsSipPath(string path) =>
        path.StartsWith("/api/sync/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/nodes/", StringComparison.OrdinalIgnoreCase);
}
