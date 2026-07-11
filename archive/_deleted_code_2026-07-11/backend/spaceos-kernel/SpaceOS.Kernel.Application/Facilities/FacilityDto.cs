using System;

namespace SpaceOS.Kernel.Application.Facilities;

/// <summary>
/// Data transfer object representing a facility returned by application queries and commands.
/// </summary>
/// <param name="Id">The unique identifier of the facility.</param>
/// <param name="Name">The display name of the facility.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
public record FacilityDto(Guid Id, string Name, Guid TenantId);
