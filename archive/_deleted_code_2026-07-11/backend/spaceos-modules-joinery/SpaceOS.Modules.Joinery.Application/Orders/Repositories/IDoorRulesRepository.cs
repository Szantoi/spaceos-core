using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Application.Orders.Repositories;

public interface IDoorRulesRepository
{
    Task<DoorTypeRule?> GetDoorTypeRuleAsync(string doorType, CancellationToken ct);
    Task<IReadOnlyList<PartDimensionRule>> GetPartDimensionRulesAsync(string doorType, CancellationToken ct);
    Task<IReadOnlyList<GlobalConstant>> GetGlobalConstantsAsync(CancellationToken ct);
    Task<IReadOnlyList<ProcessTaskTemplate>> GetProcessTaskTemplatesAsync(CancellationToken ct);
}
