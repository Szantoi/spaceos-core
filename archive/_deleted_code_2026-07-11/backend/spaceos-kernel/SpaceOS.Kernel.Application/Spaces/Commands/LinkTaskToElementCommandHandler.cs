// SpaceOS.Kernel.Application/Spaces/Commands/LinkTaskToElementCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Handles <see cref="LinkTaskToElementCommand"/>: loads the FlowTask tenant and SpatialElement,
/// enforces cross-tenant isolation (SEC-P3A-02), creates the <see cref="SpatialTaskLink"/>,
/// and persists it.
/// </summary>
internal sealed class LinkTaskToElementCommandHandler
    : IRequestHandler<LinkTaskToElementCommand, Result>
{
    private readonly IFlowTaskLookup           _taskLookup;
    private readonly ISpatialElementRepository _elementRepo;
    private readonly ISpatialTaskLinkRepository _linkRepo;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ITenantResolver           _tenantResolver;

    /// <summary>Initialises a new <see cref="LinkTaskToElementCommandHandler"/>.</summary>
    /// <param name="taskLookup">Lookup for FlowTask tenant ownership across the module boundary.</param>
    /// <param name="elementRepo">Repository for SpatialElement lookups.</param>
    /// <param name="linkRepo">Repository for SpatialTaskLink persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="tenantResolver">Resolves the current tenant from the ambient context.</param>
    public LinkTaskToElementCommandHandler(
        IFlowTaskLookup taskLookup,
        ISpatialElementRepository elementRepo,
        ISpatialTaskLinkRepository linkRepo,
        IUnitOfWork unitOfWork,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(taskLookup);
        ArgumentNullException.ThrowIfNull(elementRepo);
        ArgumentNullException.ThrowIfNull(linkRepo);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _taskLookup     = taskLookup;
        _elementRepo    = elementRepo;
        _linkRepo       = linkRepo;
        _unitOfWork     = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(LinkTaskToElementCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var taskTenantId = await _taskLookup.GetTenantIdAsync(cmd.FlowTaskId, ct).ConfigureAwait(false);
        var element = await _elementRepo.GetByIdAsync(cmd.SpatialElementId, ct).ConfigureAwait(false);

        if (taskTenantId is null || element is null)
            return Result.NotFound();

        // SEC-P3A-02: defense-in-depth — DB trigger also guards this invariant.
        if (taskTenantId.Value != element.TenantId)
            return Result.Forbidden("Cross-tenant spatial link rejected.");

        var link = SpatialTaskLink.Create(
            tenantId.Value.Value,
            cmd.FlowTaskId,
            cmd.SpatialElementId,
            cmd.WorkPhase);

        await _linkRepo.AddAsync(link, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
