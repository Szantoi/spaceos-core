public sealed record CreateDoorOrderRequest(
    Guid FlowEpicId, string ProjectId, string ProjectName,
    string? ClientName, string? ClientAddress, string? ClientPhone, DateOnly? DeliveryDate);

public sealed record AddDoorItemRequest(
    string Sorszam, string? Name, int Quantity, string DoorType, string OpeningDirection,
    decimal WallOpeningWidth, decimal DoorWidth, decimal WallOpeningHeight,
    decimal DoorHeight, decimal WallOpeningThickness, decimal DoorThickness);

public sealed record GenerateGyartasilapRequest(
    Guid JoineryOrderId,
    Guid? CuttingPlanId,
    string LabelVariant = "L1");

public sealed record GenerateBatchRequest(
    Guid OrderId,
    List<Guid> GyartasilapIds);

public sealed record GenerateAnyaglistaRequest(Guid OrderId);
