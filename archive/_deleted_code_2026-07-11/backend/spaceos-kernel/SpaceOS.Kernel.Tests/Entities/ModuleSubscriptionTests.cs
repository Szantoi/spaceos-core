// SpaceOS.Kernel.Tests/Entities/ModuleSubscriptionTests.cs

using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>Unit tests for <see cref="ModuleSubscription"/> entity creation and invariants.</summary>
public sealed class ModuleSubscriptionTests
{
    // ── Create — happy path ───────────────────────────────────────────────────

    [Fact]
    public void Create_ValidArguments_SetsAllFields()
    {
        var sub = ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", "http://127.0.0.1:5007/internal/inbox/cutting");

        Assert.Equal("Manufacturing",           sub.SubscriberModule);
        Assert.Equal("CuttingPanelCompleted",   sub.EventType);
        Assert.Equal("http://127.0.0.1:5007/internal/inbox/cutting", sub.InboxEndpoint);
        Assert.True(sub.IsActive);
        Assert.NotEqual(Guid.Empty, sub.Id);
    }

    [Fact]
    public void Create_ValidArguments_CreatedAtIsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var sub = ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", "http://127.0.0.1/inbox");

        Assert.True(sub.CreatedAt >= before);
        Assert.True(sub.CreatedAt <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    // ── Create — guard clauses ────────────────────────────────────────────────

    [Theory]
    [InlineData("", "EventType", "http://endpoint")]
    [InlineData("   ", "EventType", "http://endpoint")]
    [InlineData("Module", "", "http://endpoint")]
    [InlineData("Module", "   ", "http://endpoint")]
    [InlineData("Module", "EventType", "")]
    [InlineData("Module", "EventType", "   ")]
    public void Create_EmptyOrWhitespaceArgument_ThrowsArgumentException(
        string module, string eventType, string endpoint)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ModuleSubscription.Create(module, eventType, endpoint));
    }

    // ── Uniqueness contract ───────────────────────────────────────────────────

    [Fact]
    public void Create_TwoSubscriptionsWithSameModuleAndEvent_HaveDifferentIds()
    {
        var a = ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", "http://a");
        var b = ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", "http://b");

        Assert.NotEqual(a.Id, b.Id);
    }
}
