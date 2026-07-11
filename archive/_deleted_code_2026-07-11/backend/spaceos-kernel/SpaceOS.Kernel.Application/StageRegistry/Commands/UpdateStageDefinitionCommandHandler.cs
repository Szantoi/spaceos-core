// SpaceOS.Kernel.Application/StageRegistry/Commands/UpdateStageDefinitionCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="UpdateStageDefinitionCommand"/>: updates the module endpoint and dispatches events.</summary>
internal sealed class UpdateStageDefinitionCommandHandler : IRequestHandler<UpdateStageDefinitionCommand, Result>
{
    private readonly IStageDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="UpdateStageDefinitionCommandHandler"/>.</summary>
    public UpdateStageDefinitionCommandHandler(
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
    public async Task<Result> Handle(UpdateStageDefinitionCommand request, CancellationToken ct)
    {
        var definition = await _repository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (definition is null)
            return Result.NotFound($"StageDefinition {request.Id} not found.");

        definition.UpdateEndpoint(request.ModuleEndpoint);

        await _repository.UpdateAsync(definition, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = definition.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
