using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Domain.Services;

public interface IHardwareResolutionService
{
    IReadOnlyList<HardwareListItem> Resolve(DoorItem item, DoorTypeRule rule);
}
