using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

public sealed class HardwareResolutionService : IHardwareResolutionService
{
    public IReadOnlyList<HardwareListItem> Resolve(DoorItem item, DoorTypeRule rule)
        => Array.Empty<HardwareListItem>();
}
