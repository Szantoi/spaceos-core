using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;

public sealed record AddDoorItemCommand(
    Guid TenantId,
    Guid OrderId,
    string Sorszam,
    string? Name,
    int Quantity,
    string DoorType,
    string OpeningDirection,
    decimal WallOpeningWidth,
    decimal DoorWidth,
    decimal WallOpeningHeight,
    decimal DoorHeight,
    decimal WallOpeningThickness,
    decimal DoorThickness
) : IRequest<Result<Guid>>;
