using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetDoorOrder;

public sealed record GetDoorOrderQuery(Guid TenantId, Guid OrderId) : IRequest<Result<DoorOrderDto>>;
