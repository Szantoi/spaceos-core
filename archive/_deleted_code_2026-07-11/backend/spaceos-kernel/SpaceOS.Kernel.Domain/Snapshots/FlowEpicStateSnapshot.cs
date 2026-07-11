// SpaceOS.Kernel.Domain/Snapshots/FlowEpicStateSnapshot.cs

namespace SpaceOS.Kernel.Domain.Snapshots;

/// <summary>
/// Immutable snapshot DTO capturing the full observable state of a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>
/// at a point in time.
/// Used by <c>FlowEpic.ToSnapshotDto()</c> so that <c>JsonSerializer.Serialize</c> sees
/// public properties rather than private-setter domain fields (BE-P3B-01).
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
public sealed record FlowEpicStateSnapshot(
    Guid    EpicId,
    Guid    TenantId,
    string  Title,
    string  Phase,
    bool    IsArchived,
    string? ProofUrl,
    string? ProofHash,
    Guid?   HandshakeGuestTenantId,
    Guid    TargetFacilityId,
    string? Scope,
    string? RequiredSkillLevel,
    int     SnapshotFormatVersion = 2);
