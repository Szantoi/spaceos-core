using MediatR;
using Microsoft.Extensions.Logging;

namespace SpaceOS.Modules.Sales.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request entry and exit with timing. BE-S-08.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> log)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        log.LogDebug("Handling {RequestType}", name);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        sw.Stop();
        log.LogDebug("Handled {RequestType} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
        return result;
    }
}
