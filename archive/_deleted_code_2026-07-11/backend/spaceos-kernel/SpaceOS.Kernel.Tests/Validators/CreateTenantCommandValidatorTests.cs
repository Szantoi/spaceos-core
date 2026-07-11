using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Tenants.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public class CreateTenantCommandValidatorTests
{
    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new CreateTenantCommandValidator();
        var command = new CreateTenantCommand(Name: string.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var validator = new CreateTenantCommandValidator();
        var command = new CreateTenantCommand(Name: new string('a', 101));

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithFullyValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var validator = new CreateTenantCommandValidator();
        var command = new CreateTenantCommand(Name: "Acme Corp");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
