using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Outbox;

/// <summary>
/// Background service that polls the outbox table and dispatches pending entries
/// to the Orchestrator. Uses FOR UPDATE SKIP LOCKED (SEC-02) to prevent duplicate
/// processing across multiple worker instances.
/// </summary>
public sealed class JoineryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JoineryOutboxWorker> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    public JoineryOutboxWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<JoineryOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("JoineryOutboxWorker started");

        while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
                var client = scope.ServiceProvider.GetRequiredService<IOrchestratorClient>();
                var clock = scope.ServiceProvider.GetRequiredService<IClock>();

                // SEC-02: FOR UPDATE SKIP LOCKED prevents duplicate processing across instances
                var pending = await db.JoineryOutboxEntries
                    .FromSqlRaw(@"
                        SELECT * FROM spaceos_joinery.""JoineryOutboxEntries""
                        WHERE ""ProcessedAt"" IS NULL
                          AND ""FailedAt"" IS NULL
                          AND ""RetryCount"" < 3
                        ORDER BY ""CreatedAt""
                        LIMIT 10
                        FOR UPDATE SKIP LOCKED")
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var entry in pending)
                {
                    try
                    {
                        var result = await client.CalculateAsync(entry, ct).ConfigureAwait(false);
                        if (result.IsSuccess)
                            entry.MarkProcessed(clock.UtcNow);
                        else
                            entry.IncrementRetry(result.Errors.First(), clock.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        var truncatedMessage = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                        entry.IncrementRetry(truncatedMessage, clock.UtcNow);
                    }
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxWorker tick failed");
            }
        }
    }
}
