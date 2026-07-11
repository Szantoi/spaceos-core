// SpaceOS.Kernel.Domain/Entities/StageHandoff.cs
using System;
using System.Security.Cryptography;
using System.Text;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Events;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Immutable data package representing a stage transition event for a <see cref="FlowEpic"/>.
/// Each handoff captures a payload, its SHA-256 hash, an idempotency key (SEC-05),
/// and an advisory-lock-based version number (DB-02).
/// </summary>
public sealed class StageHandoff : TenantScopedAggregateRoot
{
    /// <summary>Gets the identifier of the associated flow epic.</summary>
    public Guid FlowEpicId { get; private set; }

    /// <summary>Gets the source stage code.</summary>
    public string SourceStageCode { get; private set; } = string.Empty;

    /// <summary>Gets the target stage code.</summary>
    public string TargetStageCode { get; private set; } = string.Empty;

    /// <summary>Gets the version number of this handoff (advisory-lock sequential, DB-02).</summary>
    public int Version { get; private set; }

    /// <summary>Gets the idempotency key — unique per (FlowEpicId, key) pair (SEC-05).</summary>
    public Guid IdempotencyKey { get; private set; }

    /// <summary>Gets the JSON payload (max 1 MB, max depth 10 — DB-05, SEC-04).</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>Gets the hex-encoded SHA-256 hash of the payload (lowercase).</summary>
    public string PayloadHash { get; private set; } = string.Empty;

    /// <summary>Gets the hash algorithm identifier (SEC-07: future-proofing).</summary>
    public string HashAlgorithm { get; private set; } = string.Empty;

    /// <summary>Gets the optional source actor tenant identifier (DB-07).</summary>
    public Guid? SourceActorId { get; private set; }

    /// <summary>Gets the optional target actor tenant identifier (DB-07).</summary>
    public Guid? TargetActorId { get; private set; }

    /// <summary>Gets the optional B2B handshake identifier that triggered this handoff.</summary>
    public Guid? HandshakeId { get; private set; }

    /// <summary>Gets the UTC timestamp when this handoff was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core parameterless constructor
    private StageHandoff() { }

    /// <summary>
    /// Creates a new immutable <see cref="StageHandoff"/>.
    /// Raises <see cref="StageHandoffCreatedEvent"/>.
    /// </summary>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="flowEpicId">The identifier of the associated flow epic.</param>
    /// <param name="sourceStage">The source stage code.</param>
    /// <param name="targetStage">The target stage code.</param>
    /// <param name="nextVersion">The advisory-lock-computed next version number.</param>
    /// <param name="idempotencyKey">A unique key for idempotency checking (SEC-05).</param>
    /// <param name="payloadJson">The JSON payload (max 1 MB, max depth 10).</param>
    /// <param name="sourceActorId">Optional source actor tenant identifier (DB-07).</param>
    /// <param name="targetActorId">Optional target actor tenant identifier (DB-07).</param>
    /// <param name="handshakeId">Optional B2B handshake identifier.</param>
    /// <returns>A new <see cref="StageHandoff"/> instance.</returns>
    public static StageHandoff Create(
        Guid tenantId,
        Guid flowEpicId,
        string sourceStage,
        string targetStage,
        int nextVersion,
        Guid idempotencyKey,
        string payloadJson,
        Guid? sourceActorId,
        Guid? targetActorId,
        Guid? handshakeId = null)
    {
        var hash = ComputeHash(tenantId, flowEpicId, sourceStage, targetStage, payloadJson, nextVersion);

        var handoff = new StageHandoff
        {
            Id              = Guid.NewGuid(),
            TenantId        = tenantId,
            FlowEpicId      = flowEpicId,
            SourceStageCode = sourceStage,
            TargetStageCode = targetStage,
            Version         = nextVersion,
            IdempotencyKey  = idempotencyKey,
            PayloadJson     = payloadJson,
            PayloadHash     = hash,
            HashAlgorithm   = "SHA-256",
            SourceActorId   = sourceActorId,
            TargetActorId   = targetActorId,
            HandshakeId     = handshakeId,
            CreatedAt       = DateTimeOffset.UtcNow
        };
        handoff.AddDomainEvent(new StageHandoffCreatedEvent(
            handoff.Id, flowEpicId, sourceStage, targetStage, nextVersion, DateTimeOffset.UtcNow));
        return handoff;
    }

    private static string ComputeHash(
        Guid tenantId,
        Guid flowEpicId,
        string source,
        string target,
        string payload,
        int version)
    {
        var input = string.Join("|",
            tenantId.ToString("D"),
            flowEpicId.ToString("D"),
            source,
            target,
            version,
            payload);
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
