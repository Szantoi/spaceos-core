// SpaceOS.Kernel.Application/StageRegistry/Commands/AssignChainCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="AssignChainCommand"/>: assigns a chain template to a flow epic.</summary>
internal sealed class AssignChainCommandHandler : IRequestHandler<AssignChainCommand, Result>
{
    private readonly IFlowEpicRepository _epicRepository;
    private readonly IStageChainTemplateRepository _chainRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="AssignChainCommandHandler"/>.</summary>
    public AssignChainCommandHandler(
        IFlowEpicRepository epicRepository,
        IStageChainTemplateRepository chainRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(epicRepository);
        ArgumentNullException.ThrowIfNull(chainRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _epicRepository  = epicRepository;
        _chainRepository = chainRepository;
        _unitOfWork      = unitOfWork;
        _dispatcher      = dispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(AssignChainCommand request, CancellationToken ct)
    {
        var epic = await _epicRepository
            .GetByIdAsync(FlowEpicId.From(request.FlowEpicId), ct)
            .ConfigureAwait(false);
        if (epic is null)
            return Result.NotFound($"FlowEpic {request.FlowEpicId} not found.");

        var chain = await _chainRepository.GetByIdAsync(request.ChainTemplateId, ct).ConfigureAwait(false);
        if (chain is null)
            return Result.NotFound($"StageChainTemplate {request.ChainTemplateId} not found.");

        epic.AssignChain(request.ChainTemplateId, request.FirstStageCode);

        await _epicRepository.UpdateAsync(epic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = epic.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
