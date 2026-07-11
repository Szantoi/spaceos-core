using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;

public sealed record ListDoorOrdersQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedList<DoorOrderDto>>>;
