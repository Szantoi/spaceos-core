using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Domain.Results;

public sealed class CalculationResult
{
    public ProductTemplate Template { get; }
    public IReadOnlyDictionary<Guid, ResolvedDimensions> Dimensions { get; }
    public IReadOnlyList<CuttingListItem> CuttingList { get; }
    public IReadOnlyDictionary<string, decimal> Parameters { get; }

    public CalculationResult(
        ProductTemplate template,
        IReadOnlyDictionary<Guid, ResolvedDimensions> dims,
        IReadOnlyList<CuttingListItem> cuttingList,
        IReadOnlyDictionary<string, decimal> parameters)
    {
        Template = template;
        Dimensions = dims;
        CuttingList = cuttingList;
        Parameters = parameters;
    }
}
