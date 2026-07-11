using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProcessPlan;

public sealed record GetProcessPlanQuery(Guid TenantId, Guid OrderId) : IRequest<Result<ProcessPlanResponse>>;
