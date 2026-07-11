using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.CreateWorkOrder;

public sealed record CreateWorkOrderCommand(
    Guid TenantId,
    Guid ConfigurationId,
    int Quantity,
    DateOnly DeliveryDate,
    string? CustomerRef,
    string? Notes,
    Guid? UserId
) : IRequest<Result<CreateWorkOrderResponse>>;
