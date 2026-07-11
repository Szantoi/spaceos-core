// SpaceOS.Kernel.Application/StageRegistry/StageHandoffDto.cs
using System;

namespace SpaceOS.Kernel.Application.StageRegistry;

/// <summary>Read model for a <see cref="Domain.Entities.StageHandoff"/>.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="TenantId">Owning tenant identifier.</param>
/// <param name="FlowEpicId">Associated flow epic identifier.</param>
/// <param name="SourceStageCode">Source stage code.</param>
/// <param name="TargetStageCode">Target stage code.</param>
/// <param name="Version">Sequential version number.</param>
/// <param name="IdempotencyKey">Client-supplied idempotency key.</param>
/// <param name="PayloadHash">Hex-encoded SHA-256 hash of the payload.</param>
/// <param name="HashAlgorithm">Algorithm used to hash the payload.</param>
/// <param name="SourceActorId">Optional source actor tenant identifier.</param>
/// <param name="TargetActorId">Optional target actor tenant identifier.</param>
/// <param name="HandshakeId">Optional B2B handshake identifier.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
public sealed record StageHandoffDto(
    Guid Id,
    Guid TenantId,
    Guid FlowEpicId,
    string SourceStageCode,
    string TargetStageCode,
    int Version,
    Guid IdempotencyKey,
    string PayloadHash,
    string HashAlgorithm,
    Guid? SourceActorId,
    Guid? TargetActorId,
    Guid? HandshakeId,
    DateTimeOffset CreatedAt);
