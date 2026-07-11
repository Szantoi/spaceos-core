using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;

public sealed record GetMaterialRequirementsQuery(Guid TenantId, Guid OrderId) : IRequest<Result<MaterialRequirementsResponse>>;
