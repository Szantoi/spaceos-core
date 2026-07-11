using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Facilities.Events;

/// <summary>
/// Handles <see cref="FacilityCreatedEvent"/>: logs the facility creation.
/// </summary>
public sealed class FacilityCreatedEventHandler : INotificationHandler<FacilityCreatedEvent>
{
    private readonly ILogger<FacilityCreatedEventHandler> _logger;

    public FacilityCreatedEventHandler(ILogger<FacilityCreatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(FacilityCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Facility {FacilityId} created for tenant {TenantId} at {OccurredOn}.",
            notification.FacilityId, notification.TenantId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
