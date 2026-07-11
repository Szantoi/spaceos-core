using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;

public sealed record GetCuttingListQuery(Guid TenantId, Guid OrderId) : IRequest<Result<CuttingListResponse>>;
