using System.Security;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Sales.Abstractions.Contracts;
using SpaceOS.Modules.Sales.Abstractions.Ports;
using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Infrastructure.Outbox;

/// <summary>
/// Background service that polls the outbox and drives the Joinery order conversion flow.
/// Three-phase protocol (ADR-039):
///   Phase 1 — CLAIM: mark messages InFlight in a short transaction (advisory-lock-safe).
///   Phase 2 — PROCESS: HTTP call to Joinery outside any DB transaction.
///   Phase 3 — COMPLETE: update Quote FSM and mark message Completed/Failed.
/// SEC-S-10: only the error type is logged, never the payload or ex.Message.
/// </summary>
public sealed class SalesIntegrationWorker(
    IServiceProvider sp,
    ISalesWorkerDbContextFactory dbFactory,
    IClock clock,
    ILogger<SalesIntegrationWorker> log) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int MaxAttempts = 3;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stop).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Outbox poll batch failed");
            }

            await Task.Delay(PollInterval, stop).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateAsync(ct).ConfigureAwait(false);
        await using var scope = sp.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<IOrderConversionPort>();

        // ── Phase 1: CLAIM ───────────────────────────────────────────────────
        // Short transaction — FOR UPDATE SKIP LOCKED ensures only one worker claims each row.
        List<Guid> claimedIds;
        await using (var claimTx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false))
        {
            var batch = await db.OutboxMessages
                .FromSqlRaw(@"
                    SELECT * FROM spaceos_sales.sales_outbox
                    WHERE ""Status"" IN ('Pending','InFlight')
                      AND ""NextAttemptAt"" <= NOW()
                    ORDER BY ""NextAttemptAt"" ASC
                    FOR UPDATE SKIP LOCKED
                    LIMIT 10")
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var m in batch) m.MarkInFlight(clock, leaseSeconds: 60);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await claimTx.CommitAsync(ct).ConfigureAwait(false);
            claimedIds = batch.Select(m => m.Id).ToList();
        }

        // ── Phase 2 + 3: PROCESS then COMPLETE (per message) ─────────────────
        foreach (var msgId in claimedIds)
        {
            var msg = await db.OutboxMessages
                .SingleAsync(m => m.Id == msgId, ct)
                .ConfigureAwait(false);

            try
            {
                var req = JsonSerializer.Deserialize<OrderConversionRequest>(msg.PayloadJson)!;

                // HTTP call — no DB transaction held during I/O
                var result = await port.CreateOrderFromQuoteAsync(req, ct).ConfigureAwait(false);

                await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

                // OPEN-04: set per-message tenant scope
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT set_config('app.current_tenant_id', {0}, true)",
                    [msg.TenantId.ToString()],
                    ct).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    var quote = await db.Quotes
                        .SingleAsync(q => q.Id == msg.AggregateId, ct)
                        .ConfigureAwait(false);

                    // SEC-S-03: explicit tenant guard (defence in depth on top of RLS)
                    if (quote.TenantId != msg.TenantId)
                        throw new SecurityException(
                            $"Tenant mismatch on outbox message {msg.Id}.");

                    var fsm = quote.CompleteConversion(result.Value.OrderId, clock);
                    if (!fsm.IsSuccess)
                        throw new InvalidOperationException(
                            fsm.ValidationErrors.FirstOrDefault()?.ErrorMessage ?? "FSM transition failed.");

                    msg.MarkCompleted(clock);
                }
                else
                {
                    msg.RecordFailure("conversion-rejected", clock, MaxAttempts);
                }

                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // SEC-S-10: log error type only — no payload, no ex.Message (PII risk)
                log.LogWarning(
                    "Outbox {MessageId} failed (attempt {Attempt}, errorType {ErrorType})",
                    msg.Id, msg.AttemptCount, ex.GetType().Name);

                msg.RecordFailure(ex.GetType().Name, clock, MaxAttempts);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
