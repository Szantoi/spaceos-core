using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Handles <see cref="UpdateWorkStationNameCommand"/>: loads the workstation, updates its name,
/// persists the change, and dispatches any raised domain events.
/// </summary>
public class UpdateWorkStationNameCommandHandler : IRequestHandler<UpdateWorkStationNameCommand, Result>
{
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UpdateWorkStationNameCommandHandler(
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

    public async Task<Result> Handle(UpdateWorkStationNameCommand request, CancellationToken ct)
    {
        var workStationId = WorkStationId.From(request.WorkStationId);
        var workStation = await _workStationRepository.GetByIdAsync(workStationId, ct).ConfigureAwait(false);

        if (workStation is null)
        {
            return Result.NotFound($"WorkStation not found: {request.WorkStationId}");
        }

        workStation.UpdateName(request.NewName);

        await _workStationRepository.UpdateAsync(workStation, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = workStation.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
