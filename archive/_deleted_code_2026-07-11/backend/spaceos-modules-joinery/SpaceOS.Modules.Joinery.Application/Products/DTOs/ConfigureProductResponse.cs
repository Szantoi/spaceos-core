namespace SpaceOS.Modules.Joinery.Application.Products.DTOs;

public sealed record ConfigureProductResponse(
    string ConfigId,
    string? PreviewUrl,
    decimal EstimatedPrice,
    IReadOnlyList<BomPreviewItem> BomPreview
);

public sealed record BomPreviewItem(
    string ItemType,
    string Name,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal TotalPrice
);
