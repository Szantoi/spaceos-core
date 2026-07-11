using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Domain.Services;

public interface IMaterialRequirementService
{
    IReadOnlyList<MaterialRequirement> Calculate(IReadOnlyList<CuttingListItem> items);
}
