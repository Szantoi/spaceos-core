// SpaceOS.Infrastructure/AuditLog/ChanneledAuditEventDispatcher.cs

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Infrastructure decorator around <see cref="IAuditEventDispatcher"/> that enqueues domain events
/// into a bounded <see cref="Channel{T}"/> before forwarding them to the inner dispatcher.
/// </summary>
/// <remarks>
/// <para>
/// <strong>SEC-01 fix:</strong> The channel uses <see cref="BoundedChannelFullMode.Wait"/> so
/// that back-pressure is applied to the caller when the buffer is full, preventing silent event loss.
/// If the wait exceeds 5 seconds a <c>LogCritical</c> entry is written and the event is discarded
/// rather than blocking the request indefinitely.
/// </para>
/// <para>
/// <strong>Graceful drain (B3):</strong> <see cref="DisposeAsync"/> completes the channel writer
/// and waits up to 30 seconds for the background reader to drain remaining items. A warning is
/// logged if the drain times out.
/// </para>
/// </remarks>
public sealed class ChanneledAuditEventDispatcher : IAuditEventDispatcher, IAsyncDisposable
{
    private readonly Channel<IReadOnlyList<IDomainEvent>> _channel;
    private readonly IAuditEventDispatcher _inner;
    private readonly ILogger<ChanneledAuditEventDispatcher> _logger;
    private readonly Task _processingTask;

    /// <summary>
    /// Initialises a new <see cref="ChanneledAuditEventDispatcher"/> with a bounded buffer of 512 event batches.
    /// </summary>
    /// <param name="inner">The inner dispatcher that performs the actual audit write.</param>
    /// <param name="logger">Logger for overflow and drain diagnostics.</param>
    public ChanneledAuditEventDispatcher(
        IAuditEventDispatcher inner,
        ILogger<ChanneledAuditEventDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner  = inner;
        _logger = logger;

        _channel = Channel.CreateBounded<IReadOnlyList<IDomainEvent>>(new BoundedChannelOptions(512)
        {
            FullMode     = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });

        _processingTask = Task.Run(ProcessAsync);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Attempts to write the event batch to the channel with a 5-second timeout.
    /// If the channel is full and does not drain within 5 seconds, logs a critical warning
    /// and drops the batch rather than blocking the calling request indefinitely.
    /// </remarks>
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0)
            return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await _channel.Writer.WriteAsync(events, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Channel was full for > 5 s — log and drop to avoid blocking the caller.
            _logger.LogCritical(
                "ChanneledAuditEventDispatcher: channel full for 5 s — dropping {Count} event(s). " +
                "Audit events may be lost. Investigate sink throughput immediately.",
                events.Count);
        }
    }

    /// <summary>
    /// Signals the channel writer to complete and waits up to 30 seconds for the reader to drain.
    /// Logs a warning if the drain times out.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await _processingTask.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "ChanneledAuditEventDispatcher: audit drain timeout after 30 s — some events may not be persisted.");
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task ProcessAsync()
    {
        await foreach (var batch in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                await _inner.DispatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ChanneledAuditEventDispatcher: inner dispatcher threw for batch of {Count} event(s).",
                    batch.Count);
            }
        }
    }
}
