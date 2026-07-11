using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// Handles <see cref="RegisterSpaceLayerCommand"/>: validates the trade type, creates the
/// appropriate <see cref="SpaceOS.Kernel.Domain.Entities.SpaceLayer"/> via its factory methods,
/// persists it, and returns the new layer's <see cref="Guid"/> identifier.
/// </summary>
public class RegisterSpaceLayerCommandHandler : IRequestHandler<RegisterSpaceLayerCommand, Result<Guid>>
{
    private readonly ISpaceLayerRepository _spaceLayerRepository;
    private readonly IFacilityRepository   _facilityRepository;
    private readonly IUnitOfWork           _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="RegisterSpaceLayerCommandHandler"/>.</summary>
    /// <param name="spaceLayerRepository">Repository for SpaceLayer persistence.</param>
    /// <param name="facilityRepository">Repository for facility lookups.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public RegisterSpaceLayerCommandHandler(
        ISpaceLayerRepository  spaceLayerRepository,
        IFacilityRepository    facilityRepository,
        IUnitOfWork            unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(spaceLayerRepository);
        ArgumentNullException.ThrowIfNull(facilityRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _spaceLayerRepository  = spaceLayerRepository;
        _facilityRepository    = facilityRepository;
        _unitOfWork            = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(
        RegisterSpaceLayerCommand request,
        CancellationToken         ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);
        var tenantId   = TenantId.From(request.TenantId);

        var facility = await _facilityRepository
            .GetByIdAsync(facilityId, ct)
            .ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound($"Facility not found: {request.FacilityId}");
        }

        // Non-null guaranteed by RegisterSpaceLayerCommandValidator When() branches.
        var layer = request.IsExternalNode
            ? SpaceLayer.CreateExternalLayer(request.ExternalSourceUrl!, facilityId, request.TradeType, tenantId)
            : SpaceLayer.CreateLocalLayer(request.IntentDataJson!,         facilityId, request.TradeType, tenantId);

        await _spaceLayerRepository.AddAsync(layer, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = layer.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(layer.Id.Value);
    }
}
