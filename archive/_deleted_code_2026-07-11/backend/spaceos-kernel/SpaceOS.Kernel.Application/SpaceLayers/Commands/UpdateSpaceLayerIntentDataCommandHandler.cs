using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// Handles <see cref="UpdateSpaceLayerIntentDataCommand"/>: loads the SpaceLayer, updates its
/// intent data, persists the change, and dispatches the raised domain event.
/// </summary>
public class UpdateSpaceLayerIntentDataCommandHandler : IRequestHandler<UpdateSpaceLayerIntentDataCommand, Result>
{
    private readonly ISpaceLayerRepository  _spaceLayerRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="UpdateSpaceLayerIntentDataCommandHandler"/>.
    /// </summary>
    /// <param name="spaceLayerRepository">Repository for SpaceLayer persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public UpdateSpaceLayerIntentDataCommandHandler(
        ISpaceLayerRepository  spaceLayerRepository,
        IUnitOfWork            unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(spaceLayerRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _spaceLayerRepository  = spaceLayerRepository;
        _unitOfWork            = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(UpdateSpaceLayerIntentDataCommand request, CancellationToken ct)
    {
        var id = SpaceLayerId.From(request.SpaceLayerId);
        var layer = await _spaceLayerRepository.GetByIdAsync(id, ct).ConfigureAwait(false);

        if (layer is null)
            return Result.NotFound($"SpaceLayer not found: {request.SpaceLayerId}");

        if (layer.IsExternalNode)
            return Result.Error("Cannot update intent data on a federated (external) SpaceLayer.");

        layer.UpdateIntentData(request.IntentDataJson);
        await _spaceLayerRepository.UpdateAsync(layer, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = layer.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
