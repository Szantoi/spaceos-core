using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// Registers a new SpaceLayer (local or federated) for a given Facility.
/// Returns the new SpaceLayerId as a Guid on success.
/// </summary>
/// <param name="FacilityId">The identifier of the owning facility.</param>
/// <param name="TradeType">The trade type classification for this layer.</param>
/// <param name="IsExternalNode">When <c>true</c>, the layer is a federated pointer; otherwise it holds local data.</param>
/// <param name="ExternalSourceUrl">Required when <paramref name="IsExternalNode"/> is <c>true</c>.</param>
/// <param name="IntentDataJson">Required when <paramref name="IsExternalNode"/> is <c>false</c>.</param>
/// <param name="TenantId">The identifier of the tenant that owns this layer.</param>
public record RegisterSpaceLayerCommand(
    Guid      FacilityId,
    TradeType TradeType,
    bool      IsExternalNode,
    string?   ExternalSourceUrl,
    string?   IntentDataJson,
    Guid      TenantId
) : IRequest<Result<Guid>>;
