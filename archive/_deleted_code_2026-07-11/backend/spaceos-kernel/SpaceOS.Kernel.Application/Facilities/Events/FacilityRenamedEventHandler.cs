using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Facilities.Events;

/// <summary>
/// Handles <see cref="FacilityRenamedEvent"/>: logs the facility rename.
/// </summary>
public sealed class FacilityRenamedEventHandler : INotificationHandler<FacilityRenamedEvent>
{
    private readonly ILogger<FacilityRenamedEventHandler> _logger;

    public FacilityRenamedEventHandler(ILogger<FacilityRenamedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(FacilityRenamedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Facility {FacilityId} renamed from '{OldName}' to '{NewName}' at {OccurredOn}.",
            notification.FacilityId, notification.OldName, notification.NewName, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
