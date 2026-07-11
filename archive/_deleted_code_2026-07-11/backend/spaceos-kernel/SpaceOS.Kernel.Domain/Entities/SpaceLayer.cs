using System.Security.Cryptography;
using System.Text;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents a specific trade's spatial data layer within a Facility.
/// A SpaceLayer is either a local data holder (IntentDataJson) or a
/// federated pointer to an external SpaceOS node (IsExternalNode = true).
/// </summary>
public class SpaceLayer : AggregateRoot
{
    /// <summary>Gets the unique identifier of this space layer.</summary>
    public SpaceLayerId Id               { get; init; }

    /// <summary>Gets the identifier of the facility this layer belongs to.</summary>
    public FacilityId   FacilityId       { get; init; }

    /// <summary>Gets the trade type classification of this layer.</summary>
    public TradeType    TradeType        { get; init; }

    /// <summary>Gets a value indicating whether this layer is a federated pointer to an external SpaceOS node.</summary>
    public bool         IsExternalNode   { get; private set; }

    /// <summary>Gets the URL of the external SpaceOS node when <see cref="IsExternalNode"/> is true.</summary>
    public string?      ExternalSourceUrl { get; private set; }

    /// <summary>Gets the raw JSON intent data for local layers.</summary>
    public string?      IntentDataJson   { get; private set; }

    /// <summary>Gets the SHA-256 hash of the current intent data state.</summary>
    public string?      LastStateHash    { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this space layer.</summary>
    public TenantId     TenantId         { get; init; }

    /// <summary>
    /// Gets the Key Vault reference name for the external auth token.
    /// Resolve the actual secret at runtime via ISecretProvider. Never log this value.
    /// </summary>
    public string?      ExternalAuthTokenRef { get; private set; }

    /// <summary>Gets a value indicating whether this space layer has been archived (soft-deleted).</summary>
    public bool         IsArchived        { get; private set; }

    private SpaceLayer(
        SpaceLayerId id,
        FacilityId   facilityId,
        TradeType    tradeType,
        bool         isExternalNode,
        string?      externalSourceUrl,
        string?      intentDataJson,
        string?      lastStateHash,
        TenantId     tenantId,
        string?      externalAuthTokenRef)
    {
        Id                   = id;
        FacilityId           = facilityId;
        TradeType            = tradeType;
        IsExternalNode       = isExternalNode;
        ExternalSourceUrl    = externalSourceUrl;
        IntentDataJson       = intentDataJson;
        LastStateHash        = lastStateHash;
        TenantId             = tenantId;
        ExternalAuthTokenRef = externalAuthTokenRef;
    }

    /// <summary>
    /// Creates a local SpaceLayer that owns its spatial JSON data directly.
    /// The initial LastStateHash is a SHA-256 fingerprint of the supplied JSON.
    /// </summary>
    /// <param name="intentDataJson">The raw JSON intent data for this layer.</param>
    /// <param name="facilityId">The identifier of the owning facility.</param>
    /// <param name="tradeType">The trade type classification.</param>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    public static SpaceLayer CreateLocalLayer(
        string     intentDataJson,
        FacilityId facilityId,
        TradeType  tradeType,
        TenantId   tenantId)
    {
        if (string.IsNullOrWhiteSpace(intentDataJson))
            throw new DomainException("IntentDataJson cannot be empty for a local SpaceLayer.");

        var layer = new SpaceLayer(
            id:                SpaceLayerId.New(),
            facilityId:        facilityId,
            tradeType:         tradeType,
            isExternalNode:    false,
            externalSourceUrl: null,
            intentDataJson:       intentDataJson,
            lastStateHash:        ComputeHash(intentDataJson),
            tenantId:             tenantId,
            externalAuthTokenRef: null);

        layer.AddDomainEvent(new SpaceLayerRegisteredEvent(
            layer.Id, facilityId, tradeType, false, DateTimeOffset.UtcNow));

        return layer;
    }

    /// <summary>
    /// Creates a federated SpaceLayer that acts as a pointer to an external SpaceOS node.
    /// No local JSON data is stored; the source of truth lives at ExternalSourceUrl.
    /// </summary>
    /// <param name="externalSourceUrl">The URL of the external SpaceOS node.</param>
    /// <param name="facilityId">The identifier of the owning facility.</param>
    /// <param name="tradeType">The trade type classification.</param>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="externalAuthTokenRef">
    /// Optional Key Vault reference name for the external node auth token.
    /// Resolve the actual secret at runtime via ISecretProvider. Never log this value.
    /// </param>
    public static SpaceLayer CreateExternalLayer(
        string     externalSourceUrl,
        FacilityId facilityId,
        TradeType  tradeType,
        TenantId   tenantId,
        string?    externalAuthTokenRef = null)
    {
        if (string.IsNullOrWhiteSpace(externalSourceUrl))
            throw new DomainException("ExternalSourceUrl is required when creating a federated (external) SpaceLayer.");

        var layer = new SpaceLayer(
            id:                   SpaceLayerId.New(),
            facilityId:           facilityId,
            tradeType:            tradeType,
            isExternalNode:       true,
            externalSourceUrl:    externalSourceUrl,
            intentDataJson:       null,
            lastStateHash:        null,
            tenantId:             tenantId,
            externalAuthTokenRef: externalAuthTokenRef);

        layer.AddDomainEvent(new SpaceLayerRegisteredEvent(
            layer.Id, facilityId, tradeType, true, DateTimeOffset.UtcNow));

        return layer;
    }

    /// <summary>
    /// Updates the local intent data and recomputes the state hash.
    /// Only valid on non-external layers.
    /// </summary>
    public void UpdateIntentData(string newJson)
    {
        if (IsExternalNode)
            throw new DomainException("Cannot update IntentDataJson on a federated (external) SpaceLayer.");

        if (string.IsNullOrWhiteSpace(newJson))
            throw new DomainException("IntentDataJson cannot be empty.");

        IntentDataJson = newJson;
        LastStateHash  = ComputeHash(newJson);
        AddDomainEvent(new SpaceLayerIntentUpdatedEvent(Id, LastStateHash!, DateTimeOffset.UtcNow));
    }

    /// <summary>Archives this space layer, preventing it from appearing in list results.</summary>
    /// <exception cref="DomainException">Thrown when the space layer is already archived.</exception>
    public void Archive()
    {
        if (IsArchived)
            throw new DomainException($"{nameof(SpaceLayer)} is already archived.");
        IsArchived = true;
        AddDomainEvent(new SpaceLayerArchivedEvent(Id, DateTimeOffset.UtcNow));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
