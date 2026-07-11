// SpaceOS.Kernel.Application/StageRegistry/Commands/SkipOptionalStageCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="SkipOptionalStageCommand"/>: records an optional stage skip and dispatches events.</summary>
internal sealed class SkipOptionalStageCommandHandler : IRequestHandler<SkipOptionalStageCommand, Result>
{
    private readonly IFlowEpicRepository _epicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="SkipOptionalStageCommandHandler"/>.</summary>
    public SkipOptionalStageCommandHandler(
        IFlowEpicRepository epicRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(epicRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _epicRepository = epicRepository;
        _unitOfWork     = unitOfWork;
        _dispatcher     = dispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(SkipOptionalStageCommand request, CancellationToken ct)
    {
        var epic = await _epicRepository
            .GetByIdAsync(FlowEpicId.From(request.FlowEpicId), ct)
            .ConfigureAwait(false);
        if (epic is null)
            return Result.NotFound($"FlowEpic {request.FlowEpicId} not found.");

        epic.SkipOptionalStage(request.StageCode);

        await _epicRepository.UpdateAsync(epic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = epic.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
