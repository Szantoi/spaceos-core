// SpaceOS.Kernel.Domain/Entities/StageChainTemplate.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Tenant-configurable ordered pipeline of <see cref="StageChainStep"/> instances.
/// A tenant may have multiple named chains (e.g. "standard", "felmérős"); at most one
/// may be marked <see cref="IsDefault"/> per tenant (enforced via partial unique index).
/// </summary>
public sealed class StageChainTemplate : TenantScopedAggregateRoot
{
    private const int MaxSteps = 20;

    private readonly List<StageChainStep> _steps = new();

    /// <summary>Gets the name of this chain template (unique per tenant).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this is the default chain for the tenant.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Gets the UTC timestamp when this template was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this template was last modified.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Gets the ordered steps of this chain template.</summary>
    public IReadOnlyList<StageChainStep> Steps => _steps.AsReadOnly();

    // EF Core parameterless constructor
    private StageChainTemplate() { }

    /// <summary>
    /// Creates a new <see cref="StageChainTemplate"/> for a tenant.
    /// Raises <see cref="StageChainCreatedEvent"/>.
    /// </summary>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="name">The unique name for this chain template within the tenant.</param>
    /// <param name="isDefault">Whether this should be the default chain for the tenant.</param>
    /// <returns>A new <see cref="StageChainTemplate"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public static StageChainTemplate Create(Guid tenantId, string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Chain template name is required.");

        var now = DateTimeOffset.UtcNow;
        var template = new StageChainTemplate
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            Name      = name,
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now
        };
        template.AddDomainEvent(new StageChainCreatedEvent(template.Id, tenantId, name, now));
        return template;
    }

    /// <summary>
    /// Adds a step to this chain template.
    /// BE-04: stage code is sourced from the <see cref="StageDefinition"/> entity, not user input.
    /// </summary>
    /// <param name="stageDef">The stage definition to add as a step.</param>
    /// <param name="sortOrder">The 1-based sort order within the chain (must be unique).</param>
    /// <param name="isOptional">Whether this step may be skipped (SEC-03).</param>
    /// <exception cref="DomainException">Thrown when the step would violate chain constraints.</exception>
    public void AddStep(StageDefinition stageDef, int sortOrder, bool isOptional = false)
    {
        if (_steps.Any(s => s.StageCode == stageDef.StageCode))
            throw new DomainException($"Stage '{stageDef.StageCode}' is already present in this chain.");

        if (_steps.Any(s => s.SortOrder == sortOrder))
            throw new DomainException($"SortOrder {sortOrder} is already taken in this chain.");

        if (_steps.Count >= MaxSteps)
            throw new DomainException($"A chain template may not exceed {MaxSteps} steps.");

        _steps.Add(StageChainStep.Create(
            Id, TenantId, stageDef.Id, stageDef.StageCode, sortOrder, isOptional));

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new StageChainStepAddedEvent(Id, TenantId, stageDef.StageCode, sortOrder, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes the step with the given <paramref name="stageCode"/> from this chain template.</summary>
    /// <param name="stageCode">The stage code of the step to remove.</param>
    /// <exception cref="DomainException">Thrown when no step with the given stage code exists.</exception>
    public void RemoveStep(string stageCode)
    {
        var step = _steps.FirstOrDefault(s => s.StageCode == stageCode);
        if (step is null)
            throw new DomainException($"Stage '{stageCode}' is not in this chain template.");

        _steps.Remove(step);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new StageChainStepRemovedEvent(Id, TenantId, stageCode, DateTimeOffset.UtcNow));
    }
}
