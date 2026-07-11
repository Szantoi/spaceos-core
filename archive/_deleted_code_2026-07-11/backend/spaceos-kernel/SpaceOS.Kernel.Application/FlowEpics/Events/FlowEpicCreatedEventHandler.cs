using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.FlowEpics.Events;

/// <summary>
/// Handles <see cref="FlowEpicCreatedEvent"/>: logs the flow epic creation.
/// </summary>
public sealed class FlowEpicCreatedEventHandler : INotificationHandler<FlowEpicCreatedEvent>
{
    private readonly ILogger<FlowEpicCreatedEventHandler> _logger;

    public FlowEpicCreatedEventHandler(ILogger<FlowEpicCreatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(FlowEpicCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FlowEpic {FlowEpicId} created for facility {TargetFacilityId} at {OccurredOn}.",
            notification.FlowEpicId, notification.TargetFacilityId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
