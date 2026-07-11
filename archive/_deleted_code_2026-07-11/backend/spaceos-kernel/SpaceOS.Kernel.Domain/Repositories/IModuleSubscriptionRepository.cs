// SpaceOS.Kernel.Domain/Repositories/IModuleSubscriptionRepository.cs

using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence interface for <see cref="ModuleSubscription"/> entities.
/// </summary>
public interface IModuleSubscriptionRepository
{
    /// <summary>
    /// Adds a new <see cref="ModuleSubscription"/> to the store.
    /// </summary>
    /// <param name="subscription">The subscription to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(ModuleSubscription subscription, CancellationToken ct = default);

    /// <summary>
    /// Returns all active subscriptions for the given <paramref name="eventType"/>.
    /// </summary>
    /// <param name="eventType">The event type discriminator to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching active subscriptions; empty when none are registered.</returns>
    Task<IReadOnlyList<ModuleSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the subscription for the given <paramref name="subscriberModule"/> and
    /// <paramref name="eventType"/> combination, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="subscriberModule">The logical name of the subscribing module.</param>
    /// <param name="eventType">The event type discriminator.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ModuleSubscription?> GetBySubscriberAndEventAsync(
        string subscriberModule,
        string eventType,
        CancellationToken ct = default);
}
