// SpaceOS.Kernel.Application/WorkStations/Commands/ArchiveWorkStationCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>Handles <see cref="ArchiveWorkStationCommand"/>: soft-deletes a <see cref="Domain.Entities.WorkStation"/> by setting <c>IsArchived = true</c>.</summary>
internal sealed class ArchiveWorkStationCommandHandler : IRequestHandler<ArchiveWorkStationCommand, Result>
{
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="ArchiveWorkStationCommandHandler"/>.</summary>
    /// <param name="workStationRepository">Repository for workstation persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public ArchiveWorkStationCommandHandler(
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

    /// <summary>Executes the archive command.</summary>
    public async Task<Result> Handle(ArchiveWorkStationCommand request, CancellationToken ct)
    {
        var workStationId = WorkStationId.From(request.Id);
        var workStation = await _workStationRepository.GetByIdAsync(workStationId, ct).ConfigureAwait(false);

        if (workStation is null)
            return Result.NotFound($"WorkStation not found: {request.Id}");

        try
        {
            workStation.Archive();
        }
        catch (DomainException ex)
        {
            return Result.Conflict(ex.Message);
        }

        await _workStationRepository.UpdateAsync(workStation, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = workStation.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.NoContent();
    }
}
