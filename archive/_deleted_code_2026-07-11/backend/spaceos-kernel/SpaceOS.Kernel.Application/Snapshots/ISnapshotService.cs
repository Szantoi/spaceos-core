// SpaceOS.Kernel.Application/Snapshots/ISnapshotService.cs

using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Application.Snapshots;

/// <summary>
/// Application service for creating <c>AggregateSnapshot</c> records.
/// Internal to the Application layer — not exposed to the API layer.
/// </summary>
internal interface ISnapshotService
{
    /// <summary>
    /// Takes a point-in-time snapshot of the given aggregate and persists it.
    /// Delegates serialisation to <see cref="ISnapshotable.ToSnapshotJson"/> so that
    /// private-setter domain fields are captured via an explicit DTO (BE-P3B-01).
    /// </summary>
    /// <typeparam name="T">The aggregate root type. Must implement <see cref="ISnapshotable"/>.</typeparam>
    /// <param name="aggregate">The aggregate to snapshot.</param>
    /// <param name="type">The aggregate type discriminator stored alongside the snapshot.</param>
    /// <param name="triggerEventId">
    /// The identifier of the domain event that triggered this snapshot, or
    /// <see langword="null"/> if the snapshot was taken without a direct trigger.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task TakeSnapshotAsync<T>(
        T aggregate,
        AggregateType type,
        Guid? triggerEventId,
        CancellationToken ct)
        where T : AggregateRoot, ISnapshotable;
}
