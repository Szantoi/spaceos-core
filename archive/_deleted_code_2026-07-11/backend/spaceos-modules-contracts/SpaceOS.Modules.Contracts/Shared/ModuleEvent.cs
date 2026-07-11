namespace SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Base record for all module events. EventId is auto-generated on construction.
/// CONSUMER MUST verify that TenantId matches the current JWT TenantId claim (SEC-03).
/// </summary>
public abstract record ModuleEvent
{
    /// <summary>Gets the unique identifier for this event instance, auto-generated on construction.</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>Gets the tenant that owns this event. Must match the JWT TenantId claim (SEC-03).</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the UTC timestamp when this event occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
