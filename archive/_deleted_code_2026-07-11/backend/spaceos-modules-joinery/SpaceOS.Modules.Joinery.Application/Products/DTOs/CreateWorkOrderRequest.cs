namespace SpaceOS.Modules.Joinery.Application.Products.DTOs;

public sealed record CreateWorkOrderRequest(
    string ConfigId,
    int Quantity,
    DateOnly DeliveryDate,
    string? CustomerRef,
    string? Notes
);
