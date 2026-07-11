using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Saved product configuration from configurator endpoint.
/// Stores configuration parameters, BOM snapshot and estimated price.
/// </summary>
public sealed class ProductConfiguration : TenantScopedEntity
{
    public string ProductType { get; private set; } = string.Empty;

    /// <summary>
    /// Configuration parameters (dimensions, materials, fittings) as JSON.
    /// </summary>
    public string Params { get; private set; } = "{}";

    /// <summary>
    /// BOM snapshot at configuration time as JSON array.
    /// </summary>
    public string BomSnapshot { get; private set; } = "[]";

    public decimal EstimatedPrice { get; private set; }
    public string? PreviewUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; private set; }

    private ProductConfiguration() { } // EF Core

    public static ProductConfiguration Create(
        Guid tenantId,
        string productType,
        string paramsJson,
        string bomSnapshotJson,
        decimal estimatedPrice,
        string? previewUrl,
        Guid? createdBy)
    {
        return new ProductConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductType = productType,
            Params = paramsJson,
            BomSnapshot = bomSnapshotJson,
            EstimatedPrice = estimatedPrice,
            PreviewUrl = previewUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }
}
