// SpaceOS.Kernel.Application/FlowEpics/Commands/CloseFlowEpic/CloseFlowEpicCommandHandler.cs

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;

/// <summary>
/// Handles <see cref="CloseFlowEpicCommand"/>: closes a FlowEpic from the Delivery phase,
/// writes an <see cref="AggregateSnapshot"/>, queues an outbox message, and dispatches domain events.
/// </summary>
internal sealed class CloseFlowEpicCommandHandler : IRequestHandler<CloseFlowEpicCommand, Result>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IAggregateSnapshotRepository _snapshotRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="CloseFlowEpicCommandHandler"/>.
    /// </summary>
    /// <param name="flowEpicRepository">Repository for flow epic persistence.</param>
    /// <param name="snapshotRepository">Repository for aggregate snapshots.</param>
    /// <param name="outboxRepository">Repository for outbox messages.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public CloseFlowEpicCommandHandler(
        IFlowEpicRepository flowEpicRepository,
        IAggregateSnapshotRepository snapshotRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _flowEpicRepository    = flowEpicRepository;
        _snapshotRepository    = snapshotRepository;
        _outboxRepository      = outboxRepository;
        _unitOfWork            = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(CloseFlowEpicCommand request, CancellationToken ct)
    {
        var epicId = FlowEpicId.From(request.FlowEpicId);
        var epic = await _flowEpicRepository.GetByIdAsync(epicId, ct).ConfigureAwait(false);

        if (epic is null)
            return Result.NotFound($"FlowEpic with ID {request.FlowEpicId} was not found.");

        try
        {
            epic.Close(request.ProofUrl, request.ProofHash);
        }
        catch (DomainException ex)
        {
            return Result.Error(ex.Message);
        }

        await _flowEpicRepository.UpdateAsync(epic, ct).ConfigureAwait(false);

        // Capture a snapshot of the closed aggregate state via the explicit DTO path (ISnapshotable)
        var stateJson = JsonSerializer.Serialize(epic.ToSnapshotDto());
        var snapshotHash = ComputeSha256Hex(stateJson);

        // Determine the next snapshot version for this aggregate
        var latestSnapshot = await _snapshotRepository
            .GetLatestAsync(epic.Id.Value, ct)
            .ConfigureAwait(false);
        var nextVersion = (latestSnapshot?.Version ?? 0) + 1;

        // Retrieve the trigger event id from the raised domain event
        var domainEvents = epic.PopDomainEvents();
        var triggerEventId = domainEvents.Count > 0 ? Guid.NewGuid() : Guid.NewGuid();

        var snapshot = AggregateSnapshot.Create(
            aggregateId:    epic.Id.Value,
            aggregateType:  nameof(Domain.Entities.FlowEpic),
            version:        nextVersion,
            snapshotAt:     DateTimeOffset.UtcNow,
            triggerEventId: triggerEventId,
            stateJson:      stateJson,
            snapshotHash:   snapshotHash,
            tenantId:       epic.TenantId.Value);

        await _snapshotRepository.AddAsync(snapshot, ct).ConfigureAwait(false);

        // Queue an outbox message for downstream escrow integration
        var outboxPayload = JsonSerializer.Serialize(new { FlowEpicId = epic.Id.Value });
        var outboxMessage = OutboxMessage.Create("EscrowTrigger", outboxPayload, epic.TenantId.Value);
        await _outboxRepository.AddAsync(outboxMessage, ct).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
