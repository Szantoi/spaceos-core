using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Domain.Services;

public interface IDoorCalculationService
{
    IReadOnlyList<CuttingListItem> CalculateCuttingList(
        DoorItem item,
        DoorTypeRule rule,
        IReadOnlyList<PartDimensionRule> dimRules,
        GlobalConstant constants);

    IReadOnlyList<FinishedDimension> CalculateFinishedDimensions(
        DoorItem item,
        DoorTypeRule rule,
        IReadOnlyList<PartDimensionRule> dimRules,
        GlobalConstant constants);
}
