// SpaceOS.Kernel.IntegrationTests/Infrastructure/EventCaptureService.cs
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Primitives;
using System.Collections.Concurrent;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Singleton test-only service that fulfils both <see cref="IEventCapture"/> and
/// <see cref="IDomainEventDispatcher"/>. When the application dispatches domain events
/// this service records each event and then publishes it through MediatR so that
/// production notification handlers still execute during integration tests.
/// </summary>
/// <remarks>
/// Registered as a <c>Singleton</c> in <see cref="SpaceOsApiFactory"/> — the singleton
/// lifetime allows captured events to be read by the test after the HTTP request scope
/// has been disposed. MediatR's <see cref="IPublisher"/> is <c>Scoped</c>, so a fresh
/// <see cref="IServiceScope"/> is created per <see cref="DispatchAsync"/> invocation.
/// </remarks>
public sealed class EventCaptureService : IEventCapture, IDomainEventDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentBag<IDomainEvent> _events = new();

    /// <summary>
    /// Initialises a new <see cref="EventCaptureService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory used to resolve <see cref="IPublisher"/> per dispatch call.</param>
    public EventCaptureService(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> Events => _events.ToList().AsReadOnly();

    /// <inheritdoc/>
    public void Capture(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _events.Add(domainEvent);
    }

    /// <inheritdoc/>
    public void Reset() => _events.Clear();

    /// <summary>
    /// Records each event via <see cref="Capture"/> and then publishes it through
    /// the MediatR pipeline so registered <see cref="INotificationHandler{TNotification}"/>
    /// implementations still run.
    /// </summary>
    /// <param name="domainEvents">The events to capture and dispatch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        var eventList = domainEvents.ToList();

        foreach (var domainEvent in eventList)
            Capture(domainEvent);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var tasks = eventList.Select(e => publisher.Publish(e, ct)).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
