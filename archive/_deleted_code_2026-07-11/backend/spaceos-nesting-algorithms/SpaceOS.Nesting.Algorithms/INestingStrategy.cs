using SpaceOS.Nesting.Algorithms.Models;

namespace SpaceOS.Nesting.Algorithms;

public interface INestingStrategy
{
    string AlgorithmName { get; }
    Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default);
}
