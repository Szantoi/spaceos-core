using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.SpaceLayers;

/// <summary>
/// Data transfer object representing a SpaceLayer returned by application queries.
/// </summary>
/// <param name="Id">The unique identifier of the SpaceLayer.</param>
/// <param name="FacilityId">The identifier of the facility this layer belongs to.</param>
/// <param name="TradeType">The trade discipline this layer represents.</param>
/// <param name="IsExternalNode">
/// <see langword="true"/> when this layer is a federated reference to an external source;
/// <see langword="false"/> when it owns its intent data locally.
/// </param>
/// <param name="ExternalSourceUrl">The source URL when <paramref name="IsExternalNode"/> is <see langword="true"/>; otherwise <see langword="null"/>.</param>
/// <param name="IntentDataJson">The raw JSON intent payload when <paramref name="IsExternalNode"/> is <see langword="false"/>; otherwise <see langword="null"/>.</param>
/// <param name="LastStateHash">The SHA-256 hash of the last known intent data state; <see langword="null"/> for external layers.</param>
public record SpaceLayerDto(
    Guid      Id,
    Guid      FacilityId,
    TradeType TradeType,
    bool      IsExternalNode,
    string?   ExternalSourceUrl,
    string?   IntentDataJson,
    string?   LastStateHash);
