using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Handles <see cref="CreateFlowEpicCommand"/>: creates a new <see cref="FlowEpic"/> aggregate,
/// persists it, and returns its identifier.
/// </summary>
public class CreateFlowEpicCommandHandler : IRequestHandler<CreateFlowEpicCommand, Result<Guid>>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="CreateFlowEpicCommandHandler"/>.
    /// </summary>
    /// <param name="flowEpicRepository">Repository for flow epic persistence.</param>
    /// <param name="facilityRepository">Repository for facility existence checks.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public CreateFlowEpicCommandHandler(
        IFlowEpicRepository flowEpicRepository,
        IFacilityRepository facilityRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        ArgumentNullException.ThrowIfNull(facilityRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _flowEpicRepository = flowEpicRepository;
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(CreateFlowEpicCommand request, CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.TargetFacilityId);
        var tenantId   = TenantId.From(request.TenantId);

        var facility = await _facilityRepository
            .GetByIdAsync(facilityId, ct)
            .ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound($"Facility not found: {request.TargetFacilityId}");
        }

        var epic = FlowEpic.Create(request.Title, facilityId, tenantId);

        await _flowEpicRepository.AddAsync(epic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = epic.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(epic.Id.Value);
    }
}
