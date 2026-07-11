using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CalculateDoorOrder;

public sealed record CalculateDoorOrderCommand(Guid TenantId, Guid OrderId) : IRequest<Result<CuttingListResponse>>;
