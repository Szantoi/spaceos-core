namespace SpaceOS.Modules.Joinery.Application.Orders.DTOs;

public sealed record DoorOrderDto(
    Guid Id,
    Guid TenantId,
    Guid FlowEpicId,
    string ProjectId,
    string ProjectName,
    string Status,
    int ItemCount,
    DateOnly? DeliveryDate,
    DateTime CreatedAt);
