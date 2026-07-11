namespace SpaceOS.Modules.Joinery.Application.Products.DTOs;

public sealed record CreateWorkOrderResponse(
    string WorkOrderId,
    string PdfUrl,
    IReadOnlyList<WorkOrderBomItem> BomItems,
    decimal TotalMaterialCost,
    decimal EstimatedLabor,
    decimal TotalCost,
    DateOnly ScheduledStart,
    DateOnly EstimatedCompletion
);

public sealed record WorkOrderBomItem(
    string ItemType,
    string Name,
    decimal Quantity,
    string Unit,
    decimal TotalPrice,
    string? Supplier,
    int InStock,
    int ToOrder
);
