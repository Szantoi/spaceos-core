using SpaceOS.Nesting.Algorithms.Models;

namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L3 Placeholder — MaxRects is a v2+ feature.
/// </summary>
public sealed class MaxRectsNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "MaxRects";

    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
        => throw new NotImplementedException("MaxRects is a v2+ feature.");
}
