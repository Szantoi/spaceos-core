using SpaceOS.Modules.Abstractions.Domain.Results;

namespace SpaceOS.Modules.Abstractions.Domain.Services;

public interface IManufacturingDerivation
{
    IReadOnlyList<CncOperation> DeriveCncPlan(CalculationResult result);
    IReadOnlyList<ProductionStep> DeriveProcessPlan(CalculationResult result);
    IReadOnlyList<GlassOrderItem> DeriveGlassOrderItems(CalculationResult result);
}
