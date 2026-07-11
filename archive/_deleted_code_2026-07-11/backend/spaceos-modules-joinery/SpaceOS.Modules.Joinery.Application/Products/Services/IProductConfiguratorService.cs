using Ardalis.Result;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Services;

/// <summary>
/// Pure service for product configuration validation, BOM calculation and pricing.
/// No I/O, no side effects - depends only on input parameters.
/// </summary>
public interface IProductConfiguratorService
{
    /// <summary>
    /// Validates configuration against template rules.
    /// </summary>
    Result ValidateConfiguration(
        ProductTemplate template,
        DimensionsDto dimensions,
        MaterialsDto materials,
        FittingsDto fittings);

    /// <summary>
    /// Calculates Bill of Materials based on configuration.
    /// </summary>
    IReadOnlyList<BomPreviewItem> CalculateBom(
        ProductTemplate template,
        DimensionsDto dimensions,
        MaterialsDto materials,
        FittingsDto fittings);

    /// <summary>
    /// Calculates total price including materials, labor and margin.
    /// </summary>
    decimal CalculatePrice(ProductTemplate template, IReadOnlyList<BomPreviewItem> bomItems);
}
