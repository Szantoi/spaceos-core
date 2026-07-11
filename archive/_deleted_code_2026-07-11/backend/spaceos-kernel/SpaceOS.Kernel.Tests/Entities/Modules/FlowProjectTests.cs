// SpaceOS.Kernel.Tests/Entities/Modules/FlowProjectTests.cs

using SpaceOS.Modules.FlowManagement.Domain;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="FlowProject"/> domain entity invariants.</summary>
public sealed class FlowProjectTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidName = "Kernel Platform";

    private static FlowProject CreateValidProject(Guid? programId = null) =>
        FlowProject.Create(ValidName, ValidTenantId, programId);

    // --- Create: property assertions ---

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        var project = CreateValidProject();

        Assert.NotEqual(Guid.Empty, project.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsName()
    {
        var project = CreateValidProject();

        Assert.Equal(ValidName, project.Name);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        var project = CreateValidProject();

        Assert.Equal(ValidTenantId, project.TenantId);
    }

    [Fact]
    public void Create_WithNoProgramId_ProgramIdIsNull()
    {
        var project = CreateValidProject();

        Assert.Null(project.ProgramId);
    }

    [Fact]
    public void Create_WithProgramId_SetsProgramId()
    {
        var programId = Guid.NewGuid();

        var project = CreateValidProject(programId);

        Assert.Equal(programId, project.ProgramId);
    }

    [Fact]
    public void Create_WithValidArgs_DescriptionIsNull()
    {
        var project = CreateValidProject();

        Assert.Null(project.Description);
    }

    [Fact]
    public void Create_WithValidArgs_StartDateIsNull()
    {
        var project = CreateValidProject();

        Assert.Null(project.StartDate);
    }

    [Fact]
    public void Create_WithValidArgs_EndDateIsNull()
    {
        var project = CreateValidProject();

        Assert.Null(project.EndDate);
    }

    // --- Create: guard clause ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            FlowProject.Create(name, ValidTenantId));
    }

    // --- UpdateDates ---

    [Fact]
    public void UpdateDates_WithBothDates_SetsStartAndEndDate()
    {
        var project = CreateValidProject();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end   = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

        project.UpdateDates(start, end);

        Assert.Equal(start, project.StartDate);
        Assert.Equal(end, project.EndDate);
    }

    [Fact]
    public void UpdateDates_WithNullBoth_ClearsDates()
    {
        var project = CreateValidProject();
        project.UpdateDates(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        project.UpdateDates(null, null);

        Assert.Null(project.StartDate);
        Assert.Null(project.EndDate);
    }

    [Fact]
    public void UpdateDates_NullStart_OnlySetsEndDate()
    {
        var project = CreateValidProject();
        var end = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

        project.UpdateDates(null, end);

        Assert.Null(project.StartDate);
        Assert.Equal(end, project.EndDate);
    }

    // --- Id uniqueness ---

    [Fact]
    public void Create_TwoProjects_HaveDifferentIds()
    {
        var p1 = CreateValidProject();
        var p2 = CreateValidProject();

        Assert.NotEqual(p1.Id, p2.Id);
    }
}
