// SpaceOS.Kernel.Tests/Validators/RegisterSpatialElementCommandValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Spaces.Commands;
using SpaceOS.Kernel.Application.Spaces.Commands.Validators;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>
/// Validates <see cref="RegisterSpatialElementCommandValidator"/> rules — bounding box ordering
/// and required fields (DoD: RegisterSpatialElementCommandValidator tests).
/// </summary>
public sealed class RegisterSpatialElementCommandValidatorTests
{
    private readonly RegisterSpatialElementCommandValidator _validator = new();

    /// <summary>
    /// Creates a valid command with sensible defaults for use as a baseline.
    /// </summary>
    private static RegisterSpatialElementCommand ValidCommand() =>
        new(
            PhysicalSpaceId: Guid.NewGuid(),
            FlowEpicId: Guid.NewGuid(),
            TradeType: TradeType.Door,
            ElementType: "IfcDoor",
            MinX: 0, MinY: 0, MinZ: 0,
            MaxX: 100, MaxY: 200, MaxZ: 50);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange & Act
        var result = _validator.TestValidate(ValidCommand());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MinXGreaterThanOrEqualToMaxX_FailsValidation()
    {
        // Arrange — MinX=10 >= MaxX=5
        var command = ValidCommand() with { MinX = 10, MaxX = 5 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("MinX"));
    }

    [Fact]
    public void Validate_MinYGreaterThanOrEqualToMaxY_FailsValidation()
    {
        // Arrange — MinY=10 >= MaxY=5
        var command = ValidCommand() with { MinY = 10, MaxY = 5 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("MinY"));
    }

    [Fact]
    public void Validate_MinZGreaterThanOrEqualToMaxZ_FailsValidation()
    {
        // Arrange — MinZ=10 >= MaxZ=5
        var command = ValidCommand() with { MinZ = 10, MaxZ = 5 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("MinZ"));
    }

    [Fact]
    public void Validate_ElementTypeEmpty_FailsValidation()
    {
        // Arrange
        var command = ValidCommand() with { ElementType = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ElementType);
    }
}
