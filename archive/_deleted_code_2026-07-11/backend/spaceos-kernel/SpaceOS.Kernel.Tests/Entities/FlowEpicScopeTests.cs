// SpaceOS.Kernel.Tests/Entities/FlowEpicScopeTests.cs

using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>
/// Tests for the <see cref="FlowEpicScope"/> extension: MicroAssembly scope,
/// RequiredResources, and RequiredSkillLevel on <see cref="FlowEpic"/>.
/// </summary>
public sealed class FlowEpicScopeTests
{
    private static readonly TenantId TestTenant = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId TestFacility = FacilityId.From(Guid.NewGuid());

    [Fact]
    public void Create_WithMicroAssemblyScope_SetsScopeCorrectly()
    {
        // Arrange & Act
        var epic = FlowEpic.Create("Cabinet Panel Assembly", TestFacility, TestTenant, FlowEpicScope.MicroAssembly);

        // Assert
        Assert.Equal(FlowEpicScope.MicroAssembly, epic.Scope);
        Assert.Equal("Cabinet Panel Assembly", epic.Title.Value);
        Assert.Equal(WorkflowPhase.Discovery, epic.Phase);
    }

    [Fact]
    public void AddRequiredResource_SingleResource_ReadBackCorrectly()
    {
        // Arrange
        var epic = FlowEpic.Create("Assembly Job", TestFacility, TestTenant, FlowEpicScope.MicroAssembly);
        var resource = FlowEpicRequiredResource.Create("Machine", "CNC Router", 2);

        // Act
        epic.AddRequiredResource(resource);

        // Assert
        Assert.Single(epic.RequiredResources);
        Assert.Equal("Machine", epic.RequiredResources[0].ResourceType);
        Assert.Equal("CNC Router", epic.RequiredResources[0].ResourceName);
        Assert.Equal(2, epic.RequiredResources[0].Quantity);
    }

    [Fact]
    public void SetRequiredSkillLevel_SetsAndReadsBack()
    {
        // Arrange
        var epic = FlowEpic.Create("Precision Assembly", TestFacility, TestTenant, FlowEpicScope.MicroAssembly);

        // Act
        epic.SetRequiredSkillLevel("Senior");

        // Assert
        Assert.Equal("Senior", epic.RequiredSkillLevel);

        // Act — clear
        epic.SetRequiredSkillLevel(null);

        // Assert
        Assert.Null(epic.RequiredSkillLevel);
    }

    [Fact]
    public void Create_WithoutScope_ScopeIsNull_BackwardCompatible()
    {
        // Arrange & Act — using the original factory (no scope parameter)
        var epic = FlowEpic.Create("Legacy Door Order", TestFacility, TestTenant);

        // Assert
        Assert.Null(epic.Scope);
        Assert.Null(epic.RequiredSkillLevel);
        Assert.Empty(epic.RequiredResources);
    }

    [Theory]
    [InlineData(FlowEpicScope.DoorOrder)]
    [InlineData(FlowEpicScope.CuttingPlan)]
    [InlineData(FlowEpicScope.MicroAssembly)]
    public void Create_WithExplicitScope_PreservesScope(FlowEpicScope scope)
    {
        // Arrange & Act
        var epic = FlowEpic.Create("Scoped Epic", TestFacility, TestTenant, scope);

        // Assert
        Assert.Equal(scope, epic.Scope);
    }
}
