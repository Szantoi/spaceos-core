// SpaceOS.Infrastructure/Sync/OfflineQueuePurgeWorker.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Abstractions.Sync;
using SpaceOS.Modules.FlowManagement.Infrastructure;

namespace SpaceOS.Infrastructure.Sync;

/// <summary>
/// Background service that periodically purges expired items from the offline sync
/// queue. Items are considered expired when their <c>ExpiresAt</c> timestamp is in
/// the past. The retention window is governed by
/// <see cref="SyncConstants.OfflineQueueTtlDays"/>.
/// </summary>
/// <remarks>
/// The worker runs once per hour. It resolves a fresh DI scope on each iteration so
/// that the <see cref="ModulesDbContext"/> lifetime matches scoped usage expectations.
/// </remarks>
internal sealed class OfflineQueuePurgeWorker : BackgroundService
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfflineQueuePurgeWorker> _logger;

    /// <summary>
    /// Initialises the worker with the required scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes per purge cycle.</param>
    /// <param name="logger">Structured logger for purge activity.</param>
    public OfflineQueuePurgeWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OfflineQueuePurgeWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await PurgeExpiredAsync(ct).ConfigureAwait(false);
            await Task.Delay(PurgeInterval, ct).ConfigureAwait(false);
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ModulesDbContext>();
            var cutoff = DateTimeOffset.UtcNow;

            var deleted = await db.Database
                .ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM modules.\"OfflineSyncQueue\" WHERE \"ExpiresAt\" < {cutoff}", ct)
                .ConfigureAwait(false);

            if (deleted > 0)
                _logger.LogInformation(
                    "Purged {Count} expired offline sync queue items (cutoff: {Cutoff:O}).",
                    deleted, cutoff);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error purging expired offline sync queue items.");
        }
    }
}
