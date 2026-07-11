// SpaceOS.Infrastructure/Alerting/LoggingAlertService.cs

using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Alerting;

/// <summary>
/// Development implementation of <see cref="IAlertService"/> that emits alerts as structured
/// warning log entries. No external delivery is performed.
/// </summary>
internal sealed class LoggingAlertService : IAlertService
{
    private readonly ILogger<LoggingAlertService> _logger;

    /// <summary>Initialises a new <see cref="LoggingAlertService"/>.</summary>
    /// <param name="logger">The logger used to emit alert messages.</param>
    public LoggingAlertService(ILogger<LoggingAlertService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendAlertAsync(string alertType, string message, CancellationToken ct = default)
    {
        _logger.LogWarning("[ALERT:{AlertType}] {Message}", alertType, message);
        return Task.CompletedTask;
    }
}
