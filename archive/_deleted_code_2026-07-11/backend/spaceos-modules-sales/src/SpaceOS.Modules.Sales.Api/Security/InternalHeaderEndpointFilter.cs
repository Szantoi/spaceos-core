using Microsoft.Extensions.Configuration;

namespace SpaceOS.Modules.Sales.Api.Security;

/// <summary>
/// Endpoint filter that validates the <c>X-SpaceOS-Internal</c> header on loopback/internal
/// endpoints. Returns 503 if the secret is not configured, 401 if it does not match.
/// </summary>
internal sealed class InternalHeaderEndpointFilter(IConfiguration cfg) : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var secret = cfg["SpaceOS:InternalSecret"];
        if (string.IsNullOrEmpty(secret))
            return Results.Problem("Internal secret not configured.", statusCode: 503);

        var header = ctx.HttpContext.Request.Headers["X-SpaceOS-Internal"].FirstOrDefault();
        if (header != secret)
            return Results.Unauthorized();

        return await next(ctx).ConfigureAwait(false);
    }
}
