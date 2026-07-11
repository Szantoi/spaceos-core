// SpaceOS.Kernel.Domain/Specifications/ChainStepsByTemplateSpec.cs
using System;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns all <see cref="StageChainStep"/> records for a given chain template, ordered by sort order.</summary>
public sealed class ChainStepsByTemplateSpec : Specification<StageChainStep>
{
    /// <summary>Initialises a new <see cref="ChainStepsByTemplateSpec"/> for the given chain template.</summary>
    /// <param name="chainTemplateId">The identifier of the chain template whose steps to return.</param>
    public ChainStepsByTemplateSpec(Guid chainTemplateId)
    {
        Query.Where(s => s.ChainTemplateId == chainTemplateId).OrderBy(s => s.SortOrder);
    }
}
