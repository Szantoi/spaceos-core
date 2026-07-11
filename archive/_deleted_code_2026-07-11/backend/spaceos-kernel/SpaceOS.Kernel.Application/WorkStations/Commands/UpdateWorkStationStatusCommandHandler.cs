using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Handles <see cref="UpdateWorkStationStatusCommand"/>: loads the workstation, applies the status
/// change, persists the result, and dispatches any raised domain events.
/// </summary>
public class UpdateWorkStationStatusCommandHandler : IRequestHandler<UpdateWorkStationStatusCommand, Result>
{
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="UpdateWorkStationStatusCommandHandler"/>.
    /// </summary>
    /// <param name="workStationRepository">Repository for workstation persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for publishing domain events after persistence.</param>
    public UpdateWorkStationStatusCommandHandler(
        IWorkStationRepository workStationRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(workStationRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _workStationRepository = workStationRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(UpdateWorkStationStatusCommand request, CancellationToken ct)
    {
        var workStationId = WorkStationId.From(request.Id);
        var workStation = await _workStationRepository.GetByIdAsync(workStationId, ct).ConfigureAwait(false);

        if (workStation is null)
        {
            return Result.NotFound($"WorkStation not found: {request.Id}");
        }

        workStation.ChangeStatus(request.NewStatus);

        await _workStationRepository.UpdateAsync(workStation, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = workStation.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
