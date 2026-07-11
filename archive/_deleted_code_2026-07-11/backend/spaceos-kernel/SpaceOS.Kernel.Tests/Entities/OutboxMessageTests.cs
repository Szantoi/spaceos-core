// SpaceOS.Kernel.Tests/Entities/OutboxMessageTests.cs

using SpaceOS.Kernel.Domain.Outbox;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>Unit tests for <see cref="OutboxMessage"/> creation and state transitions.</summary>
public class OutboxMessageTests
{
    private static readonly Guid SomeTenantId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArguments_SetsType()
    {
        // Act
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        // Assert
        Assert.Equal("EscrowTrigger", message.Type);
    }

    [Fact]
    public void Create_WithValidArguments_SetsPayload()
    {
        // Arrange
        const string payload = "{\"FlowEpicId\":\"abc\"}";

        // Act
        var message = OutboxMessage.Create("EscrowTrigger", payload, SomeTenantId);

        // Assert
        Assert.Equal(payload, message.Payload);
    }

    [Fact]
    public void Create_WithValidArguments_SetsTenantId()
    {
        // Act
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        // Assert
        Assert.Equal(SomeTenantId, message.TenantId);
    }

    [Fact]
    public void Create_WithValidArguments_AssignsNonEmptyId()
    {
        // Act
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Create_WithValidArguments_ProcessedAtIsNull()
    {
        // Act
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        // Assert
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void Create_WithValidArguments_CreatedAtIsRecentUtc()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        // Assert
        Assert.True(message.CreatedAt >= before);
        Assert.True(message.CreatedAt <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceType_ThrowsArgumentException(string type)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => OutboxMessage.Create(type, "{}", SomeTenantId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespacePayload_ThrowsArgumentException(string payload)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => OutboxMessage.Create("EscrowTrigger", payload, SomeTenantId));
    }

    // ── MarkProcessed ────────────────────────────────────────────────────────

    [Fact]
    public void MarkProcessed_SetsProcessedAt()
    {
        // Arrange
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);
        var processedAt = DateTimeOffset.UtcNow;

        // Act
        message.MarkProcessed(processedAt);

        // Assert
        Assert.Equal(processedAt, message.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_CalledTwice_OverwritesProcessedAt()
    {
        // Arrange
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);
        var first  = DateTimeOffset.UtcNow.AddMinutes(-1);
        var second = DateTimeOffset.UtcNow;

        // Act
        message.MarkProcessed(first);
        message.MarkProcessed(second);

        // Assert
        Assert.Equal(second, message.ProcessedAt);
    }
}
