// SpaceOS.Kernel.Application/Snapshots/Dtos/FlowEpicSnapshotDto.cs

namespace SpaceOS.Kernel.Application.Snapshots.Dtos;

/// <summary>
/// Snapshot DTO for a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> aggregate.
/// Used by <c>ISnapshotable.ToSnapshotJson()</c> — never serialise the aggregate directly
/// (private setters produce empty JSON).
/// </summary>
/// <param name="EpicId">The unique identifier of the epic.</param>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="Title">The epic title at snapshot time.</param>
/// <param name="Phase">The workflow phase string (e.g. "Discovery", "Delivery", "ClosedDone").</param>
/// <param name="IsArchived">Whether the epic was archived at snapshot time.</param>
/// <param name="ProofUrl">The proof URL, or <see langword="null"/> if not yet closed.</param>
/// <param name="ProofHash">The proof hash, or <see langword="null"/> if not yet closed.</param>
/// <param name="HandshakeGuestTenantId">The guest tenant from the B2B handshake, or <see langword="null"/>.</param>
/// <param name="TargetFacilityId">The target facility identifier.</param>
/// <param name="SnapshotFormatVersion">Monotonically increasing schema version of this DTO format.</param>
public sealed record FlowEpicSnapshotDto(
    Guid    EpicId,
    Guid    TenantId,
    string  Title,
    string  Phase,
    bool    IsArchived,
    string? ProofUrl,
    string? ProofHash,
    Guid?   HandshakeGuestTenantId,
    Guid    TargetFacilityId,
    int     SnapshotFormatVersion = 1);
