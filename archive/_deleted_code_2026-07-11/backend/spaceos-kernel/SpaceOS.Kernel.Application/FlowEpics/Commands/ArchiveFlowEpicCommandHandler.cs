// SpaceOS.Kernel.Application/FlowEpics/Commands/ArchiveFlowEpicCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>Handles <see cref="ArchiveFlowEpicCommand"/>: soft-deletes a <see cref="Domain.Entities.FlowEpic"/> by setting <c>IsArchived = true</c>.</summary>
internal sealed class ArchiveFlowEpicCommandHandler : IRequestHandler<ArchiveFlowEpicCommand, Result>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="ArchiveFlowEpicCommandHandler"/>.</summary>
    /// <param name="flowEpicRepository">Repository for flow epic persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public ArchiveFlowEpicCommandHandler(
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

    /// <summary>Executes the archive command.</summary>
    public async Task<Result> Handle(ArchiveFlowEpicCommand request, CancellationToken ct)
    {
        var flowEpicId = FlowEpicId.From(request.Id);
        var flowEpic = await _flowEpicRepository.GetByIdAsync(flowEpicId, ct).ConfigureAwait(false);

        if (flowEpic is null)
            return Result.NotFound($"FlowEpic not found: {request.Id}");

        try
        {
            flowEpic.Archive();
        }
        catch (DomainException ex)
        {
            return Result.Conflict(ex.Message);
        }

        await _flowEpicRepository.UpdateAsync(flowEpic, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = flowEpic.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.NoContent();
    }
}
