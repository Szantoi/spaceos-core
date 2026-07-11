// SpaceOS.Kernel.Domain/Common/ISnapshotable.cs

namespace SpaceOS.Kernel.Domain.Common;

/// <summary>
/// Marks an aggregate as capable of producing a JSON snapshot of its current state.
/// Implementations must use an explicit DTO serialisation path — never
/// <c>JsonSerializer.Serialize(aggregate)</c> directly, because private setters on
/// domain objects produce empty JSON.
/// </summary>
public interface ISnapshotable
{
    /// <summary>
    /// Returns the JSON representation of the aggregate's current state.
    /// Implementations should serialise a dedicated snapshot DTO, not the aggregate itself.
    /// </summary>
    /// <returns>A non-empty JSON string representing the current aggregate state.</returns>
    string ToSnapshotJson();
}
