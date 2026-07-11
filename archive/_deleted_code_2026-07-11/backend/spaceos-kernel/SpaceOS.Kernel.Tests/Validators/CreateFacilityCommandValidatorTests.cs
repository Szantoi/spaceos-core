using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Facilities.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public class CreateFacilityCommandValidatorTests
{
    [Fact]
    public void Validate_WhenTenantIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new CreateFacilityCommandValidator();
        var command = new CreateFacilityCommand(TenantId: Guid.Empty, Name: "Main Floor");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new CreateFacilityCommandValidator();
        var command = new CreateFacilityCommand(TenantId: Guid.NewGuid(), Name: string.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var validator = new CreateFacilityCommandValidator();
        var command = new CreateFacilityCommand(TenantId: Guid.NewGuid(), Name: new string('b', 101));

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithFullyValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var validator = new CreateFacilityCommandValidator();
        var command = new CreateFacilityCommand(TenantId: Guid.NewGuid(), Name: "Building A");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
