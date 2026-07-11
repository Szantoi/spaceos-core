// SpaceOS.Kernel.Application/StageRegistry/Commands/RemoveStageChainStepCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="RemoveStageChainStepCommand"/>: removes a step from a chain template.</summary>
internal sealed class RemoveStageChainStepCommandHandler : IRequestHandler<RemoveStageChainStepCommand, Result>
{
    private readonly IStageChainTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="RemoveStageChainStepCommandHandler"/>.</summary>
    public RemoveStageChainStepCommandHandler(
        IStageChainTemplateRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(RemoveStageChainStepCommand request, CancellationToken ct)
    {
        var chain = await _repository.GetByIdWithStepsAsync(request.ChainTemplateId, ct).ConfigureAwait(false);
        if (chain is null)
            return Result.NotFound($"StageChainTemplate {request.ChainTemplateId} not found.");

        chain.RemoveStep(request.StageCode);

        await _repository.UpdateAsync(chain, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = chain.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
