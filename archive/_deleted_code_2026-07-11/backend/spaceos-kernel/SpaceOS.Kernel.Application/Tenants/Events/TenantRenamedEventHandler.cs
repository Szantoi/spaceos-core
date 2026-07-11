using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Application.Tenants.Events;

/// <summary>
/// Handles <see cref="TenantRenamedEvent"/>: logs the tenant rename.
/// </summary>
public sealed class TenantRenamedEventHandler : INotificationHandler<TenantRenamedEvent>
{
    private readonly ILogger<TenantRenamedEventHandler> _logger;

    public TenantRenamedEventHandler(ILogger<TenantRenamedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Handle(TenantRenamedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant {TenantId} renamed from '{OldName}' to '{NewName}' at {OccurredOn}.",
            notification.TenantId, notification.OldName, notification.NewName, notification.OccurredOn);
        return Task.CompletedTask;
    }
}
