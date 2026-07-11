using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public class UpdateFlowEpicTitleCommandValidatorTests
{
    [Fact]
    public void Validate_WhenFlowEpicIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new UpdateFlowEpicTitleCommandValidator();
        var command = new UpdateFlowEpicTitleCommand(FlowEpicId: Guid.Empty, NewTitle: "Valid Title");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FlowEpicId);
    }

    [Fact]
    public void Validate_WhenNewTitleIsEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new UpdateFlowEpicTitleCommandValidator();
        var command = new UpdateFlowEpicTitleCommand(FlowEpicId: Guid.NewGuid(), NewTitle: string.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewTitle);
    }

    [Fact]
    public void Validate_WhenNewTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var validator = new UpdateFlowEpicTitleCommandValidator();
        var command = new UpdateFlowEpicTitleCommand(FlowEpicId: Guid.NewGuid(), NewTitle: new string('x', 201));

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewTitle);
    }

    [Fact]
    public void Validate_WithFullyValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var validator = new UpdateFlowEpicTitleCommandValidator();
        var command = new UpdateFlowEpicTitleCommand(FlowEpicId: Guid.NewGuid(), NewTitle: "Redesign Lobby Layout");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
