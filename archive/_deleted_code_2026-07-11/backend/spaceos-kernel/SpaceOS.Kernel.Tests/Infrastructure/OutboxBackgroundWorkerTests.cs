// SpaceOS.Kernel.Tests/Infrastructure/OutboxBackgroundWorkerTests.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Infrastructure.Outbox;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="OutboxBackgroundWorker"/> — verifies Status-based polling,
/// fan-out dispatch, and failure handling.
/// </summary>
public sealed class OutboxBackgroundWorkerTests
{
    private readonly Mock<IOutboxRepository>           _outboxRepo          = new();
    private readonly Mock<IUnitOfWork>                 _unitOfWork          = new();
    private readonly Mock<ISignalROutboxFanOut>        _fanOut              = new();
    private readonly Mock<IHashChainOutboxSink>        _hashSink            = new();
    private readonly Mock<ICrossModuleOutboxDispatcher> _crossModuleDispatcher = new();

    private OutboxBackgroundWorker BuildWorker()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_outboxRepo.Object);
        services.AddSingleton(_unitOfWork.Object);
        services.AddSingleton(_fanOut.Object);
        services.AddSingleton(_hashSink.Object);
        services.AddSingleton(_crossModuleDispatcher.Object);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new OutboxBackgroundWorker(scopeFactory, NullLogger<OutboxBackgroundWorker>.Instance);
    }

    /// <summary>
    /// Starts the worker, waits for <see cref="IUnitOfWork.SaveChangesAsync"/> to be invoked
    /// (indicating one batch completed), then stops the worker.
    /// </summary>
    private async Task RunOneBatchAsync(TimeSpan timeout)
    {
        var batchDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => batchDone.TrySetResult())
            .Returns(Task.CompletedTask);

        var worker = BuildWorker();
        await worker.StartAsync(CancellationToken.None);
        await batchDone.Task.WaitAsync(timeout);
        await worker.StopAsync(CancellationToken.None);
    }

    // ── 1. Pending messages are dispatched through fan-out ────────────────────

    [Fact]
    public async Task ProcessBatch_PendingMessage_CallsFanOut()
    {
        var message = OutboxMessage.Create("TestEvent", "{}", Guid.NewGuid());

        _outboxRepo
            .SetupSequence(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message })
            .ReturnsAsync(new List<OutboxMessage>());

        await RunOneBatchAsync(TimeSpan.FromSeconds(5));

        _fanOut.Verify(f => f.DispatchAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 2. Successful dispatch → MarkProcessed called (Status=Processed) ──────

    [Fact]
    public async Task ProcessBatch_SuccessfulDispatch_SetsStatusProcessed()
    {
        var message = OutboxMessage.Create("TestEvent", "{}", Guid.NewGuid());

        _outboxRepo
            .SetupSequence(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message })
            .ReturnsAsync(new List<OutboxMessage>());

        await RunOneBatchAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(OutboxStatus.Processed, message.Status);
    }

    // ── 3. Fan-out failure → MarkFailed called, Attempts incremented ──────────

    [Fact]
    public async Task ProcessBatch_FanOutThrows_SetsStatusFailedAndIncrementsAttempts()
    {
        var message = OutboxMessage.Create("TestEvent", "{}", Guid.NewGuid());

        _outboxRepo
            .SetupSequence(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message })
            .ReturnsAsync(new List<OutboxMessage>());

        _fanOut
            .Setup(f => f.DispatchAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("hub unavailable"));

        await RunOneBatchAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(OutboxStatus.Failed, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("hub unavailable", message.LastError);
    }

    // ── 4. Empty batch → UpdateAsync never called ─────────────────────────────

    [Fact]
    public async Task ProcessBatch_NoPendingMessages_DoesNotCallUpdate()
    {
        _outboxRepo
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage>());

        var worker = BuildWorker();
        await worker.StartAsync(CancellationToken.None);
        // Give the worker one poll cycle, then stop.
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await worker.StopAsync(CancellationToken.None);

        _outboxRepo.Verify(
            r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
