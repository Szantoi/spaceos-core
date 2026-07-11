using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Outbox;

/// <summary>
/// Background service that runs hourly and deletes outbox entries processed
/// more than 7 days ago to keep the table from growing unbounded.
/// </summary>
public sealed class JoineryOutboxCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JoineryOutboxCleanupJob> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    public JoineryOutboxCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<JoineryOutboxCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();

                await db.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM ""JoineryOutboxEntries"" WHERE ""ProcessedAt"" < NOW() - INTERVAL '7 days'",
                    ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxCleanupJob failed");
            }
        }
    }
}
