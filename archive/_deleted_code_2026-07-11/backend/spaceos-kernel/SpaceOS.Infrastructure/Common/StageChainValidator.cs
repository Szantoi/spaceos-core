// SpaceOS.Infrastructure/Common/StageChainValidator.cs
using System.Collections.Generic;
using System.Linq;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Infrastructure.Common;

/// <summary>
/// Validates stage-advance operations against the ordered steps of a chain template (BE-01 / SEC-03).
/// Ensures the target stage exists in the chain, that the transition is forward-only,
/// and that no required intermediate stages would be skipped.
/// </summary>
internal sealed class StageChainValidator : IStageChainValidator
{
    /// <inheritdoc/>
    public void ValidateAdvance(
        FlowEpic epic,
        string targetStageCode,
        IReadOnlyList<StageChainStep> chainSteps)
    {
        if (!epic.StageChainTemplateId.HasValue)
            throw new DomainException("No stage chain is assigned to this FlowEpic.");

        var currentStep = chainSteps.FirstOrDefault(s => s.StageCode == epic.CurrentStageCode);
        var targetStep  = chainSteps.FirstOrDefault(s => s.StageCode == targetStageCode);

        if (targetStep is null)
            throw new DomainException($"Stage '{targetStageCode}' is not part of the assigned chain.");

        if (currentStep is not null && targetStep.SortOrder <= currentStep.SortOrder)
            throw new DomainException(
                $"Cannot advance backward: '{epic.CurrentStageCode}' (order {currentStep.SortOrder}) " +
                $"→ '{targetStageCode}' (order {targetStep.SortOrder}).");

        // SEC-03: required stages between current and target must not be skipped
        var currentOrder = currentStep?.SortOrder ?? 0;
        var skippedRequired = chainSteps
            .Where(s =>
                s.SortOrder > currentOrder &&
                s.SortOrder < targetStep.SortOrder &&
                !s.IsOptional)
            .ToList();

        if (skippedRequired.Count > 0)
            throw new DomainException(
                $"Cannot skip required stages: {string.Join(", ", skippedRequired.Select(s => s.StageCode))}.");
    }
}
