using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// Updates the intent data JSON of a local (non-federated) SpaceLayer.
/// </summary>
/// <param name="SpaceLayerId">The identifier of the SpaceLayer to update.</param>
/// <param name="IntentDataJson">The new intent data JSON string.</param>
/// <param name="TradeType">
/// Optional trade type used to select the structural JSON schema for validation.
/// When <c>null</c>, generic schema rules apply (max depth 10, max 64 KB, object or array).
/// </param>
public sealed record UpdateSpaceLayerIntentDataCommand(
    Guid       SpaceLayerId,
    string     IntentDataJson,
    TradeType? TradeType = null) : IRequest<Result>;
