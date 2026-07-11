// SpaceOS.Kernel.Api/Middleware/RequestBodySizeLimitFilter.cs

namespace SpaceOS.Kernel.Api.Middleware;

/// <summary>
/// Endpoint filter that rejects requests whose <c>Content-Length</c> header exceeds 64 KB.
/// This enforces the same limit as the Kestrel <c>MaxRequestBodySize</c> setting so that
/// integration tests (which use <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> and
/// therefore bypass Kestrel) also receive a 413 response for oversized payloads.
/// </summary>
internal sealed class RequestBodySizeLimitFilter : IEndpointFilter
{
    private const long MaxBodyBytes = 64 * 1024; // 64 KB

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var contentLength = context.HttpContext.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > MaxBodyBytes)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status413RequestEntityTooLarge;
            return Results.Problem(
                detail:   $"Request body must not exceed {MaxBodyBytes} bytes (64 KB).",
                title:    "Request Entity Too Large",
                statusCode: 413,
                type:     "https://httpstatuses.io/413");
        }

        return await next(context).ConfigureAwait(false);
    }
}
