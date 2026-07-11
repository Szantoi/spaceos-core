using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.SpaceLayers.Events;

/// <summary>
/// Handles <see cref="SpaceLayerRegisteredEvent"/>: logs the space layer registration.
/// </summary>
public sealed class SpaceLayerRegisteredEventHandler : INotificationHandler<SpaceLayerRegisteredEvent>
{
    private readonly ILogger<SpaceLayerRegisteredEventHandler> _logger;

    public SpaceLayerRegisteredEventHandler(ILogger<SpaceLayerRegisteredEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(SpaceLayerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SpaceLayer {SpaceLayerId} registered in facility {FacilityId} (trade: {TradeType}, external: {IsExternal}) at {OccurredOn}.",
            notification.SpaceLayerId, notification.FacilityId, notification.TradeType,
            notification.IsExternalNode, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
