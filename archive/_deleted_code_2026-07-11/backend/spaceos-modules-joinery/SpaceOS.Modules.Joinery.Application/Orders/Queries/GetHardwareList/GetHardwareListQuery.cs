using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareList;

public sealed record GetHardwareListQuery(Guid TenantId, Guid OrderId) : IRequest<Result<HardwareListResponse>>;
