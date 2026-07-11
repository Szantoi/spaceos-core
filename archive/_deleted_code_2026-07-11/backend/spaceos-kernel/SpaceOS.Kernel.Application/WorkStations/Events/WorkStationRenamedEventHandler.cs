using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.WorkStations.Events;

/// <summary>
/// Handles <see cref="WorkStationRenamedEvent"/>: logs the name change.
/// </summary>
public sealed class WorkStationRenamedEventHandler : INotificationHandler<WorkStationRenamedEvent>
{
    private readonly ILogger<WorkStationRenamedEventHandler> _logger;

    public WorkStationRenamedEventHandler(ILogger<WorkStationRenamedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(WorkStationRenamedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WorkStation {WorkStationId} renamed from '{OldName}' to '{NewName}' at {OccurredOn}.",
            notification.WorkStationId, notification.OldName, notification.NewName, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
