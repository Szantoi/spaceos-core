using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.WorkStations.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public class RegisterWorkStationCommandValidatorTests
{
    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: string.Empty,
            Type: "Assembly",
            FacilityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: new string('a', 101),
            Type: "Assembly",
            FacilityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenTypeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: "Station Alpha",
            Type: string.Empty,
            FacilityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WhenTypeExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: "Station Alpha",
            Type: new string('t', 51),
            FacilityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WhenFacilityIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: "Station Alpha",
            Type: "Assembly",
            FacilityId: Guid.Empty,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FacilityId);
    }

    [Fact]
    public void Validate_WithFullyValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var validator = new RegisterWorkStationCommandValidator();
        var command = new RegisterWorkStationCommand(
            Name: "Station Alpha",
            Type: "Assembly",
            FacilityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
