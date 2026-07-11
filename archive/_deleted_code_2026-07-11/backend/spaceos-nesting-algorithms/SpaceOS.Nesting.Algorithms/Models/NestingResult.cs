namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>Complete result of a nesting computation across all panels.</summary>
public sealed record NestingResult(
    IReadOnlyList<PanelAssignment> Assignments,
    IReadOnlyList<NestingPart> UnplacedParts,
    decimal TotalWastePercentage,
    int PanelsUsed,
    string AlgorithmUsed,
    TimeSpan ComputationTime);
