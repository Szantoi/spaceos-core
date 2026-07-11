namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Product template containing validation rules, allowed materials, fittings and pricing rules.
/// Template-based rule engine for configurator validation (Phase 1).
/// </summary>
public class ProductTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }

    /// <summary>
    /// Dimension constraints: { minWidth, maxWidth, minHeight, maxHeight, allowedThickness[] }
    /// </summary>
    public string DimensionRules { get; set; } = "{}";

    /// <summary>
    /// Allowed materials: [ { id, name, type, unitPrice } ]
    /// </summary>
    public string AllowedMaterials { get; set; } = "[]";

    /// <summary>
    /// Allowed fittings: [ { id, name, category, unitPrice } ]
    /// </summary>
    public string AllowedFittings { get; set; } = "[]";

    /// <summary>
    /// Pricing rules: { laborRate, marginPercent, setupCost }
    /// </summary>
    public string PricingRules { get; set; } = "{}";

    public int LeadTimeDays { get; set; } = 7;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
