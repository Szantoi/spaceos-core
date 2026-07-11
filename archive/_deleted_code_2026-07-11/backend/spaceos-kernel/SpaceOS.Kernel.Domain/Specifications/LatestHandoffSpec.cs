// SpaceOS.Kernel.Domain/Specifications/LatestHandoffSpec.cs
using System;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns the most recent <see cref="StageHandoff"/> for a given (FlowEpic, source, target) triple.</summary>
public sealed class LatestHandoffSpec : SingleResultSpecification<StageHandoff>
{
    /// <summary>
    /// Initialises a new <see cref="LatestHandoffSpec"/>.
    /// </summary>
    /// <param name="flowEpicId">The identifier of the flow epic.</param>
    /// <param name="sourceStage">The source stage code.</param>
    /// <param name="targetStage">The target stage code.</param>
    public LatestHandoffSpec(Guid flowEpicId, string sourceStage, string targetStage)
    {
        Query
            .Where(h =>
                h.FlowEpicId      == flowEpicId &&
                h.SourceStageCode == sourceStage &&
                h.TargetStageCode == targetStage)
            .OrderByDescending(h => h.Version);
    }
}
