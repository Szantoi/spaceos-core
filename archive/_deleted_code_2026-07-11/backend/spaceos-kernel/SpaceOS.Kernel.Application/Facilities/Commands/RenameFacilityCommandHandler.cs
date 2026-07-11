using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// Handles <see cref="RenameFacilityCommand"/>: loads the facility, renames it,
/// persists the change, and dispatches any raised domain events.
/// </summary>
public class RenameFacilityCommandHandler : IRequestHandler<RenameFacilityCommand, Result>
{
    private readonly IFacilityRepository _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public RenameFacilityCommandHandler(
        IFacilityRepository facilityRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(facilityRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<Result> Handle(RenameFacilityCommand request, CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);
        var facility = await _facilityRepository.GetByIdAsync(facilityId, ct).ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound($"Facility not found: {request.FacilityId}");
        }

        facility.Rename(request.NewName);

        await _facilityRepository.UpdateAsync(facility, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = facility.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
