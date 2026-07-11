// SpaceOS.Kernel.Application/Tools/Queries/FacilitySummaryDto.cs
namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Compact Facility representation for LLM tool consumption.</summary>
/// <param name="Id">The unique identifier of the facility.</param>
/// <param name="Name">The display name of the facility.</param>
public sealed record FacilitySummaryDto(Guid Id, string Name);
