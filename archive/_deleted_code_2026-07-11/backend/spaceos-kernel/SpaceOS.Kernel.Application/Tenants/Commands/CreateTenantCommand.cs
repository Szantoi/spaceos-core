using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Command to create a new tenant.
/// </summary>
/// <param name="Name">The display name for the tenant.</param>
/// <param name="TenantType">The ecosystem actor type. Defaults to <see cref="TenantType.Manufacturer"/>.</param>
/// <param name="EnabledModules">Optional initial set of enabled module names.</param>
public record CreateTenantCommand(
    string Name,
    TenantType TenantType = TenantType.Manufacturer,
    string[]? EnabledModules = null) : IRequest<Result<Guid>>;
