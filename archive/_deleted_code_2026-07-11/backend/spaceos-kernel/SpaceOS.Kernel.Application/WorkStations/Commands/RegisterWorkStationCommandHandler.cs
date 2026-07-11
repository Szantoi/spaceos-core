using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Handles <see cref="RegisterWorkStationCommand"/>: validates facility existence, creates the
/// <see cref="WorkStation"/> aggregate, persists it, and returns the new workstation ID.
/// </summary>
public class RegisterWorkStationCommandHandler : IRequestHandler<RegisterWorkStationCommand, Result<Guid>>
{
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="RegisterWorkStationCommandHandler"/>.
    /// </summary>
    /// <param name="workStationRepository">Repository for workstation persistence.</param>
    /// <param name="facilityRepository">Repository for facility lookups.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public RegisterWorkStationCommandHandler(
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

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(RegisterWorkStationCommand request, CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);
        var tenantId   = TenantId.From(request.TenantId);

        var facility = await _facilityRepository.GetByIdAsync(facilityId, ct).ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound($"Facility not found: {request.FacilityId}");
        }

        var nameExists = await _workStationRepository.ExistsByNameAsync(facilityId, request.Name, ct).ConfigureAwait(false);
        if (nameExists)
            return Result.Error("A WorkStation with that name already exists in this facility.");

        var workStation = WorkStation.Create(
            request.Name,
            request.Type,
            facilityId,
            tenantId);

        await _workStationRepository.AddAsync(workStation, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = workStation.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(workStation.Id.Value);
    }
}
