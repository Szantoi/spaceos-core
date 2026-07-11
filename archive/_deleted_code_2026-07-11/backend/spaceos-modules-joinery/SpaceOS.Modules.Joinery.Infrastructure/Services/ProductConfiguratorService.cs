using System.Text.Json;
using Ardalis.Result;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Application.Products.Services;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

/// <summary>
/// Pure service for product configuration validation, BOM calculation and pricing.
/// No I/O, no side effects - depends only on input parameters.
/// </summary>
public sealed class ProductConfiguratorService : IProductConfiguratorService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Result ValidateConfiguration(
        ProductTemplate template,
        DimensionsDto dimensions,
        MaterialsDto materials,
        FittingsDto fittings)
    {
        var errors = new List<ValidationError>();

        // Parse dimension rules
        var dimRules = ParseDimensionRules(template.DimensionRules);

        // Validate width
        if (dimensions.Width < dimRules.MinWidth)
            errors.Add(new ValidationError("Dimensions.Width", $"Width must be at least {dimRules.MinWidth}mm."));
        if (dimensions.Width > dimRules.MaxWidth)
            errors.Add(new ValidationError("Dimensions.Width", $"Width cannot exceed {dimRules.MaxWidth}mm."));

        // Validate height
        if (dimensions.Height < dimRules.MinHeight)
            errors.Add(new ValidationError("Dimensions.Height", $"Height must be at least {dimRules.MinHeight}mm."));
        if (dimensions.Height > dimRules.MaxHeight)
            errors.Add(new ValidationError("Dimensions.Height", $"Height cannot exceed {dimRules.MaxHeight}mm."));

        // Validate thickness
        if (dimRules.AllowedThickness.Length > 0 && !dimRules.AllowedThickness.Contains((int)dimensions.Thickness))
            errors.Add(new ValidationError("Dimensions.Thickness", $"Thickness must be one of: {string.Join(", ", dimRules.AllowedThickness)}mm."));

        // Parse and validate materials
        var allowedMaterials = ParseMaterialList(template.AllowedMaterials);
        if (!allowedMaterials.Any(m => m.Id == materials.Core))
            errors.Add(new ValidationError("Materials.Core", $"Core material '{materials.Core}' is not allowed for this product type."));
        if (!string.IsNullOrEmpty(materials.Veneer) && !allowedMaterials.Any(m => m.Id == materials.Veneer))
            errors.Add(new ValidationError("Materials.Veneer", $"Veneer '{materials.Veneer}' is not allowed for this product type."));
        if (!string.IsNullOrEmpty(materials.Edge) && !allowedMaterials.Any(m => m.Id == materials.Edge))
            errors.Add(new ValidationError("Materials.Edge", $"Edge '{materials.Edge}' is not allowed for this product type."));

        // Parse and validate fittings
        var allowedFittings = ParseFittingList(template.AllowedFittings);
        if (!allowedFittings.Any(f => f.Id == fittings.Hinge))
            errors.Add(new ValidationError("Fittings.Hinge", $"Hinge '{fittings.Hinge}' is not allowed for this product type."));
        if (!string.IsNullOrEmpty(fittings.Handle) && !allowedFittings.Any(f => f.Id == fittings.Handle))
            errors.Add(new ValidationError("Fittings.Handle", $"Handle '{fittings.Handle}' is not allowed for this product type."));
        if (!string.IsNullOrEmpty(fittings.Lock) && !allowedFittings.Any(f => f.Id == fittings.Lock))
            errors.Add(new ValidationError("Fittings.Lock", $"Lock '{fittings.Lock}' is not allowed for this product type."));

        return errors.Count > 0
            ? Result.Invalid(errors)
            : Result.Success();
    }

    public IReadOnlyList<BomPreviewItem> CalculateBom(
        ProductTemplate template,
        DimensionsDto dimensions,
        MaterialsDto materials,
        FittingsDto fittings)
    {
        var items = new List<BomPreviewItem>();
        var allowedMaterials = ParseMaterialList(template.AllowedMaterials);
        var allowedFittings = ParseFittingList(template.AllowedFittings);

        // Core material (quantity = 1 panel)
        var coreMaterial = allowedMaterials.FirstOrDefault(m => m.Id == materials.Core);
        if (coreMaterial != null)
        {
            var panelName = $"{coreMaterial.Name} ({dimensions.Width:F0}×{dimensions.Height:F0})";
            items.Add(new BomPreviewItem(
                ItemType: "material",
                Name: panelName,
                Quantity: 1,
                Unit: "db",
                UnitPrice: coreMaterial.UnitPrice,
                TotalPrice: coreMaterial.UnitPrice
            ));
        }

        // Veneer (quantity = area in m², both sides)
        var veneerMaterial = allowedMaterials.FirstOrDefault(m => m.Id == materials.Veneer);
        if (veneerMaterial != null)
        {
            var areaM2 = (dimensions.Width / 1000m) * (dimensions.Height / 1000m) * 2; // both sides
            var veneerName = $"{veneerMaterial.Name} ({dimensions.Width:F0}×{dimensions.Height:F0})";
            items.Add(new BomPreviewItem(
                ItemType: "veneer",
                Name: veneerName,
                Quantity: Math.Round(areaM2, 2),
                Unit: "m²",
                UnitPrice: veneerMaterial.UnitPrice,
                TotalPrice: Math.Round(areaM2 * veneerMaterial.UnitPrice, 2)
            ));
        }

        // Edge (quantity = perimeter in meters)
        var edgeMaterial = allowedMaterials.FirstOrDefault(m => m.Id == materials.Edge);
        if (edgeMaterial != null)
        {
            var perimeterM = ((dimensions.Width * 2) + (dimensions.Height * 2)) / 1000m;
            items.Add(new BomPreviewItem(
                ItemType: "edge",
                Name: edgeMaterial.Name,
                Quantity: Math.Round(perimeterM, 2),
                Unit: "fm",
                UnitPrice: edgeMaterial.UnitPrice,
                TotalPrice: Math.Round(perimeterM * edgeMaterial.UnitPrice, 2)
            ));
        }

        // Hinges (quantity = 3 for standard door)
        var hinge = allowedFittings.FirstOrDefault(f => f.Id == fittings.Hinge);
        if (hinge != null)
        {
            var hingeCount = dimensions.Height > 2100 ? 4 : 3;
            items.Add(new BomPreviewItem(
                ItemType: "fitting",
                Name: hinge.Name,
                Quantity: hingeCount,
                Unit: "db",
                UnitPrice: hinge.UnitPrice,
                TotalPrice: hingeCount * hinge.UnitPrice
            ));
        }

        // Handle (quantity = 1)
        var handle = allowedFittings.FirstOrDefault(f => f.Id == fittings.Handle);
        if (handle != null)
        {
            items.Add(new BomPreviewItem(
                ItemType: "fitting",
                Name: handle.Name,
                Quantity: 1,
                Unit: "db",
                UnitPrice: handle.UnitPrice,
                TotalPrice: handle.UnitPrice
            ));
        }

        // Lock (quantity = 1)
        var lockFitting = allowedFittings.FirstOrDefault(f => f.Id == fittings.Lock);
        if (lockFitting != null)
        {
            items.Add(new BomPreviewItem(
                ItemType: "fitting",
                Name: lockFitting.Name,
                Quantity: 1,
                Unit: "db",
                UnitPrice: lockFitting.UnitPrice,
                TotalPrice: lockFitting.UnitPrice
            ));
        }

        return items;
    }

    public decimal CalculatePrice(ProductTemplate template, IReadOnlyList<BomPreviewItem> bomItems)
    {
        var pricingRules = ParsePricingRules(template.PricingRules);

        // Sum of BOM items
        var materialsCost = bomItems.Sum(i => i.TotalPrice);

        // Add labor and setup
        var labor = pricingRules.LaborRate;
        var setup = pricingRules.SetupCost;

        // Calculate subtotal
        var subtotal = materialsCost + labor + setup;

        // Apply margin
        var margin = subtotal * (pricingRules.MarginPercent / 100m);

        return Math.Round(subtotal + margin, 0);
    }

    // ── Helper methods ──────────────────────────────────────────────────────

    private static DimensionRules ParseDimensionRules(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DimensionRules>(json, JsonOptions) ?? new DimensionRules();
        }
        catch
        {
            return new DimensionRules();
        }
    }

    private static IReadOnlyList<MaterialItem> ParseMaterialList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<MaterialItem>>(json, JsonOptions) ?? new List<MaterialItem>();
        }
        catch
        {
            return new List<MaterialItem>();
        }
    }

    private static IReadOnlyList<FittingItem> ParseFittingList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<FittingItem>>(json, JsonOptions) ?? new List<FittingItem>();
        }
        catch
        {
            return new List<FittingItem>();
        }
    }

    private static PricingRules ParsePricingRules(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PricingRules>(json, JsonOptions) ?? new PricingRules();
        }
        catch
        {
            return new PricingRules();
        }
    }

    // ── Internal DTOs for JSON parsing ──────────────────────────────────────

    private sealed record DimensionRules
    {
        public decimal MinWidth { get; init; } = 600;
        public decimal MaxWidth { get; init; } = 1200;
        public decimal MinHeight { get; init; } = 1800;
        public decimal MaxHeight { get; init; } = 2400;
        public int[] AllowedThickness { get; init; } = Array.Empty<int>();
    }

    private sealed record MaterialItem
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
    }

    private sealed record FittingItem
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
    }

    private sealed record PricingRules
    {
        public decimal LaborRate { get; init; } = 5000;
        public decimal MarginPercent { get; init; } = 15;
        public decimal SetupCost { get; init; } = 2000;
    }
}
