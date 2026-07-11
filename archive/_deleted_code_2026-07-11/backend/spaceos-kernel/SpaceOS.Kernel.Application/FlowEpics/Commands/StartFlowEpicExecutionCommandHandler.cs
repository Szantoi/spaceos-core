using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Handles <see cref="StartFlowEpicExecutionCommand"/>: loads the epic, advances it to the Delivery phase,
/// and persists the updated state.
/// </summary>
public class StartFlowEpicExecutionCommandHandler : IRequestHandler<StartFlowEpicExecutionCommand, Result>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="StartFlowEpicExecutionCommandHandler"/>.
    /// </summary>
    /// <param name="flowEpicRepository">Repository for flow epic persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public StartFlowEpicExecutionCommandHandler(
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

    /// <inheritdoc/>
    public async Task<Result> Handle(StartFlowEpicExecutionCommand request, CancellationToken ct)
    {
        var epicId = FlowEpicId.From(request.FlowEpicId);
        var epic = await _flowEpicRepository.GetByIdAsync(epicId, ct).ConfigureAwait(false);

        if (epic is null)
        {
            return Result.NotFound($"FlowEpic with ID {request.FlowEpicId} was not found.");
        }

        try
        {
            epic.StartExecution();
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
