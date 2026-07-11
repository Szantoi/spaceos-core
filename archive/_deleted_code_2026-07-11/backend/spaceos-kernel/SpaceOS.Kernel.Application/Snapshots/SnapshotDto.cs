// SpaceOS.Kernel.Application/Snapshots/SnapshotDto.cs

namespace SpaceOS.Kernel.Application.Snapshots;

/// <summary>
/// Read-only projection of an <see cref="SpaceOS.Kernel.Domain.Snapshots.AggregateSnapshot"/> for API consumers.
/// </summary>
/// <param name="Id">The unique identifier of the snapshot record.</param>
/// <param name="AggregateId">The identifier of the aggregate this snapshot belongs to.</param>
/// <param name="AggregateType">The CLR type name of the aggregate.</param>
/// <param name="Version">The aggregate version this snapshot represents.</param>
/// <param name="SnapshotAt">The UTC timestamp when this snapshot was taken.</param>
/// <param name="StateJson">The JSON-serialised state of the aggregate at this version.</param>
/// <param name="SnapshotHash">The SHA-256 hex hash of <see cref="StateJson"/> for tamper detection.</param>
/// <param name="TenantId">The identifier of the tenant that owns the aggregate.</param>
public sealed record SnapshotDto(
    Guid Id,
    Guid AggregateId,
    string AggregateType,
    int Version,
    DateTimeOffset SnapshotAt,
    string StateJson,
    string SnapshotHash,
    Guid TenantId);
