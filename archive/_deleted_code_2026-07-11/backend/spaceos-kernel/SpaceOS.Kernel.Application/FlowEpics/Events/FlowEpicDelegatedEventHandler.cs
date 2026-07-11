using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.FlowEpics.Events;

/// <summary>
/// Handles <see cref="FlowEpicDelegatedEvent"/> by logging the B2B delegation.
/// </summary>
public sealed class FlowEpicDelegatedEventHandler : INotificationHandler<FlowEpicDelegatedEvent>
{
    private readonly ILogger<FlowEpicDelegatedEventHandler> _logger;

    /// <summary>
    /// Initialises a new <see cref="FlowEpicDelegatedEventHandler"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public FlowEpicDelegatedEventHandler(ILogger<FlowEpicDelegatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(FlowEpicDelegatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FlowEpic {FlowEpicId} delegated to guest tenant {GuestTenantId} at {DelegatedOn}.",
            notification.FlowEpicId,
            notification.GuestTenantId,
            notification.OccurredOn);

        return Task.CompletedTask;
    }
}
