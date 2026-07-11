using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.WorkStations;

/// <summary>
/// Data transfer object representing a workstation returned by application queries and commands.
/// </summary>
/// <param name="Id">The unique identifier of the workstation.</param>
/// <param name="Name">The display name of the workstation.</param>
/// <param name="Type">The type classification of the workstation.</param>
/// <param name="FacilityId">The identifier of the facility this workstation belongs to.</param>
/// <param name="Status">The current operational status of the workstation.</param>
public record WorkStationDto(
    Guid Id,
    string Name,
    string Type,
    Guid FacilityId,
    WorkStationStatus Status);
