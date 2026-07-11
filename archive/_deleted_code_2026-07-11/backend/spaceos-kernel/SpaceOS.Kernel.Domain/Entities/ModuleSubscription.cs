// SpaceOS.Kernel.Domain/Entities/ModuleSubscription.cs

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents a system-level subscription for cross-module event routing.
/// A subscription binds a subscriber module name and an event type to an HTTP inbox endpoint
/// that will receive dispatched outbox messages. This entity is NOT tenant-scoped.
/// </summary>
public sealed class ModuleSubscription
{
    /// <summary>Gets the unique identifier of this subscription.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the logical name of the subscribing module (e.g. <c>"Manufacturing"</c>).
    /// Maximum length: 100 characters.
    /// </summary>
    public string SubscriberModule { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the domain event type discriminator this subscription listens to
    /// (e.g. <c>"CuttingPanelCompleted"</c>). Maximum length: 200 characters.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the internal HTTP endpoint that receives dispatched messages
    /// (e.g. <c>"http://127.0.0.1:5007/internal/inbox/cutting"</c>). Maximum length: 500 characters.
    /// </summary>
    public string InboxEndpoint { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this subscription is currently active.
    /// Inactive subscriptions are skipped during dispatch.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the UTC timestamp when this subscription was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core parameterless constructor
    private ModuleSubscription() { }

    /// <summary>
    /// Creates a new active <see cref="ModuleSubscription"/>.
    /// </summary>
    /// <param name="subscriberModule">The logical name of the subscribing module. Must not be null or whitespace. Maximum 100 characters.</param>
    /// <param name="eventType">The event type discriminator to subscribe to. Must not be null or whitespace. Maximum 200 characters.</param>
    /// <param name="inboxEndpoint">The HTTP endpoint that will receive dispatched messages. Must not be null or whitespace. Maximum 500 characters.</param>
    /// <returns>A new <see cref="ModuleSubscription"/> with <see cref="IsActive"/> set to <see langword="true"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when any required argument is null, empty, or whitespace.</exception>
    public static ModuleSubscription Create(
        string subscriberModule,
        string eventType,
        string inboxEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(inboxEndpoint);

        return new ModuleSubscription
        {
            Id               = Guid.NewGuid(),
            SubscriberModule = subscriberModule,
            EventType        = eventType,
            InboxEndpoint    = inboxEndpoint,
            IsActive         = true,
            CreatedAt        = DateTimeOffset.UtcNow,
        };
    }
}
