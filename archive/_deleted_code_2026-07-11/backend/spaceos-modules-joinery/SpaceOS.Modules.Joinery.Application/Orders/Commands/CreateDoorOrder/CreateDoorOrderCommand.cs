using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateDoorOrder;

public sealed record CreateDoorOrderCommand(
    Guid TenantId,
    Guid FlowEpicId,
    string ProjectId,
    string ProjectName,
    string? ClientName,
    string? ClientAddress,
    string? ClientPhone,
    DateOnly? DeliveryDate
) : IRequest<Result<Guid>>;
