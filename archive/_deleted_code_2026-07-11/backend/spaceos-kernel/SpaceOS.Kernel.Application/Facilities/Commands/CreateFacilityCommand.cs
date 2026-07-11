using MediatR;
using Ardalis.Result;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// Command to create a new facility for the specified tenant.
/// Returns the <see cref="Guid"/> of the newly created facility on success.
/// </summary>
/// <param name="TenantId">The identifier of the tenant that will own the facility.</param>
/// <param name="Name">The display name for the new facility.</param>
public record CreateFacilityCommand(Guid TenantId, string Name) : IRequest<Result<Guid>>;
