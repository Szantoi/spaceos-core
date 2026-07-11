// Identity.Infrastructure/Workers/KcSyncWorkerService.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Workers;

public sealed class KcSyncWorkerService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KcSyncWorkerService> _logger;

    public KcSyncWorkerService(IServiceScopeFactory scopeFactory, ILogger<KcSyncWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KcSyncWorkerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<KcSyncProcessor>();
                await processor.ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in KcSyncWorkerService poll cycle.");
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("KcSyncWorkerService stopped.");
    }
}
