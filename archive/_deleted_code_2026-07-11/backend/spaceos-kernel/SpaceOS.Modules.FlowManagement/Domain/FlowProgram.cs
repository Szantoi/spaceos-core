// SpaceOS.Modules.FlowManagement/Domain/FlowProgram.cs
using SpaceOS.Modules.Abstractions;

namespace SpaceOS.Modules.FlowManagement.Domain;

/// <summary>
/// Represents a high-level program that groups related <see cref="FlowProject"/> instances.
/// A program is the top-level container in the FlowManagement hierarchy.
/// </summary>
public sealed class FlowProgram : IFlowNode
{
    /// <summary>Gets the unique identifier of this program.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the display name of the program.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the tenant that owns this program.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets an optional description of the program.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the current workflow phase of this program.</summary>
    public WorkflowPhase Phase { get; private set; } = WorkflowPhase.Discovery;

    private FlowProgram() { }

    /// <summary>
    /// Creates a new <see cref="FlowProgram"/> with the given name and tenant.
    /// </summary>
    /// <param name="name">The display name of the program. Must not be null or whitespace.</param>
    /// <param name="tenantId">The tenant that owns this program.</param>
    /// <param name="description">An optional description of the program.</param>
    /// <returns>A new <see cref="FlowProgram"/> instance with phase Discovery.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public static FlowProgram Create(string name, Guid tenantId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Program name cannot be empty.", nameof(name));

        return new FlowProgram
        {
            Id          = Guid.NewGuid(),
            Name        = name,
            TenantId    = tenantId,
            Description = description,
            Phase       = WorkflowPhase.Discovery,
        };
    }
}
