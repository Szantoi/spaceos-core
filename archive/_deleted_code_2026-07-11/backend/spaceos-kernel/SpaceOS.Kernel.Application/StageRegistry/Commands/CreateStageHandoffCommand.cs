// SpaceOS.Kernel.Application/StageRegistry/Commands/CreateStageHandoffCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>
/// Creates an immutable <see cref="Domain.Entities.StageHandoff"/> data package for a stage transition.
/// The handler applies <c>pg_advisory_xact_lock</c> for version serialisation (DB-02) and uses
/// an idempotency key to prevent duplicate processing (SEC-05).
/// </summary>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="FlowEpicId">The identifier of the associated flow epic.</param>
/// <param name="SourceStageCode">The source stage code.</param>
/// <param name="TargetStageCode">The target stage code.</param>
/// <param name="IdempotencyKey">A client-supplied UUID for idempotent replay protection (SEC-05).</param>
/// <param name="PayloadJson">The JSON payload (max 1 MB, max depth 10 — DB-05 / SEC-04).</param>
/// <param name="SourceActorId">Optional source actor tenant identifier (DB-07).</param>
/// <param name="TargetActorId">Optional target actor tenant identifier (DB-07).</param>
/// <param name="HandshakeId">Optional B2B handshake identifier.</param>
public sealed record CreateStageHandoffCommand(
    Guid TenantId,
    Guid FlowEpicId,
    string SourceStageCode,
    string TargetStageCode,
    Guid IdempotencyKey,
    string PayloadJson,
    Guid? SourceActorId = null,
    Guid? TargetActorId = null,
    Guid? HandshakeId = null) : IRequest<Result<Guid>>;
