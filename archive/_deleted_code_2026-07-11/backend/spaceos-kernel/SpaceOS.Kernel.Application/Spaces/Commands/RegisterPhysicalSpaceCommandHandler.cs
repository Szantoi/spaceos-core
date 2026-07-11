// SpaceOS.Kernel.Application/Spaces/Commands/RegisterPhysicalSpaceCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Handles <see cref="RegisterPhysicalSpaceCommand"/>: validates facility existence,
/// creates the <see cref="PhysicalSpace"/> aggregate, persists it, and dispatches domain events.
/// </summary>
internal sealed class RegisterPhysicalSpaceCommandHandler
    : IRequestHandler<RegisterPhysicalSpaceCommand, Result<Guid>>
{
    private readonly IPhysicalSpaceRepository _spaceRepo;
    private readonly IFacilityRepository      _facilityRepo;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly IDomainEventDispatcher   _dispatcher;
    private readonly ITenantResolver          _tenantResolver;

    /// <summary>Initialises a new <see cref="RegisterPhysicalSpaceCommandHandler"/>.</summary>
    /// <param name="spaceRepo">Repository for physical space persistence.</param>
    /// <param name="facilityRepo">Repository for facility lookups.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="dispatcher">Dispatcher for publishing domain events after persistence.</param>
    /// <param name="tenantResolver">Resolves the current tenant from the ambient context.</param>
    public RegisterPhysicalSpaceCommandHandler(
        IPhysicalSpaceRepository spaceRepo,
        IFacilityRepository facilityRepo,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(spaceRepo);
        ArgumentNullException.ThrowIfNull(facilityRepo);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _spaceRepo      = spaceRepo;
        _facilityRepo   = facilityRepo;
        _unitOfWork     = unitOfWork;
        _dispatcher     = dispatcher;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(RegisterPhysicalSpaceCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var facilityId = FacilityId.From(cmd.FacilityId);
        var facility = await _facilityRepo.GetByIdAsync(facilityId, ct).ConfigureAwait(false);

        if (facility is null)
            return Result.NotFound($"Facility not found: {cmd.FacilityId}");

        var space = PhysicalSpace.Register(
            tenantId.Value.Value,
            facilityId,
            new DimensionVector(cmd.WidthMm, cmd.HeightMm, cmd.DepthMm),
            new Point3D(cmd.OriginX, cmd.OriginY, cmd.OriginZ),
            cmd.SpaceType,
            cmd.CellSizeMm);

        await _spaceRepo.AddAsync(space, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = space.PopDomainEvents();
        await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(space.Id);
    }
}
