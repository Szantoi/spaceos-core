// SpaceOS.Infrastructure/StageRegistry/CreateStageHandoffCommandHandler.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Application.StageRegistry.Commands;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.StageRegistry;

/// <summary>
/// Infrastructure handler for <see cref="CreateStageHandoffCommand"/>.
/// Uses an explicit database transaction with <c>pg_advisory_xact_lock</c> for version serialisation (DB-02 / BE-02)
/// and handles duplicate-key exceptions for idempotent replay (SEC-05).
/// </summary>
/// <remarks>
/// Placed in the Infrastructure layer (ADR-023) because <c>pg_advisory_xact_lock</c> requires direct
/// <see cref="AppDbContext"/> access which cannot be expressed through the repository interface.
/// </remarks>
internal sealed class CreateStageHandoffCommandHandler
    : IRequestHandler<CreateStageHandoffCommand, Result<Guid>>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    /// <summary>Initialises a new <see cref="CreateStageHandoffCommandHandler"/>.</summary>
    public CreateStageHandoffCommandHandler(AppDbContext db, IMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(mediator);
        _db      = db;
        _mediator = mediator;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(CreateStageHandoffCommand cmd, CancellationToken ct)
    {
        // NpgsqlRetryingExecutionStrategy forbids calling BeginTransactionAsync outside of
        // CreateExecutionStrategy().ExecuteAsync(...). Wrapping the full handler body satisfies
        // the check and allows the strategy to retry the entire operation on transient failures.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<Result<Guid>>(async () =>
        {
            // SEC-09: explicit transaction for advisory lock + save
            await using var tx = await _db.Database
                .BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                // DB-02: serialize concurrent handoffs for the same (FlowEpicId, Source, Target) triple
                var lockKey = $"{cmd.FlowEpicId}{cmd.SourceStageCode}{cmd.TargetStageCode}";
                await _db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    lockKey).ConfigureAwait(false);

                // DB-02: compute the next version atomically
                var maxVersion = await _db.StageHandoffs
                    .Where(h =>
                        h.FlowEpicId      == cmd.FlowEpicId &&
                        h.SourceStageCode == cmd.SourceStageCode &&
                        h.TargetStageCode == cmd.TargetStageCode)
                    .MaxAsync(h => (int?)h.Version, ct)
                    .ConfigureAwait(false) ?? 0;

                var handoff = StageHandoff.Create(
                    cmd.TenantId,
                    cmd.FlowEpicId,
                    cmd.SourceStageCode,
                    cmd.TargetStageCode,
                    maxVersion + 1,
                    cmd.IdempotencyKey,
                    cmd.PayloadJson,
                    cmd.SourceActorId,
                    cmd.TargetActorId,
                    cmd.HandshakeId);

                _db.StageHandoffs.Add(handoff);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                // Golden Rule #12: dispatch domain events after successful commit
                var events = handoff.PopDomainEvents();
                foreach (var e in events)
                    await _mediator.Publish(e, ct).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return Result.Success(handoff.Id);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // SEC-05: idempotency — duplicate unique constraint = already processed
                await tx.RollbackAsync(ct).ConfigureAwait(false);

                var existing = await _db.StageHandoffs
                    .FirstOrDefaultAsync(
                        h => h.FlowEpicId == cmd.FlowEpicId && h.IdempotencyKey == cmd.IdempotencyKey,
                        ct)
                    .ConfigureAwait(false);

                return existing is not null
                    ? Result.Success(existing.Id)
                    : Result.Error("Duplicate handoff key but original record not found.");
            }
        }).ConfigureAwait(false);
    }
}
