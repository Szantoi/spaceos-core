// SpaceOS.Kernel.Application/FlowEpics/Events/FlowEpicClosedEventHandler.cs

using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Outbox;

namespace SpaceOS.Kernel.Application.FlowEpics.Events;

/// <summary>
/// MediatR notification handler for <see cref="FlowEpicClosedEvent"/>.
/// Queues an outbox message so that downstream Escrow integration can be triggered
/// reliably, even if the external service is temporarily unavailable.
/// The outbox entry is written in the same scope — the calling handler is responsible
/// for committing the unit of work that persists both the epic state and this entry.
/// </summary>
internal sealed class FlowEpicClosedEventHandler : INotificationHandler<FlowEpicClosedEvent>
{
    private const string EventTypeName = "FlowEpicClosedDone";

    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<FlowEpicClosedEventHandler> _logger;

    /// <summary>Initialises a new <see cref="FlowEpicClosedEventHandler"/>.</summary>
    /// <param name="outboxRepository">Repository for appending outbox messages.</param>
    /// <param name="logger">Structured logger.</param>
    public FlowEpicClosedEventHandler(
        IOutboxRepository outboxRepository,
        ILogger<FlowEpicClosedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(logger);
        _outboxRepository = outboxRepository;
        _logger           = logger;
    }

    /// <inheritdoc/>
    public async Task Handle(FlowEpicClosedEvent notification, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            EpicId    = notification.FlowEpicId.Value,
            TenantId  = notification.TenantId.Value,
            ProofHash = notification.ProofHash,
            OccurredOn = notification.OccurredOn
        });

        var message = OutboxMessage.Create(EventTypeName, payload, notification.TenantId.Value);
        await _outboxRepository.AddAsync(message, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Queued outbox message {MessageType} for FlowEpic {EpicId}, tenant {TenantId}.",
            EventTypeName, notification.FlowEpicId.Value, notification.TenantId.Value);
    }
}
