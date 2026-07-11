using MediatR;
using Ardalis.Result;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Command to register a new workstation within a facility.
/// Returns the <see cref="Guid"/> of the newly created workstation on success.
/// </summary>
/// <param name="Name">The display name for the workstation.</param>
/// <param name="Type">The type classification string (validated by domain rules).</param>
/// <param name="FacilityId">The identifier of the facility this workstation belongs to.</param>
/// <param name="TenantId">The identifier of the tenant that owns this workstation.</param>
public record RegisterWorkStationCommand(string Name, string Type, Guid FacilityId, Guid TenantId) : IRequest<Result<Guid>>;
