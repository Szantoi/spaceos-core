// SpaceOS.Kernel.Application/Spaces/Commands/RegisterSpatialElementCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Spaces.Services;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Handles <see cref="RegisterSpatialElementCommand"/>: validates the physical space exists
/// and belongs to the current tenant, then delegates element insertion to
/// <see cref="IBvhTreeService.InsertElementAsync"/>.
/// </summary>
internal sealed class RegisterSpatialElementCommandHandler
    : IRequestHandler<RegisterSpatialElementCommand, Result<Guid>>
{
    private readonly IBvhTreeService          _bvhTreeService;
    private readonly IPhysicalSpaceRepository _spaceRepo;
    private readonly ITenantResolver          _tenantResolver;

    /// <summary>Initialises a new <see cref="RegisterSpatialElementCommandHandler"/>.</summary>
    /// <param name="bvhTreeService">The internal BVH tree management service.</param>
    /// <param name="spaceRepo">Repository for physical space lookups.</param>
    /// <param name="tenantResolver">Resolves the current tenant from the ambient context.</param>
    public RegisterSpatialElementCommandHandler(
        IBvhTreeService bvhTreeService,
        IPhysicalSpaceRepository spaceRepo,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(bvhTreeService);
        ArgumentNullException.ThrowIfNull(spaceRepo);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _bvhTreeService = bvhTreeService;
        _spaceRepo      = spaceRepo;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(RegisterSpatialElementCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var space = await _spaceRepo.GetByIdAsync(cmd.PhysicalSpaceId, ct).ConfigureAwait(false);
        if (space is null)
            return Result.NotFound($"PhysicalSpace not found: {cmd.PhysicalSpaceId}");

        if (space.TenantId != tenantId.Value.Value)
            return Result.Forbidden();

        var elementBox = new BoundingBox(cmd.MinX, cmd.MinY, cmd.MinZ, cmd.MaxX, cmd.MaxY, cmd.MaxZ);

        return await _bvhTreeService.InsertElementAsync(
            cmd.PhysicalSpaceId, elementBox,
            cmd.FlowEpicId, cmd.TradeType, cmd.ElementType, ct).ConfigureAwait(false);
    }
}
