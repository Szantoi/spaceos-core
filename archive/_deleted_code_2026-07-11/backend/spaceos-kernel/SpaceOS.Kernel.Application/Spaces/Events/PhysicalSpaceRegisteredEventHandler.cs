// SpaceOS.Kernel.Application/Spaces/Events/PhysicalSpaceRegisteredEventHandler.cs

using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Spaces.Events;

/// <summary>
/// Handles <see cref="PhysicalSpaceRegisteredEvent"/>: logs the physical space registration.
/// </summary>
public sealed class PhysicalSpaceRegisteredEventHandler
    : INotificationHandler<PhysicalSpaceRegisteredEvent>
{
    private readonly ILogger<PhysicalSpaceRegisteredEventHandler> _logger;

    /// <summary>Initialises a new <see cref="PhysicalSpaceRegisteredEventHandler"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PhysicalSpaceRegisteredEventHandler(ILogger<PhysicalSpaceRegisteredEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(PhysicalSpaceRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PhysicalSpace {PhysicalSpaceId} registered for tenant {TenantId} in facility {FacilityId}. Type={SpaceType}, Dimensions={WidthMm}x{HeightMm}x{DepthMm}mm.",
            notification.PhysicalSpaceId,
            notification.TenantId,
            notification.FacilityId,
            notification.SpaceType,
            notification.WidthMm,
            notification.HeightMm,
            notification.DepthMm);
        return Task.CompletedTask;
    }
}
