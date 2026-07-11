using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Tenants.Events;

/// <summary>
/// Handles <see cref="TenantCreatedEvent"/>: logs the tenant creation.
/// </summary>
public sealed class TenantCreatedEventHandler : INotificationHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant {TenantId} created at {OccurredOn}.",
            notification.TenantId, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
