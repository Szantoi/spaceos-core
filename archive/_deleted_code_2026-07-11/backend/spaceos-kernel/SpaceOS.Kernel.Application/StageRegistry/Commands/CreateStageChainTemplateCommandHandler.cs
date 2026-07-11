// SpaceOS.Kernel.Application/StageRegistry/Commands/CreateStageChainTemplateCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Handles <see cref="CreateStageChainTemplateCommand"/>: creates and persists a new chain template.</summary>
internal sealed class CreateStageChainTemplateCommandHandler
    : IRequestHandler<CreateStageChainTemplateCommand, Result<Guid>>
{
    private readonly IStageChainTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="CreateStageChainTemplateCommandHandler"/>.</summary>
    public CreateStageChainTemplateCommandHandler(
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
    public async Task<Result<Guid>> Handle(CreateStageChainTemplateCommand request, CancellationToken ct)
    {
        var template = StageChainTemplate.Create(request.TenantId, request.Name, request.IsDefault);

        await _repository.AddAsync(template, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var events = template.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result.Success(template.Id);
    }
}
