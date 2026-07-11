// SpaceOS.Kernel.Tests/Entities/Modules/FlowTaskTests.cs

using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="FlowTask"/> domain entity invariants.</summary>
public sealed class FlowTaskTests
{
    private static readonly Guid ValidEpicKernelId = Guid.NewGuid();
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidName = "Implement login page";

    private static FlowTask CreateValidTask(Guid? milestoneId = null) =>
        FlowTask.Create(ValidEpicKernelId, ValidName, ValidTenantId, milestoneId);

    // --- Create: property assertions ---

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        var task = CreateValidTask();

        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsEpicKernelId()
    {
        var task = CreateValidTask();

        Assert.Equal(ValidEpicKernelId, task.EpicKernelId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsName()
    {
        var task = CreateValidTask();

        Assert.Equal(ValidName, task.Name);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        var task = CreateValidTask();

        Assert.Equal(ValidTenantId, task.TenantId);
    }

    [Fact]
    public void Create_WithValidArgs_SetsStatusToOpen()
    {
        var task = CreateValidTask();

        Assert.Equal("Open", task.Status);
    }

    [Fact]
    public void Create_WithNoMilestoneId_MilestoneIdIsNull()
    {
        var task = CreateValidTask();

        Assert.Null(task.MilestoneId);
    }

    [Fact]
    public void Create_WithMilestoneId_SetsMilestoneId()
    {
        var milestoneId = Guid.NewGuid();

        var task = CreateValidTask(milestoneId);

        Assert.Equal(milestoneId, task.MilestoneId);
    }

    // --- Create: guard clause ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            FlowTask.Create(ValidEpicKernelId, name, ValidTenantId));
    }

    // --- Complete ---

    [Fact]
    public void Complete_SetsStatusToCompleted()
    {
        var task = CreateValidTask();

        task.Complete();

        Assert.Equal("Completed", task.Status);
    }

    // --- Reopen ---

    [Fact]
    public void Reopen_AfterComplete_SetsStatusBackToOpen()
    {
        var task = CreateValidTask();
        task.Complete();

        task.Reopen();

        Assert.Equal("Open", task.Status);
    }

    [Fact]
    public void Reopen_OnOpenTask_ThrowsInvalidOperationException()
    {
        var task = CreateValidTask();

        Assert.Throws<InvalidOperationException>(() => task.Reopen());
    }

    // --- Complete: phase transition ---

    [Fact]
    public void Complete_SetsPhaseToClosedDone()
    {
        var task = CreateValidTask();

        task.Complete();

        Assert.Equal(WorkflowPhase.ClosedDone, task.Phase);
    }

    // --- Reopen: phase transition ---

    [Fact]
    public void Reopen_AfterComplete_SetsPhaseBackToDiscovery()
    {
        var task = CreateValidTask();
        task.Complete();

        task.Reopen();

        Assert.Equal(WorkflowPhase.Discovery, task.Phase);
    }

    // --- Assign ---

    [Fact]
    public void Assign_SetsAssigneeId()
    {
        var task = CreateValidTask();
        var assigneeId = Guid.NewGuid();

        task.Assign(assigneeId);

        Assert.Equal(assigneeId, task.AssigneeId);
    }

    [Fact]
    public void Assign_OverwritesPreviousAssignee()
    {
        var task = CreateValidTask();
        task.Assign(Guid.NewGuid());
        var newAssignee = Guid.NewGuid();

        task.Assign(newAssignee);

        Assert.Equal(newAssignee, task.AssigneeId);
    }

    // --- Id uniqueness ---

    [Fact]
    public void Create_TwoTasks_HaveDifferentIds()
    {
        var task1 = CreateValidTask();
        var task2 = CreateValidTask();

        Assert.NotEqual(task1.Id, task2.Id);
    }
}
