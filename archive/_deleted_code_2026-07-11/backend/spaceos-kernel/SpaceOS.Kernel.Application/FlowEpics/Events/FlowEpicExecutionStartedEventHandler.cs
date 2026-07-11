using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.FlowEpics.Events;

/// <summary>
/// Handles <see cref="FlowEpicExecutionStartedEvent"/>: logs the phase transition.
/// </summary>
public sealed class FlowEpicExecutionStartedEventHandler : INotificationHandler<FlowEpicExecutionStartedEvent>
{
    private readonly ILogger<FlowEpicExecutionStartedEventHandler> _logger;

    public FlowEpicExecutionStartedEventHandler(ILogger<FlowEpicExecutionStartedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(FlowEpicExecutionStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FlowEpic {FlowEpicId} execution started (Discovery → Delivery) at {OccurredOn}.",
            notification.FlowEpicId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
