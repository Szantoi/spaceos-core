namespace SpaceOS.Nesting.Algorithms;

public sealed class NestingStrategyFactory
{
    private readonly IReadOnlyDictionary<string, INestingStrategy> _strategies;

    public NestingStrategyFactory(IEnumerable<INestingStrategy> strategies)
        => _strategies = strategies.ToDictionary(s => s.AlgorithmName, StringComparer.OrdinalIgnoreCase);

    public INestingStrategy GetStrategy(string algorithmName)
        => _strategies.TryGetValue(algorithmName, out var s)
            ? s
            : throw new KeyNotFoundException($"No nesting strategy registered for '{algorithmName}'.");

    public IEnumerable<string> AvailableStrategies => _strategies.Keys;
}
