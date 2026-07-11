using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.SpaceLayers.Queries;

/// <summary>
/// Handles <see cref="GetSpaceLayerByIdQuery"/>: loads a SpaceLayer by its identifier
/// and projects it to a <see cref="SpaceLayerDto"/>.
/// </summary>
public class GetSpaceLayerByIdQueryHandler : IRequestHandler<GetSpaceLayerByIdQuery, Result<SpaceLayerDto>>
{
    private readonly ISpaceLayerRepository _spaceLayerRepository;

    /// <summary>
    /// Initialises a new <see cref="GetSpaceLayerByIdQueryHandler"/>.
    /// </summary>
    /// <param name="spaceLayerRepository">Repository for SpaceLayer lookups.</param>
    public GetSpaceLayerByIdQueryHandler(ISpaceLayerRepository spaceLayerRepository)
    {
        ArgumentNullException.ThrowIfNull(spaceLayerRepository);
        _spaceLayerRepository = spaceLayerRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<SpaceLayerDto>> Handle(GetSpaceLayerByIdQuery request, CancellationToken cancellationToken)
    {
        var id = SpaceLayerId.From(request.SpaceLayerId);
        var layer = await _spaceLayerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (layer is null)
            return Result.NotFound();

        return Result.Success(new SpaceLayerDto(
            layer.Id.Value,
            layer.FacilityId.Value,
            layer.TradeType,
            layer.IsExternalNode,
            layer.ExternalSourceUrl,
            layer.IntentDataJson,
            layer.LastStateHash));
    }
}
