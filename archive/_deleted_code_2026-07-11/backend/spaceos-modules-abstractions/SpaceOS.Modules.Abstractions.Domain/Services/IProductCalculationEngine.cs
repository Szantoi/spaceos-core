using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Domain.Services;

public interface IProductCalculationEngine
{
    CalculationResult Calculate(
        ProductTemplate template,
        DimensionInput root,
        IReadOnlyDictionary<string, decimal>? parameterOverrides = null);
}
