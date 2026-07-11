// SpaceOS.Kernel.Application/Tools/Queries/FlowEpicSummaryDto.cs
namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Compact FlowEpic representation for LLM tool consumption.</summary>
/// <param name="Id">The unique identifier of the epic.</param>
/// <param name="Title">The title describing the epic's scope.</param>
/// <param name="Phase">The current workflow phase as a string.</param>
/// <param name="TargetFacilityId">The identifier of the facility targeted by this epic.</param>
public sealed record FlowEpicSummaryDto(Guid Id, string Title, string Phase, Guid TargetFacilityId);
