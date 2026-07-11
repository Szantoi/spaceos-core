// SpaceOS.Kernel.Application/Tenants/Commands/UpdateTenantModulesCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Command to update the enabled module list of an existing tenant.
/// Module names are validated against the tenant's <c>TenantType</c> by <c>IModuleRegistryService</c>.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant to update.</param>
/// <param name="Modules">The new set of enabled module names (lowercase identifiers, e.g. "door", "cutting").</param>
public record UpdateTenantModulesCommand(Guid TenantId, string[] Modules) : IRequest<Result>;
