// SpaceOS.Kernel.Domain/Specifications/HandoffsByFlowEpicSpec.cs
using System;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns all <see cref="StageHandoff"/> records for a given flow epic, ordered by creation time.</summary>
public sealed class HandoffsByFlowEpicSpec : Specification<StageHandoff>
{
    /// <summary>Initialises a new <see cref="HandoffsByFlowEpicSpec"/> for the given epic.</summary>
    /// <param name="flowEpicId">The identifier of the flow epic whose handoffs to return.</param>
    public HandoffsByFlowEpicSpec(Guid flowEpicId)
    {
        Query.Where(h => h.FlowEpicId == flowEpicId).OrderBy(h => h.CreatedAt);
    }
}
