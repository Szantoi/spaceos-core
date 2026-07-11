// SpaceOS.Kernel.Api/Middleware/ExceptionHandlingMiddleware.cs
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Api.Middleware;

/// <summary>
/// Global exception-handling middleware. Catches unhandled exceptions and
/// converts them to RFC 7807 Problem Details responses — no stack traces ever leave the process.
/// </summary>
internal sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Initialises the middleware with the next delegate and a logger.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the middleware, forwarding to the next delegate and catching any unhandled exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, 400, "Domain Rule Violation", ex.Message)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, 500, "Internal Server Error",
                "An unexpected error occurred.")
                .ConfigureAwait(false);
        }
    }

    private static Task WriteProblemAsync(
        HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type     = $"https://httpstatuses.io/{status}",
            Title    = title,
            Status   = status,
            Detail   = detail,
            Instance = context.Request.Path
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
