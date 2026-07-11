using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

public sealed class MaterialRequirementService : IMaterialRequirementService
{
    public IReadOnlyList<MaterialRequirement> Calculate(IReadOnlyList<CuttingListItem> items)
        => Array.Empty<MaterialRequirement>();
}
