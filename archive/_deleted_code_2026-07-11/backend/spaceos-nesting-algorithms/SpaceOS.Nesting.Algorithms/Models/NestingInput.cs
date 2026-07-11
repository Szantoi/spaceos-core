namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>Input to a nesting computation.</summary>
public sealed record NestingInput(
    IReadOnlyList<NestingPart> Parts,
    IReadOnlyList<AvailablePanel> Panels,
    int SawBladeGapMm = 4);
