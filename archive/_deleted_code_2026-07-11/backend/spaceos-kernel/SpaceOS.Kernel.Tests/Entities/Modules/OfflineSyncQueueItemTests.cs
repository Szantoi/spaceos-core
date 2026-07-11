// SpaceOS.Kernel.Tests/Entities/Modules/OfflineSyncQueueItemTests.cs

using SpaceOS.Modules.FlowManagement.Domain;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="OfflineSyncQueueItem"/> domain entity invariants.</summary>
public sealed class OfflineSyncQueueItemTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Guid ValidClientSignalId = Guid.NewGuid();
    private const string ValidPayload = "{\"op\":\"UpdateStatus\",\"status\":\"Completed\"}";

    private static OfflineSyncQueueItem CreateValidItem() =>
        OfflineSyncQueueItem.Create(ValidTenantId, ValidPayload, ValidClientSignalId);

    // --- Create: property assertions ---

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        var item = CreateValidItem();

        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        var item = CreateValidItem();

        Assert.Equal(ValidTenantId, item.TenantId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsPayload()
    {
        var item = CreateValidItem();

        Assert.Equal(ValidPayload, item.Payload);
    }

    [Fact]
    public void Create_WithValidArgs_SetsClientSignalId()
    {
        var item = CreateValidItem();

        Assert.Equal(ValidClientSignalId, item.ClientSignalId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsCreatedAtToNearNow()
    {
        var before = DateTimeOffset.UtcNow;

        var item = CreateValidItem();

        var after = DateTimeOffset.UtcNow;
        Assert.True(item.CreatedAt >= before && item.CreatedAt <= after);
    }

    [Fact]
    public void Create_WithValidArgs_SetsExpiresAt_30DaysFromNow()
    {
        var before = DateTimeOffset.UtcNow;

        var item = CreateValidItem();

        var after = DateTimeOffset.UtcNow;
        var expectedMin = before.AddDays(30);
        var expectedMax = after.AddDays(30);
        Assert.True(item.ExpiresAt >= expectedMin && item.ExpiresAt <= expectedMax);
    }

    [Fact]
    public void Create_WithValidArgs_ExpiresAt_IsExactly30DaysAfterCreatedAt()
    {
        var item = CreateValidItem();

        var ttl = item.ExpiresAt - item.CreatedAt;
        // Allow 1 second tolerance for clock ticks between the two DateTimeOffset.UtcNow calls inside Create()
        Assert.True(ttl.TotalDays >= 30 - 0.001 && ttl.TotalDays <= 30.001);
    }

    // --- Create: guard clause ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespacePayload_ThrowsArgumentException(string payload)
    {
        Assert.Throws<ArgumentException>(() =>
            OfflineSyncQueueItem.Create(ValidTenantId, payload, ValidClientSignalId));
    }

    // --- Id uniqueness ---

    [Fact]
    public void Create_TwoItems_HaveDifferentIds()
    {
        var item1 = CreateValidItem();
        var item2 = CreateValidItem();

        Assert.NotEqual(item1.Id, item2.Id);
    }
}
