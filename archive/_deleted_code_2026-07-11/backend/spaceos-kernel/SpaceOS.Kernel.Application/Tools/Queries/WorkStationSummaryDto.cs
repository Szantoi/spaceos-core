// SpaceOS.Kernel.Application/Tools/Queries/WorkStationSummaryDto.cs
namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Compact WorkStation representation for LLM tool consumption.</summary>
/// <param name="Id">The unique identifier of the workstation.</param>
/// <param name="Name">The display name of the workstation.</param>
/// <param name="Status">The current operational status as a string.</param>
/// <param name="FacilityId">The identifier of the facility this workstation belongs to.</param>
public sealed record WorkStationSummaryDto(Guid Id, string Name, string Status, Guid FacilityId);
