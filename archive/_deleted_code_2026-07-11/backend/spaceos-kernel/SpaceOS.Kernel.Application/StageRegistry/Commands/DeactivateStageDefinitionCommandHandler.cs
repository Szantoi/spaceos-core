// SpaceOS.Kernel.Application/StageRegistry/Commands/DeactivateStageDefinitionCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="DeactivateStageDefinitionCommand"/>: deactivates the stage definition and dispatches domain events.</summary>
internal sealed class DeactivateStageDefinitionCommandHandler : IRequestHandler<DeactivateStageDefinitionCommand, Result>
{
    private readonly IStageDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="DeactivateStageDefinitionCommandHandler"/>.</summary>
    public DeactivateStageDefinitionCommandHandler(
        IStageDefinitionRepository repository,
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
    public async Task<Result> Handle(DeactivateStageDefinitionCommand request, CancellationToken ct)
    {
        var definition = await _repository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (definition is null)
            return Result.NotFound($"StageDefinition {request.Id} not found.");

        definition.Deactivate();

        await _repository.UpdateAsync(definition, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = definition.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
