// SpaceOS.Kernel.Application/Facilities/Commands/ArchiveFacilityCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>Handles <see cref="ArchiveFacilityCommand"/>: soft-deletes a <see cref="Domain.Entities.Facility"/> by setting <c>IsArchived = true</c>.</summary>
internal sealed class ArchiveFacilityCommandHandler : IRequestHandler<ArchiveFacilityCommand, Result>
{
    private readonly IFacilityRepository _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="ArchiveFacilityCommandHandler"/>.</summary>
    /// <param name="facilityRepository">Repository for facility persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public ArchiveFacilityCommandHandler(
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

    /// <summary>Executes the archive command.</summary>
    public async Task<Result> Handle(ArchiveFacilityCommand request, CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.Id);
        var facility = await _facilityRepository.GetByIdAsync(facilityId, ct).ConfigureAwait(false);

        if (facility is null)
            return Result.NotFound($"Facility not found: {request.Id}");

        try
        {
            facility.Archive();
        }
        catch (DomainException ex)
        {
            return Result.Conflict(ex.Message);
        }

        await _facilityRepository.UpdateAsync(facility, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = facility.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.NoContent();
    }
}
