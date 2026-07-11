// SpaceOS.Kernel.Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Sync.Commands.ReceiveSignal;

/// <summary>
/// Handles <see cref="ReceiveSyncSignalCommand"/> with BE-02 transaction boundary and BE-03 idempotency.
/// <list type="number">
///   <item>Checks idempotency via <c>GetByClientSignalIdAsync</c>; returns success immediately on duplicate.</item>
///   <item>Acquires <see cref="ISyncSignalWriteLock"/> to serialise chain appends.</item>
///   <item>Begins a database transaction via <see cref="ITransactionManager"/>.</item>
///   <item>Reads the tail hash, computes the new HMAC-SHA256 hash, creates and persists the signal.</item>
///   <item>Dispatches domain events after the transaction commits successfully.</item>
/// </list>
/// </summary>
internal sealed class ReceiveSyncSignalCommandHandler : IRequestHandler<ReceiveSyncSignalCommand, Result>
{
    private readonly ISyncSignalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISyncSignalWriteLock _writeLock;
    private readonly ITransactionManager _transactionManager;
    private readonly ISyncSignalHasher _hasher;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="ReceiveSyncSignalCommandHandler"/>.</summary>
    public ReceiveSyncSignalCommandHandler(
        ISyncSignalRepository repository,
        IUnitOfWork unitOfWork,
        ISyncSignalWriteLock writeLock,
        ITransactionManager transactionManager,
        ISyncSignalHasher hasher,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(writeLock);
        ArgumentNullException.ThrowIfNull(transactionManager);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _writeLock = writeLock;
        _transactionManager = transactionManager;
        _hasher = hasher;
        _dispatcher = dispatcher;
    }

    /// <summary>Handles the receipt and hash-chain append of a sync signal.</summary>
    /// <param name="command">The receive-sync-signal command.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns><see cref="Result.Success"/> on success or idempotent duplicate.</returns>
    public async Task<Result> Handle(ReceiveSyncSignalCommand command, CancellationToken ct)
    {
        var tenantId = TenantId.From(command.TenantId);

        // BE-03: Idempotency — return success immediately if already processed.
        var existing = await _repository
            .GetByClientSignalIdAsync(tenantId, command.ClientSignalId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return Result.Success();

        // BE-02: Acquire write-lock to serialise hash-chain appends for this tenant.
        await using var lockHandle = await _writeLock
            .AcquireAsync(command.TenantId, ct)
            .ConfigureAwait(false);

        // Begin transaction to make GetLastHash + Insert atomic.
        await using var txHandle = await _transactionManager
            .BeginTransactionAsync(ct)
            .ConfigureAwait(false);

        var previousHash = await _repository
            .GetLastHashAsync(tenantId, ct)
            .ConfigureAwait(false);

        var occurredAt = DateTimeOffset.UtcNow;
        var stateHash = _hasher.ComputeHash(previousHash, command.PayloadJson, occurredAt);

        var epicId = FlowEpicId.From(command.EpicId);
        var signal = SyncSignal.Create(
            epicId,
            tenantId,
            command.NewState,
            stateHash,
            previousHash,
            command.ClientSignalId);

        await _repository.AddAsync(signal, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        await _transactionManager.CommitAsync(ct).ConfigureAwait(false);

        // Dispatch domain events only after the transaction has committed successfully.
        var domainEvents = signal.PopDomainEvents();
        await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
