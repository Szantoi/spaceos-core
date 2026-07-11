// SpaceOS.Kernel.Tests/Application/FlowEpicClosedEventHandlerTests.cs

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Kernel.Application.FlowEpics.Events;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="FlowEpicClosedEventHandler"/>.</summary>
public sealed class FlowEpicClosedEventHandlerTests
{
    private readonly Mock<IOutboxRepository> _outboxRepository = new();
    private readonly FlowEpicClosedEventHandler _sut;

    private static readonly TenantId   SomeTenantId = TenantId.From(Guid.NewGuid());
    private static readonly FlowEpicId SomeEpicId   = FlowEpicId.New();

    public FlowEpicClosedEventHandlerTests()
    {
        _sut = new FlowEpicClosedEventHandler(
            _outboxRepository.Object,
            NullLogger<FlowEpicClosedEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidEvent_CallsAddAsync_Once()
    {
        // Arrange
        var notification = new FlowEpicClosedEvent(
            SomeEpicId, SomeTenantId, "abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234",
            OccurredOn: DateTimeOffset.UtcNow);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _outboxRepository.Verify(
            r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidEvent_OutboxMessageHasCorrectEventType()
    {
        // Arrange
        OutboxMessage? captured = null;
        _outboxRepository
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var notification = new FlowEpicClosedEvent(
            SomeEpicId, SomeTenantId, "aaaa",
            OccurredOn: DateTimeOffset.UtcNow);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("FlowEpicClosedDone", captured!.Type);
    }

    [Fact]
    public async Task Handle_ValidEvent_OutboxMessageHasCorrectTenantId()
    {
        // Arrange
        OutboxMessage? captured = null;
        _outboxRepository
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var notification = new FlowEpicClosedEvent(
            SomeEpicId, SomeTenantId, "aaaa",
            OccurredOn: DateTimeOffset.UtcNow);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(SomeTenantId.Value, captured!.TenantId);
    }

    [Fact]
    public async Task Handle_ValidEvent_OutboxMessagePayloadContainsEpicId()
    {
        // Arrange
        OutboxMessage? captured = null;
        _outboxRepository
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var notification = new FlowEpicClosedEvent(
            SomeEpicId, SomeTenantId, "aaaa",
            OccurredOn: DateTimeOffset.UtcNow);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Contains(SomeEpicId.Value.ToString(), captured!.Payload, StringComparison.OrdinalIgnoreCase);
    }
}
