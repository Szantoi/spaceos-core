using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.FlowEpics.Events;

/// <summary>
/// Handles <see cref="FlowEpicTitleUpdatedEvent"/>: logs the title change.
/// </summary>
public sealed class FlowEpicTitleUpdatedEventHandler : INotificationHandler<FlowEpicTitleUpdatedEvent>
{
    private readonly ILogger<FlowEpicTitleUpdatedEventHandler> _logger;

    public FlowEpicTitleUpdatedEventHandler(ILogger<FlowEpicTitleUpdatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(FlowEpicTitleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FlowEpic {FlowEpicId} title updated from '{OldTitle}' to '{NewTitle}' at {OccurredOn}.",
            notification.FlowEpicId, notification.OldTitle, notification.NewTitle, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
