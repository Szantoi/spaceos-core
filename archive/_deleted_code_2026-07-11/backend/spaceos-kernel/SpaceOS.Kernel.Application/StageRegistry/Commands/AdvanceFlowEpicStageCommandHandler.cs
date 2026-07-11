// SpaceOS.Kernel.Application/StageRegistry/Commands/AdvanceFlowEpicStageCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>
/// Handles <see cref="AdvanceFlowEpicStageCommand"/>: validates the advance via
/// <see cref="IStageChainValidator"/> (BE-01 / SEC-03), advances the epic stage,
/// and dispatches domain events.
/// </summary>
internal sealed class AdvanceFlowEpicStageCommandHandler : IRequestHandler<AdvanceFlowEpicStageCommand, Result>
{
    private readonly IFlowEpicRepository _epicRepository;
    private readonly IStageChainTemplateRepository _chainRepository;
    private readonly IStageChainValidator _validator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="AdvanceFlowEpicStageCommandHandler"/>.</summary>
    public AdvanceFlowEpicStageCommandHandler(
        IFlowEpicRepository epicRepository,
        IStageChainTemplateRepository chainRepository,
        IStageChainValidator validator,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(epicRepository);
        ArgumentNullException.ThrowIfNull(chainRepository);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _epicRepository  = epicRepository;
        _chainRepository = chainRepository;
        _validator       = validator;
        _unitOfWork      = unitOfWork;
        _dispatcher      = dispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(AdvanceFlowEpicStageCommand request, CancellationToken ct)
    {
        var epic = await _epicRepository
            .GetByIdAsync(FlowEpicId.From(request.FlowEpicId), ct)
            .ConfigureAwait(false);
        if (epic is null)
            return Result.NotFound($"FlowEpic {request.FlowEpicId} not found.");

        if (!epic.StageChainTemplateId.HasValue)
            return Result.Error("No stage chain is assigned to this FlowEpic.");

        var chain = await _chainRepository
            .GetByIdWithStepsAsync(epic.StageChainTemplateId.Value, ct)
            .ConfigureAwait(false);
        if (chain is null)
            return Result.NotFound($"StageChainTemplate {epic.StageChainTemplateId} not found.");

        _validator.ValidateAdvance(epic, request.TargetStageCode, chain.Steps);
        epic.AdvanceToStage(request.TargetStageCode);

        await _epicRepository.UpdateAsync(epic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = epic.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
