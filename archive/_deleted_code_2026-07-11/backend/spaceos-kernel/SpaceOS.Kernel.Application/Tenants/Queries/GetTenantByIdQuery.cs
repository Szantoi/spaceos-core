using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

public record GetTenantByIdQuery(Guid TenantId) : IRequest<Result<TenantDto>>;
