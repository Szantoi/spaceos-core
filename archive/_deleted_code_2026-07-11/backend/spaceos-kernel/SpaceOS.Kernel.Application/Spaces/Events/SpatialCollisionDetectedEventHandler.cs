// SpaceOS.Kernel.Application/Spaces/Events/SpatialCollisionDetectedEventHandler.cs

using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Spaces.Events;

/// <summary>
/// Handles <see cref="SpatialCollisionDetectedEvent"/>: logs a warning when two spatial elements
/// have overlapping bounding boxes.
/// </summary>
public sealed class SpatialCollisionDetectedEventHandler
    : INotificationHandler<SpatialCollisionDetectedEvent>
{
    private readonly ILogger<SpatialCollisionDetectedEventHandler> _logger;

    /// <summary>Initialises a new <see cref="SpatialCollisionDetectedEventHandler"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public SpatialCollisionDetectedEventHandler(ILogger<SpatialCollisionDetectedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(SpatialCollisionDetectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Spatial collision detected between elements {ElementIdA} and {ElementIdB}. Intersection volume: ({MinX},{MinY},{MinZ})-({MaxX},{MaxY},{MaxZ}).",
            notification.ElementIdA,
            notification.ElementIdB,
            notification.IntersectionVolume.MinX,
            notification.IntersectionVolume.MinY,
            notification.IntersectionVolume.MinZ,
            notification.IntersectionVolume.MaxX,
            notification.IntersectionVolume.MaxY,
            notification.IntersectionVolume.MaxZ);
        return Task.CompletedTask;
    }
}
