// SpaceOS.Kernel.Tests/Validators/RegisterPhysicalSpaceCommandValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Spaces.Commands;
using SpaceOS.Kernel.Application.Spaces.Commands.Validators;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>
/// Validates <see cref="RegisterPhysicalSpaceCommandValidator"/> boundary rules
/// (DoD: RegisterPhysicalSpaceCommandValidator tests — valid + invalid boundary cases).
/// </summary>
public sealed class RegisterPhysicalSpaceCommandValidatorTests
{
    private readonly RegisterPhysicalSpaceCommandValidator _validator = new();

    /// <summary>
    /// Creates a valid command with sensible defaults for use as a baseline.
    /// </summary>
    private static RegisterPhysicalSpaceCommand ValidCommand() =>
        new(
            FacilityId: Guid.NewGuid(),
            WidthMm: 5000,
            HeightMm: 2500,
            DepthMm: 3000,
            OriginX: 0,
            OriginY: 0,
            OriginZ: 0,
            SpaceType: SpaceType.Room,
            CellSizeMm: 500);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange & Act
        var result = _validator.TestValidate(ValidCommand());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FacilityIdEmpty_FailsValidation()
    {
        // Arrange
        var command = ValidCommand() with { FacilityId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FacilityId);
    }

    [Fact]
    public void Validate_WidthMmZero_FailsValidation()
    {
        // Arrange
        var command = ValidCommand() with { WidthMm = 0 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WidthMm);
    }

    [Fact]
    public void Validate_WidthMmExceedsMax_FailsValidation()
    {
        // Arrange — max is 100_000, so 100_001 should fail
        var command = ValidCommand() with { WidthMm = 100_001 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WidthMm);
    }

    [Fact]
    public void Validate_HeightMmExceedsMax_FailsValidation()
    {
        // Arrange — max is 30_000, so 30_001 should fail
        var command = ValidCommand() with { HeightMm = 30_001 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HeightMm);
    }

    [Fact]
    public void Validate_CellSizeMmBelowMin_FailsValidation()
    {
        // Arrange — min is 100, so 99 should fail
        var command = ValidCommand() with { CellSizeMm = 99 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CellSizeMm);
    }

    [Fact]
    public void Validate_CellSizeMmExceedsMax_FailsValidation()
    {
        // Arrange — max is 5_000, so 5_001 should fail
        var command = ValidCommand() with { CellSizeMm = 5_001 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CellSizeMm);
    }
}
