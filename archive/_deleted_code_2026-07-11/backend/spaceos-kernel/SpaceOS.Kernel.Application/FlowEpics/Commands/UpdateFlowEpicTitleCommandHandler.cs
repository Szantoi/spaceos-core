using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Handles <see cref="UpdateFlowEpicTitleCommand"/>: loads the epic, updates its title,
/// persists the change, and dispatches any raised domain events.
/// </summary>
public class UpdateFlowEpicTitleCommandHandler : IRequestHandler<UpdateFlowEpicTitleCommand, Result>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UpdateFlowEpicTitleCommandHandler(
        IFlowEpicRepository flowEpicRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _flowEpicRepository = flowEpicRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<Result> Handle(UpdateFlowEpicTitleCommand request, CancellationToken ct)
    {
        var flowEpicId = FlowEpicId.From(request.FlowEpicId);
        var flowEpic = await _flowEpicRepository.GetByIdAsync(flowEpicId, ct).ConfigureAwait(false);

        if (flowEpic is null)
        {
            return Result.NotFound($"FlowEpic not found: {request.FlowEpicId}");
        }

        flowEpic.UpdateTitle(request.NewTitle);

        await _flowEpicRepository.UpdateAsync(flowEpic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = flowEpic.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
