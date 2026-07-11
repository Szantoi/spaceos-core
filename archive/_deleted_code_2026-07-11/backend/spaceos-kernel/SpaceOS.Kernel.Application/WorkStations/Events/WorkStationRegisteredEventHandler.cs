using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.WorkStations.Events;

/// <summary>
/// Handles <see cref="WorkStationRegisteredEvent"/>: logs the workstation registration.
/// </summary>
public sealed class WorkStationRegisteredEventHandler : INotificationHandler<WorkStationRegisteredEvent>
{
    private readonly ILogger<WorkStationRegisteredEventHandler> _logger;

    public WorkStationRegisteredEventHandler(ILogger<WorkStationRegisteredEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(WorkStationRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WorkStation {WorkStationId} registered in facility {FacilityId} at {OccurredOn}.",
            notification.WorkStationId, notification.FacilityId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
