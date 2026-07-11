using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.DelegateFlowEpic;

/// <summary>
/// Handles <see cref="DelegateFlowEpicCommand"/>: loads the epic, delegates it to the guest tenant,
/// and persists the updated state.
/// </summary>
public class DelegateFlowEpicCommandHandler : IRequestHandler<DelegateFlowEpicCommand, Result>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="DelegateFlowEpicCommandHandler"/>.
    /// </summary>
    /// <param name="flowEpicRepository">Repository for flow epic persistence.</param>
    /// <param name="tenantRepository">Repository used to verify the guest tenant exists.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public DelegateFlowEpicCommandHandler(
        IFlowEpicRepository flowEpicRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _flowEpicRepository = flowEpicRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(DelegateFlowEpicCommand request, CancellationToken ct)
    {
        var epicId = FlowEpicId.From(request.EpicId);
        var epic = await _flowEpicRepository.GetByIdAsync(epicId, ct).ConfigureAwait(false);

        if (epic is null)
        {
            return Result.NotFound($"FlowEpic with ID {request.EpicId} was not found.");
        }

        var guestTenantId = TenantId.From(request.GuestTenantId);
        var guestTenant = await _tenantRepository.GetByIdAsync(guestTenantId, ct).ConfigureAwait(false);

        if (guestTenant is null)
        {
            return Result.NotFound($"Guest tenant not found: {request.GuestTenantId}");
        }

        try
        {
            epic.DelegateTo(guestTenantId);
            await _flowEpicRepository.UpdateAsync(epic, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            var domainEvents = epic.PopDomainEvents();
            await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Error(ex.Message);
        }
    }
}
