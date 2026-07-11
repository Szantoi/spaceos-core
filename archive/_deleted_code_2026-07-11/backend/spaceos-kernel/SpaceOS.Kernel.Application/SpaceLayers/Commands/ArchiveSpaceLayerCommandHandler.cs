// SpaceOS.Kernel.Application/SpaceLayers/Commands/ArchiveSpaceLayerCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>Handles <see cref="ArchiveSpaceLayerCommand"/>: soft-deletes a <see cref="Domain.Entities.SpaceLayer"/> by setting <c>IsArchived = true</c>.</summary>
internal sealed class ArchiveSpaceLayerCommandHandler : IRequestHandler<ArchiveSpaceLayerCommand, Result>
{
    private readonly ISpaceLayerRepository _spaceLayerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="ArchiveSpaceLayerCommandHandler"/>.</summary>
    /// <param name="spaceLayerRepository">Repository for space layer persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public ArchiveSpaceLayerCommandHandler(
        ISpaceLayerRepository spaceLayerRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(spaceLayerRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _spaceLayerRepository = spaceLayerRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>Executes the archive command.</summary>
    public async Task<Result> Handle(ArchiveSpaceLayerCommand request, CancellationToken ct)
    {
        var spaceLayerId = SpaceLayerId.From(request.Id);
        var spaceLayer = await _spaceLayerRepository.GetByIdAsync(spaceLayerId, ct).ConfigureAwait(false);

        if (spaceLayer is null)
            return Result.NotFound($"SpaceLayer not found: {request.Id}");

        try
        {
            spaceLayer.Archive();
        }
        catch (DomainException ex)
        {
            return Result.Conflict(ex.Message);
        }

        await _spaceLayerRepository.UpdateAsync(spaceLayer, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = spaceLayer.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.NoContent();
    }
}
