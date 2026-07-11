// SpaceOS.Kernel.Application/Nodes/Commands/Heartbeat/HeartbeatCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Nodes.Commands.Heartbeat;

/// <summary>Handles <see cref="HeartbeatCommand"/> — finds the manifest, records the heartbeat, and saves.</summary>
internal sealed class HeartbeatCommandHandler : IRequestHandler<HeartbeatCommand, Result>
{
    private readonly INodeManifestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="HeartbeatCommandHandler"/>.</summary>
    public HeartbeatCommandHandler(
        INodeManifestRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    /// <summary>Handles the heartbeat for a registered node.</summary>
    /// <param name="command">The heartbeat command.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Result.Success"/> when the heartbeat is recorded,
    /// or <see cref="Result.NotFound"/> when no manifest exists for the tenant.
    /// </returns>
    public async Task<Result> Handle(HeartbeatCommand command, CancellationToken ct)
    {
        var tenantId = TenantId.From(command.TenantId);

        var manifest = await _repository.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);
        if (manifest is null)
            return Result.NotFound();

        // Node is considered to have come back online when it was silent for more than 2 minutes.
        var isOnlineChanged = manifest.LastHeartbeatAt is null
            || (DateTimeOffset.UtcNow - manifest.LastHeartbeatAt.Value).TotalMinutes > 2;

        manifest.RecordHeartbeat(isOnlineChanged);

        await _repository.UpdateAsync(manifest, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = manifest.PopDomainEvents();
        await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
