using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.WorkStations.Events;

/// <summary>
/// Handles <see cref="WorkStationReassignedEvent"/>: logs the facility reassignment.
/// </summary>
public sealed class WorkStationReassignedEventHandler : INotificationHandler<WorkStationReassignedEvent>
{
    private readonly ILogger<WorkStationReassignedEventHandler> _logger;

    public WorkStationReassignedEventHandler(ILogger<WorkStationReassignedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(WorkStationReassignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WorkStation {WorkStationId} reassigned from facility {OldFacilityId} to {NewFacilityId} at {OccurredOn}.",
            notification.WorkStationId, notification.OldFacilityId, notification.NewFacilityId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
