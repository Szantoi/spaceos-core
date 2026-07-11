// SpaceOS.Kernel.Tests/StageRegistry/StageDefinitionTests.cs
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry;

/// <summary>Unit tests for <see cref="StageDefinition"/> domain entity invariants and events.</summary>
public sealed class StageDefinitionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ─── Register — StageCode format ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_EmptyStageCode_ThrowsDomainException(string stageCode)
    {
        Assert.Throws<DomainException>(() =>
            StageDefinition.Register(TenantId, stageCode, "Display", "http://127.0.0.1:5000"));
    }

    [Theory]
    [InlineData("1invalid")]          // starts with digit
    [InlineData("A")]                 // single char (regex requires at least 3)
    [InlineData("ab")]                // two chars (pattern requires {1,28} middle part)
    [InlineData("has space")]         // whitespace not allowed
    [InlineData("has-hyphen")]        // hyphen not in charset
    [InlineData("toolongstagecodenamethatexceedsthirtycharacters")] // > 30 chars
    public void Register_InvalidStageCode_ThrowsDomainException(string stageCode)
    {
        Assert.Throws<DomainException>(() =>
            StageDefinition.Register(TenantId, stageCode, "Display", "http://127.0.0.1:5000"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("my_stage01")]
    [InlineData("review_step")]
    public void Register_ValidStageCode_CreatesDefinition(string stageCode)
    {
        var sd = StageDefinition.Register(TenantId, stageCode, "Display", "http://127.0.0.1:5000");

        Assert.NotEqual(Guid.Empty, sd.Id);
        Assert.Equal(stageCode, sd.StageCode);
        Assert.True(sd.IsActive);
    }

    [Fact]
    public void Register_StageCode_IsNormalisedToLowerCase()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");

        Assert.Equal("my_stage01", sd.StageCode);
    }

    // ─── Register — raises StageDefinitionRegisteredEvent ────────────────────

    [Fact]
    public void Register_Valid_RaisesStageDefinitionRegisteredEvent()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");

        var events = sd.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<StageDefinitionRegisteredEvent>(events[0]);
        Assert.Equal(sd.Id, evt.Id);
        Assert.Equal(TenantId, evt.TenantId);
        Assert.Equal("my_stage01", evt.StageCode);
    }

    // ─── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");

        sd.Deactivate();

        Assert.False(sd.IsActive);
    }

    [Fact]
    public void Deactivate_UpdatesUpdatedAt()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");
        var before = sd.UpdatedAt;

        sd.Deactivate();

        Assert.True(sd.UpdatedAt >= before);
    }

    // ─── UpdateEndpoint ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateEndpoint_EmptyUrl_ThrowsDomainException()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");

        Assert.Throws<DomainException>(() => sd.UpdateEndpoint(""));
    }

    [Fact]
    public void UpdateEndpoint_Valid_RaisesStageDefinitionUpdatedEvent()
    {
        var sd = StageDefinition.Register(TenantId, "my_stage01", "Display", "http://127.0.0.1:5000");
        sd.PopDomainEvents(); // clear register event

        sd.UpdateEndpoint("http://127.0.0.1:5001");

        var events = sd.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<StageDefinitionUpdatedEvent>(events[0]);
    }
}
