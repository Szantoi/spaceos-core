// SpaceOS.Kernel.Application/StageRegistry/Commands/AddStageChainStepCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="AddStageChainStepCommand"/>: adds a step to an existing chain template.</summary>
internal sealed class AddStageChainStepCommandHandler : IRequestHandler<AddStageChainStepCommand, Result>
{
    private readonly IStageChainTemplateRepository _chainRepository;
    private readonly IStageDefinitionRepository _definitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="AddStageChainStepCommandHandler"/>.</summary>
    public AddStageChainStepCommandHandler(
        IStageChainTemplateRepository chainRepository,
        IStageDefinitionRepository definitionRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(chainRepository);
        ArgumentNullException.ThrowIfNull(definitionRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _chainRepository      = chainRepository;
        _definitionRepository = definitionRepository;
        _unitOfWork           = unitOfWork;
        _dispatcher           = dispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(AddStageChainStepCommand request, CancellationToken ct)
    {
        var chain = await _chainRepository.GetByIdWithStepsAsync(request.ChainTemplateId, ct).ConfigureAwait(false);
        if (chain is null)
            return Result.NotFound($"StageChainTemplate {request.ChainTemplateId} not found.");

        var stageDef = await _definitionRepository.GetByIdAsync(request.StageDefinitionId, ct).ConfigureAwait(false);
        if (stageDef is null)
            return Result.NotFound($"StageDefinition {request.StageDefinitionId} not found.");

        chain.AddStep(stageDef, request.SortOrder, request.IsOptional);

        await _chainRepository.UpdateAsync(chain, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = chain.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
