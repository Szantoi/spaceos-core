// SpaceOS.Infrastructure/Outbox/OutboxBackgroundWorker.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Outbox;

/// <summary>
/// Hosted background service that polls the outbox table every 5 seconds and processes
/// any <see cref="OutboxStatus.Pending"/> <see cref="OutboxMessage"/> records.
/// Each message is dispatched through <see cref="ISignalROutboxFanOut"/>, sunk to the
/// audit hash chain via <see cref="IHashChainOutboxSink"/>, and forwarded to cross-module
/// subscribers via <see cref="ICrossModuleOutboxDispatcher"/> before being marked
/// <see cref="OutboxStatus.Processed"/>. On failure the message is marked
/// <see cref="OutboxStatus.Failed"/> and <see cref="OutboxMessage.Attempts"/> is incremented.
/// </summary>
public sealed class OutboxBackgroundWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundWorker> _logger;

    /// <summary>
    /// Initialises a new <see cref="OutboxBackgroundWorker"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes per poll cycle.</param>
    /// <param name="logger">Structured logger.</param>
    public OutboxBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxBackgroundWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("OutboxBackgroundWorker started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful shutdown — do not log as error.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in OutboxBackgroundWorker poll cycle.");
            }

            await Task.Delay(PollInterval, ct).ConfigureAwait(false);
        }

        _logger.LogInformation("OutboxBackgroundWorker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var signalRFanOut           = scope.ServiceProvider.GetRequiredService<ISignalROutboxFanOut>();
        var hashChainSink           = scope.ServiceProvider.GetRequiredService<IHashChainOutboxSink>();
        var crossModuleDispatcher   = scope.ServiceProvider.GetRequiredService<ICrossModuleOutboxDispatcher>();

        var messages = await outboxRepository
            .GetPendingAsync(BatchSize, ct)
            .ConfigureAwait(false);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("OutboxBackgroundWorker processing {Count} message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "Processing outbox message {MessageId} of type {MessageType} for tenant {TenantId}.",
                    message.Id, message.Type, message.TenantId);

                await signalRFanOut.DispatchAsync(message, ct).ConfigureAwait(false);
                await hashChainSink.SinkAsync(message, ct).ConfigureAwait(false);
                await crossModuleDispatcher.DispatchAsync(message, ct).ConfigureAwait(false);

                message.MarkProcessed(DateTimeOffset.UtcNow);
                await outboxRepository.UpdateAsync(message, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId} of type {MessageType}.",
                    message.Id, message.Type);

                message.MarkFailed(ex.Message);
                await outboxRepository.UpdateAsync(message, ct).ConfigureAwait(false);
                // Continue to next message — do not let one failure block the batch.
            }
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
