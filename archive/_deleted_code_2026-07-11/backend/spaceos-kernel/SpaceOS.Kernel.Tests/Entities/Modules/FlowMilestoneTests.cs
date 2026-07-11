// SpaceOS.Kernel.Tests/Entities/Modules/FlowMilestoneTests.cs

using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="FlowMilestone"/> domain entity invariants.</summary>
public sealed class FlowMilestoneTests
{
    private static readonly Guid ValidProjectId = Guid.NewGuid();
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidName = "Alpha Release";

    private static FlowMilestone CreateValidMilestone() =>
        FlowMilestone.Create(ValidName, ValidProjectId, ValidTenantId);

    // --- Create: property assertions ---

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        var milestone = CreateValidMilestone();

        Assert.NotEqual(Guid.Empty, milestone.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsName()
    {
        var milestone = CreateValidMilestone();

        Assert.Equal(ValidName, milestone.Name);
    }

    [Fact]
    public void Create_WithValidArgs_SetsProjectId()
    {
        var milestone = CreateValidMilestone();

        Assert.Equal(ValidProjectId, milestone.ProjectId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        var milestone = CreateValidMilestone();

        Assert.Equal(ValidTenantId, milestone.TenantId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsStatusToOpen()
    {
        var milestone = CreateValidMilestone();

        Assert.Equal("Open", milestone.Status);
    }

    [Fact]
    public void Create_WithValidArgs_TargetDateIsNull()
    {
        var milestone = CreateValidMilestone();

        Assert.Null(milestone.TargetDate);
    }

    // --- Create: guard clause ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            FlowMilestone.Create(name, ValidProjectId, ValidTenantId));
    }

    // --- Complete ---

    [Fact]
    public void Complete_SetsStatusToCompleted()
    {
        var milestone = CreateValidMilestone();

        milestone.Complete();

        Assert.Equal("Completed", milestone.Status);
    }

    [Fact]
    public void Complete_SetsPhaseToClosedDone()
    {
        var milestone = CreateValidMilestone();

        milestone.Complete();

        Assert.Equal(WorkflowPhase.ClosedDone, milestone.Phase);
    }

    // --- UpdateTargetDate ---

    [Fact]
    public void UpdateTargetDate_WithDate_SetsTargetDate()
    {
        var milestone = CreateValidMilestone();
        var date = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

        milestone.UpdateTargetDate(date);

        Assert.Equal(date, milestone.TargetDate);
    }

    [Fact]
    public void UpdateTargetDate_WithNull_ClearsTargetDate()
    {
        var milestone = CreateValidMilestone();
        milestone.UpdateTargetDate(DateTimeOffset.UtcNow);

        milestone.UpdateTargetDate(null);

        Assert.Null(milestone.TargetDate);
    }

    // --- Id uniqueness ---

    [Fact]
    public void Create_TwoMilestones_HaveDifferentIds()
    {
        var m1 = CreateValidMilestone();
        var m2 = CreateValidMilestone();

        Assert.NotEqual(m1.Id, m2.Id);
    }
}
