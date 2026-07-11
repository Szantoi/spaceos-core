// SpaceOS.Kernel.Tests/Entities/Modules/FlowProgramTests.cs

using SpaceOS.Modules.FlowManagement.Domain;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="FlowProgram"/> domain entity invariants.</summary>
public sealed class FlowProgramTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidName = "Digital Transformation";

    private static FlowProgram CreateValidProgram(string? description = null) =>
        FlowProgram.Create(ValidName, ValidTenantId, description);

    // --- Create: property assertions ---

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        var program = CreateValidProgram();

        Assert.NotEqual(Guid.Empty, program.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsName()
    {
        var program = CreateValidProgram();

        Assert.Equal(ValidName, program.Name);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        var program = CreateValidProgram();

        Assert.Equal(ValidTenantId, program.TenantId);
    }

    [Fact]
    public void Create_WithNoDescription_DescriptionIsNull()
    {
        var program = CreateValidProgram();

        Assert.Null(program.Description);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        const string description = "Multi-year transformation initiative.";

        var program = CreateValidProgram(description);

        Assert.Equal(description, program.Description);
    }

    // --- Create: guard clause ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            FlowProgram.Create(name, ValidTenantId));
    }

    // --- Id uniqueness ---

    [Fact]
    public void Create_TwoPrograms_HaveDifferentIds()
    {
        var p1 = CreateValidProgram();
        var p2 = CreateValidProgram();

        Assert.NotEqual(p1.Id, p2.Id);
    }
}
