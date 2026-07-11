// SpaceOS.Kernel.Domain/Entities/StageChainStep.cs
using System;
using SpaceOS.Kernel.Domain.Common;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents an ordered step within a <see cref="StageChainTemplate"/>.
/// Each step links a <see cref="StageDefinition"/> to a chain with a sort order and
/// an optional flag that controls whether the stage may be skipped (SEC-03).
/// </summary>
public sealed class StageChainStep : TenantScopedEntity
{
    /// <summary>Gets the identifier of the owning <see cref="StageChainTemplate"/>.</summary>
    public Guid ChainTemplateId { get; private set; }

    /// <summary>Gets the identifier of the referenced <see cref="StageDefinition"/> (DB-04: direct PK FK).</summary>
    public Guid StageDefinitionId { get; private set; }

    /// <summary>Gets the denormalised stage code (BE-04: sourced from <see cref="StageDefinition"/>, not user input).</summary>
    public string StageCode { get; private set; } = string.Empty;

    /// <summary>Gets the 1-based sort order of this step within the chain.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Gets a value indicating whether this step may be skipped without a validator error (SEC-03).</summary>
    public bool IsOptional { get; private set; }

    // EF Core parameterless constructor
    private StageChainStep() { }

    /// <summary>
    /// Creates a new <see cref="StageChainStep"/>. Only called from <see cref="StageChainTemplate.AddStep"/>.
    /// </summary>
    /// <param name="chainTemplateId">The owning chain template identifier.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="stageDefinitionId">The referenced stage definition identifier.</param>
    /// <param name="stageCode">The denormalised stage code sourced from the stage definition.</param>
    /// <param name="sortOrder">The 1-based sort order within the chain.</param>
    /// <param name="isOptional">Whether this step may be skipped.</param>
    /// <returns>A new <see cref="StageChainStep"/> instance.</returns>
    internal static StageChainStep Create(
        Guid chainTemplateId,
        Guid tenantId,
        Guid stageDefinitionId,
        string stageCode,
        int sortOrder,
        bool isOptional)
    {
        return new StageChainStep
        {
            Id                = Guid.NewGuid(),
            ChainTemplateId   = chainTemplateId,
            TenantId          = tenantId,
            StageDefinitionId = stageDefinitionId,
            StageCode         = stageCode,
            SortOrder         = sortOrder,
            IsOptional        = isOptional
        };
    }
}
