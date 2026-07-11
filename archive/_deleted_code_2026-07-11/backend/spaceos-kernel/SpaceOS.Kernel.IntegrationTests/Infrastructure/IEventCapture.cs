// SpaceOS.Kernel.IntegrationTests/Infrastructure/IEventCapture.cs
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Provides read access to domain events captured during a test run.
/// Implemented by <see cref="EventCaptureService"/> and exposed on
/// <see cref="SpaceOsApiFactory.Capture"/> for test assertions.
/// </summary>
public interface IEventCapture
{
    /// <summary>Gets a snapshot of every <see cref="IDomainEvent"/> captured since the last reset.</summary>
    IReadOnlyList<IDomainEvent> Events { get; }

    /// <summary>Records a single domain event into the internal buffer.</summary>
    /// <param name="domainEvent">The event to capture.</param>
    void Capture(IDomainEvent domainEvent);

    /// <summary>Clears all previously captured events.</summary>
    void Reset();
}
