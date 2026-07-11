// SpaceOS.Kernel.Tests/StageRegistry/StageChainTemplateTests.cs
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry;

/// <summary>Unit tests for <see cref="StageChainTemplate"/> aggregate behaviour.</summary>
public sealed class StageChainTemplateTests
{
    private static readonly Guid TenantId = new("40000000-0000-0000-0000-000000000004");

    private static StageDefinition MakeStageDef(string code = "stage_a")
        => StageDefinition.Register(TenantId, code, $"Display {code}", "http://127.0.0.1:5000");

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_EmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            StageChainTemplate.Create(TenantId, ""));
    }

    [Fact]
    public void Create_ValidName_SetsName()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");

        Assert.Equal("standard", template.Name);
        Assert.Equal(TenantId, template.TenantId);
    }

    [Fact]
    public void Create_RaisesStageChainCreatedEvent()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");

        var events = template.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<StageChainCreatedEvent>(events[0]);
        Assert.Equal(template.Id, evt.Id);
        Assert.Equal(TenantId, evt.TenantId);
        Assert.Equal("standard", evt.Name);
    }

    // ─── AddStep ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddStep_Valid_AddsStep()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        var stageDef = MakeStageDef("stage_a");

        template.AddStep(stageDef, sortOrder: 1);

        Assert.Single(template.Steps);
        Assert.Equal("stage_a", template.Steps[0].StageCode);
        Assert.Equal(1, template.Steps[0].SortOrder);
    }

    [Fact]
    public void AddStep_DuplicateStageCode_ThrowsDomainException()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        var stageDef = MakeStageDef("stage_a");
        template.AddStep(stageDef, sortOrder: 1);

        Assert.Throws<DomainException>(() =>
            template.AddStep(stageDef, sortOrder: 2));
    }

    [Fact]
    public void AddStep_DuplicateSortOrder_ThrowsDomainException()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        template.AddStep(MakeStageDef("stage_a"), sortOrder: 1);

        Assert.Throws<DomainException>(() =>
            template.AddStep(MakeStageDef("stage_b"), sortOrder: 1));
    }

    [Fact]
    public void AddStep_MaxStepsExceeded_ThrowsDomainException()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        for (var i = 1; i <= 20; i++)
            template.AddStep(MakeStageDef($"stage_{i:D2}"), sortOrder: i);

        // 21st step must fail
        var extra = MakeStageDef("stage_extra");
        Assert.Throws<DomainException>(() =>
            template.AddStep(extra, sortOrder: 21));
    }

    [Fact]
    public void AddStep_ExactlyTwentySteps_Succeeds()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        for (var i = 1; i <= 20; i++)
            template.AddStep(MakeStageDef($"stage_{i:D2}"), sortOrder: i);

        Assert.Equal(20, template.Steps.Count);
    }

    [Fact]
    public void AddStep_Optional_IsOptionalFlagSet()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        var stageDef = MakeStageDef("stage_a");

        template.AddStep(stageDef, sortOrder: 1, isOptional: true);

        Assert.True(template.Steps[0].IsOptional);
    }

    // ─── RemoveStep ───────────────────────────────────────────────────────────

    [Fact]
    public void RemoveStep_ExistingStageCode_RemovesStep()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");
        template.AddStep(MakeStageDef("stage_a"), sortOrder: 1);
        template.AddStep(MakeStageDef("stage_b"), sortOrder: 2);

        template.RemoveStep("stage_a");

        Assert.Single(template.Steps);
        Assert.Equal("stage_b", template.Steps[0].StageCode);
    }

    [Fact]
    public void RemoveStep_NonexistentStageCode_ThrowsDomainException()
    {
        var template = StageChainTemplate.Create(TenantId, "standard");

        Assert.Throws<DomainException>(() => template.RemoveStep("ghost_stage"));
    }
}
