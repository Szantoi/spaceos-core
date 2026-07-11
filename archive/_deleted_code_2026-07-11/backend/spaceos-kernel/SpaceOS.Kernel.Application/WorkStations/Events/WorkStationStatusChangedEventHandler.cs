using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.WorkStations.Events;

/// <summary>
/// Handles <see cref="WorkStationStatusChangedEvent"/> by logging the status transition.
/// </summary>
public sealed class WorkStationStatusChangedEventHandler : INotificationHandler<WorkStationStatusChangedEvent>
{
    private readonly ILogger<WorkStationStatusChangedEventHandler> _logger;

    /// <summary>
    /// Initialises a new <see cref="WorkStationStatusChangedEventHandler"/>.
    /// </summary>
    /// <param name="logger">Logger for recording status change telemetry.</param>
    public WorkStationStatusChangedEventHandler(ILogger<WorkStationStatusChangedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(WorkStationStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WorkStation {WorkStationId} status changed from {OldStatus} to {NewStatus} at {OccurredOn}",
            notification.WorkStationId.Value,
            notification.OldStatus,
            notification.NewStatus,
            notification.OccurredOn);

        return Task.CompletedTask;
    }
}
