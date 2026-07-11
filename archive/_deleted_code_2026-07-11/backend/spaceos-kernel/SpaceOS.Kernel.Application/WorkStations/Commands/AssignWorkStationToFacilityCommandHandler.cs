using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Handles <see cref="AssignWorkStationToFacilityCommand"/>: loads the workstation and target facility,
/// reassigns the workstation, persists the change, and dispatches any raised domain events.
/// </summary>
public class AssignWorkStationToFacilityCommandHandler : IRequestHandler<AssignWorkStationToFacilityCommand, Result>
{
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public AssignWorkStationToFacilityCommandHandler(
        IWorkStationRepository workStationRepository,
        IFacilityRepository facilityRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(workStationRepository);
        ArgumentNullException.ThrowIfNull(facilityRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _workStationRepository = workStationRepository;
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<Result> Handle(AssignWorkStationToFacilityCommand request, CancellationToken ct)
    {
        var workStationId = WorkStationId.From(request.WorkStationId);
        var workStation = await _workStationRepository.GetByIdAsync(workStationId, ct).ConfigureAwait(false);

        if (workStation is null)
        {
            return Result.NotFound($"WorkStation not found: {request.WorkStationId}");
        }

        var newFacilityId = FacilityId.From(request.NewFacilityId);
        var facility = await _facilityRepository.GetByIdAsync(newFacilityId, ct).ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound($"Facility not found: {request.NewFacilityId}");
        }

        workStation.AssignToFacility(newFacilityId);

        await _workStationRepository.UpdateAsync(workStation, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = workStation.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
