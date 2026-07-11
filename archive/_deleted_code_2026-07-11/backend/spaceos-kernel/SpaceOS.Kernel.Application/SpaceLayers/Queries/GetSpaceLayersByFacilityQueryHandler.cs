// SpaceOS.Kernel.Application/SpaceLayers/Queries/GetSpaceLayersByFacilityQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.SpaceLayers.Queries;

/// <summary>
/// Handles <see cref="GetSpaceLayersByFacilityQuery"/>: returns a paged list of SpaceLayers for the given facility.
/// </summary>
public sealed class GetSpaceLayersByFacilityQueryHandler
    : IRequestHandler<GetSpaceLayersByFacilityQuery, Result<PagedList<SpaceLayerDto>>>
{
    private readonly ISpaceLayerRepository _spaceLayerRepository;

    /// <summary>Initialises a new <see cref="GetSpaceLayersByFacilityQueryHandler"/>.</summary>
    /// <param name="spaceLayerRepository">The space layer repository.</param>
    public GetSpaceLayersByFacilityQueryHandler(ISpaceLayerRepository spaceLayerRepository)
    {
        ArgumentNullException.ThrowIfNull(spaceLayerRepository);
        _spaceLayerRepository = spaceLayerRepository;
    }

    /// <summary>Executes the query and returns a paged result of space layer DTOs.</summary>
    public async Task<Result<PagedList<SpaceLayerDto>>> Handle(
        GetSpaceLayersByFacilityQuery request,
        CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);

        var countSpec = new SpaceLayersByFacilityIdSpec(facilityId);
        var totalCount = await _spaceLayerRepository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new SpaceLayersByFacilityPagedSpec(facilityId, request.Page, request.PageSize);
        var layers = await _spaceLayerRepository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = layers
            .Select(sl => new SpaceLayerDto(
                sl.Id.Value,
                sl.FacilityId.Value,
                sl.TradeType,
                sl.IsExternalNode,
                sl.ExternalSourceUrl,
                sl.IntentDataJson,
                sl.LastStateHash))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<SpaceLayerDto>(items, request.Page, request.PageSize, totalCount));
    }
}
