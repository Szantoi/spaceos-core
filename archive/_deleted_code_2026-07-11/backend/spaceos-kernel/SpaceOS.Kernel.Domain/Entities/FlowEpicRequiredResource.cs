namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents a resource required by a <see cref="FlowEpic"/> when its scope is
/// <see cref="Enums.FlowEpicScope.MicroAssembly"/>.
/// Owned by <see cref="FlowEpic"/>; has no independent lifecycle.
/// </summary>
public sealed class FlowEpicRequiredResource
{
    /// <summary>Gets the database-generated identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the type of resource required (e.g. "Machine", "Tool", "Material").</summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable name of the resource.</summary>
    public string ResourceName { get; private set; } = string.Empty;

    /// <summary>Gets the quantity of the resource required.</summary>
    public int Quantity { get; private set; }

    private FlowEpicRequiredResource() { }

    /// <summary>
    /// Creates a new <see cref="FlowEpicRequiredResource"/> instance.
    /// </summary>
    /// <param name="resourceType">The type of resource (e.g. "Machine", "Tool").</param>
    /// <param name="resourceName">The human-readable resource name.</param>
    /// <param name="quantity">The required quantity (must be &gt; 0).</param>
    /// <returns>A new resource instance.</returns>
    public static FlowEpicRequiredResource Create(string resourceType, string resourceName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
            throw new Domain.Exceptions.DomainException("ResourceType cannot be empty.");
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new Domain.Exceptions.DomainException("ResourceName cannot be empty.");
        if (quantity <= 0)
            throw new Domain.Exceptions.DomainException("Quantity must be greater than zero.");

        return new FlowEpicRequiredResource
        {
            Id = Guid.NewGuid(),
            ResourceType = resourceType,
            ResourceName = resourceName,
            Quantity = quantity
        };
    }
}
