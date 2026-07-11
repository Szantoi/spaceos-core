// SpaceOS.Kernel.Application/Nodes/Queries/GetManifestQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Nodes.Queries;

/// <summary>Handles <see cref="GetManifestQuery"/> — returns the node manifest DTO or 404.</summary>
internal sealed class GetManifestQueryHandler
    : IRequestHandler<GetManifestQuery, Result<NodeManifestDto>>
{
    private readonly INodeManifestRepository _repository;

    /// <summary>Initialises a new <see cref="GetManifestQueryHandler"/>.</summary>
    public GetManifestQueryHandler(INodeManifestRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Handles retrieval of a node manifest.</summary>
    /// <param name="query">The get-manifest query.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Result{T}.Success"/> with the <see cref="NodeManifestDto"/>,
    /// or <see cref="Result{T}.NotFound"/> when no manifest exists for the tenant.
    /// </returns>
    public async Task<Result<NodeManifestDto>> Handle(GetManifestQuery query, CancellationToken ct)
    {
        var tenantId = TenantId.From(query.TenantId);

        var manifest = await _repository.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);
        if (manifest is null)
            return Result<NodeManifestDto>.NotFound();

        var dto = new NodeManifestDto(
            manifest.Id,
            manifest.TenantId.Value,
            manifest.ServerUrl,
            manifest.PublicApiVersion,
            manifest.LastHeartbeatAt,
            manifest.MaxGuestLod);

        return Result<NodeManifestDto>.Success(dto);
    }
}
