using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

public sealed class DoorCalculationService : IDoorCalculationService
{
    public IReadOnlyList<CuttingListItem> CalculateCuttingList(
        DoorItem item, DoorTypeRule rule, IReadOnlyList<PartDimensionRule> dimRules, GlobalConstant constants)
    {
        var results = new List<CuttingListItem>();
        var cuttingOversize = constants.Value; // CuttingOversize = 1mm

        var bkmWidthFixed = item.Dimensions.DoorWidth + rule.BkmWidthFixed;
        var bkmHeightFixed = item.Dimensions.DoorHeight + rule.BkmHeightFixed;

        foreach (var dimRule in dimRules.Where(r => r.DoorType == item.DoorType.ToString()))
        {
            var width = dimRule.WidthBase + (dimRule.WidthMultiplierFactor * bkmWidthFixed) + cuttingOversize;
            var length = dimRule.LengthBase + (dimRule.LengthMultiplierFactor * bkmHeightFixed) + cuttingOversize;

            results.Add(new CuttingListItem(
                item.Sorszam,
                dimRule.ComponentName,
                dimRule.Material ?? string.Empty,
                dimRule.Thickness ?? 0,
                Math.Round(width, 1),
                Math.Round(length, 1),
                dimRule.Quantity * item.Quantity,
                dimRule.ComponentType
            ));
        }
        return results.AsReadOnly();
    }

    public IReadOnlyList<FinishedDimension> CalculateFinishedDimensions(
        DoorItem item, DoorTypeRule rule, IReadOnlyList<PartDimensionRule> dimRules, GlobalConstant constants)
    {
        return dimRules.Where(r => r.DoorType == item.DoorType.ToString())
            .Select(r => new FinishedDimension(
                item.Sorszam, r.ComponentName,
                Math.Round(r.WidthBase + r.WidthMultiplierFactor * (item.Dimensions.DoorWidth + rule.BkmWidthFixed), 1),
                Math.Round(r.LengthBase + r.LengthMultiplierFactor * (item.Dimensions.DoorHeight + rule.BkmHeightFixed), 1),
                r.Quantity * item.Quantity, r.Material ?? string.Empty,
                item.FixSide?.SurfaceType.ToString() ?? string.Empty, null))
            .ToList().AsReadOnly();
    }
}
