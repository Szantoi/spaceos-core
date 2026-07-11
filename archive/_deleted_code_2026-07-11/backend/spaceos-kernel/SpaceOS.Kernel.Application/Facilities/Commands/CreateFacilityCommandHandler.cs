using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// Handles <see cref="CreateFacilityCommand"/>: validates tenant existence and name uniqueness,
/// creates the <see cref="Facility"/> aggregate, persists it, and returns the new facility ID.
/// </summary>
public class CreateFacilityCommandHandler : IRequestHandler<CreateFacilityCommand, Result<Guid>>
{
    private readonly IFacilityRepository _facilityRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="CreateFacilityCommandHandler"/>.
    /// </summary>
    /// <param name="facilityRepository">Repository for facility persistence.</param>
    /// <param name="tenantRepository">Repository for tenant lookups.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public CreateFacilityCommandHandler(
        IFacilityRepository facilityRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(facilityRepository);
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _facilityRepository = facilityRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(CreateFacilityCommand request, CancellationToken ct)
    {
        var tenantId = TenantId.From(request.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            return Result.NotFound($"Tenant not found: {request.TenantId}");
        }

        var exists = await _facilityRepository.ExistsByNameAsync(tenantId, request.Name, ct).ConfigureAwait(false);
        if (exists)
        {
            return Result.Error("Facility name must be unique within a tenant.");
        }

        var facility = Facility.Create(request.Name, tenantId);

        await _facilityRepository.AddAsync(facility, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = facility.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(facility.Id.Value);
    }
}
