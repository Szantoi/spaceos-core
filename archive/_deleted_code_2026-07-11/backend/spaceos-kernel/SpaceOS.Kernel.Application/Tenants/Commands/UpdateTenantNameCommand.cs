using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Command to update an existing tenant's display name.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="NewName">The new display name.</param>
public record UpdateTenantNameCommand(Guid TenantId, string NewName) : IRequest<Result>;
