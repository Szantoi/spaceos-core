using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.SpaceLayers.Events;

/// <summary>
/// Handles <see cref="SpaceLayerIntentUpdatedEvent"/>: logs intent data updates.
/// </summary>
public sealed class SpaceLayerIntentUpdatedEventHandler : INotificationHandler<SpaceLayerIntentUpdatedEvent>
{
    private readonly ILogger<SpaceLayerIntentUpdatedEventHandler> _logger;

    /// <summary>
    /// Initialises a new <see cref="SpaceLayerIntentUpdatedEventHandler"/>.
    /// </summary>
    /// <param name="logger">Logger for recording intent update notifications.</param>
    public SpaceLayerIntentUpdatedEventHandler(ILogger<SpaceLayerIntentUpdatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(SpaceLayerIntentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SpaceLayer {SpaceLayerId} intent data updated. New hash: {Hash} at {OccurredOn}.",
            notification.SpaceLayerId, notification.NewHash, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
