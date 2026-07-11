// SpaceOS.Kernel.Application/StageRegistry/Commands/RegisterStageDefinitionCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>
/// Handles <see cref="RegisterStageDefinitionCommand"/>: creates and persists a new
/// <see cref="StageDefinition"/> aggregate, then dispatches domain events.
/// </summary>
internal sealed class RegisterStageDefinitionCommandHandler
    : IRequestHandler<RegisterStageDefinitionCommand, Result<Guid>>
{
    private readonly IStageDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="RegisterStageDefinitionCommandHandler"/>.</summary>
    public RegisterStageDefinitionCommandHandler(
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
    public async Task<Result<Guid>> Handle(RegisterStageDefinitionCommand request, CancellationToken ct)
    {
        var definition = StageDefinition.Register(
            request.TenantId,
            request.StageCode,
            request.DisplayName,
            request.ModuleEndpoint);

        await _repository.AddAsync(definition, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = definition.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success(definition.Id);
    }
}
