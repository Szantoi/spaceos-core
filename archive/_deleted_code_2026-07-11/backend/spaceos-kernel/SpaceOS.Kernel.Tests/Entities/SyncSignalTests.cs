// SpaceOS.Kernel.Tests/Entities/SyncSignalTests.cs

using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>Unit tests for <see cref="SyncSignal"/> aggregate root invariants.</summary>
public sealed class SyncSignalTests
{
    private static readonly FlowEpicId ValidEpicId = FlowEpicId.New();
    private static readonly TenantId ValidTenantId = TenantId.New();
    private const string ValidNewState = "Delivery";
    private const string ValidStateHash = "abc123statehashabcdef1234567890abcdef1234567890abcdef1234567890ab";
    private const string ValidPreviousHash = "GENESIS";
    private static readonly Guid ValidClientSignalId = Guid.NewGuid();

    private static SyncSignal CreateValidSignal() =>
        SyncSignal.Create(ValidEpicId, ValidTenantId, ValidNewState,
            ValidStateHash, ValidPreviousHash, ValidClientSignalId);

    [Fact]
    public void Create_WithValidArgs_SetsEpicId()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidEpicId, signal.EpicId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidTenantId, signal.TenantId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsNewState()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidNewState, signal.NewState);
    }

    [Fact]
    public void Create_WithValidArgs_SetsStateHash()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidStateHash, signal.StateHash);
    }

    [Fact]
    public void Create_WithValidArgs_SetsPreviousHash()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidPreviousHash, signal.PreviousHash);
    }

    [Fact]
    public void Create_WithValidArgs_SetsClientSignalId()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.Equal(ValidClientSignalId, signal.ClientSignalId);
    }

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.NotEqual(Guid.Empty, signal.Id);
    }

    [Fact]
    public void Create_SetsIsSyncedToKernel_False()
    {
        // Act
        var signal = CreateValidSignal();

        // Assert
        Assert.False(signal.IsSyncedToKernel);
    }

    [Fact]
    public void Create_SetsExpiresAt_30DaysFromNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var signal = CreateValidSignal();

        // Assert
        var after = DateTimeOffset.UtcNow;
        var expectedMin = before.AddDays(SyncConstants.OfflineQueueTtlDays);
        var expectedMax = after.AddDays(SyncConstants.OfflineQueueTtlDays);
        Assert.True(signal.ExpiresAt >= expectedMin && signal.ExpiresAt <= expectedMax);
    }

    [Fact]
    public void Create_SetsOccurredAtToNearNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var signal = CreateValidSignal();

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.True(signal.OccurredAt >= before && signal.OccurredAt <= after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceNewState_ThrowsArgumentException(string newState)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SyncSignal.Create(ValidEpicId, ValidTenantId, newState,
                ValidStateHash, ValidPreviousHash, ValidClientSignalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceStateHash_ThrowsArgumentException(string stateHash)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SyncSignal.Create(ValidEpicId, ValidTenantId, ValidNewState,
                stateHash, ValidPreviousHash, ValidClientSignalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespacePreviousHash_ThrowsArgumentException(string previousHash)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SyncSignal.Create(ValidEpicId, ValidTenantId, ValidNewState,
                ValidStateHash, previousHash, ValidClientSignalId));
    }

    [Fact]
    public void MarkSynced_SetsIsSyncedToKernel_True()
    {
        // Arrange
        var signal = CreateValidSignal();

        // Act
        signal.MarkSynced();

        // Assert
        Assert.True(signal.IsSyncedToKernel);
    }

    [Fact]
    public void MarkSynced_CalledTwice_IsSyncedToKernel_RemainsTrue()
    {
        // Arrange
        var signal = CreateValidSignal();
        signal.MarkSynced();

        // Act
        signal.MarkSynced();

        // Assert
        Assert.True(signal.IsSyncedToKernel);
    }
}
