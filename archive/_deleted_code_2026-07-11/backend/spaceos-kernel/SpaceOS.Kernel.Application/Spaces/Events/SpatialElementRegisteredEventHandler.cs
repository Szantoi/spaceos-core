// SpaceOS.Kernel.Application/Spaces/Events/SpatialElementRegisteredEventHandler.cs

using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Spaces.Events;

/// <summary>
/// Handles <see cref="SpatialElementRegisteredEvent"/>: logs the spatial element registration.
/// </summary>
public sealed class SpatialElementRegisteredEventHandler
    : INotificationHandler<SpatialElementRegisteredEvent>
{
    private readonly ILogger<SpatialElementRegisteredEventHandler> _logger;

    /// <summary>Initialises a new <see cref="SpatialElementRegisteredEventHandler"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public SpatialElementRegisteredEventHandler(ILogger<SpatialElementRegisteredEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(SpatialElementRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SpatialElement {ElementId} registered in PhysicalSpace {PhysicalSpaceId}. FlowEpic={FlowEpicId}, Trade={TradeType}.",
            notification.ElementId,
            notification.PhysicalSpaceId,
            notification.FlowEpicId,
            notification.TradeType);
        return Task.CompletedTask;
    }
}
