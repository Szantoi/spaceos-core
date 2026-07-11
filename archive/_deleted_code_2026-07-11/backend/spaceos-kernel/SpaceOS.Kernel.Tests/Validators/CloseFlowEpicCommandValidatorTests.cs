// SpaceOS.Kernel.Tests/Validators/CloseFlowEpicCommandValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>Unit tests for <see cref="CloseFlowEpicCommandValidator"/>.</summary>
public class CloseFlowEpicCommandValidatorTests
{
    private const string ValidUrl      = "https://storage.example.com/proof/doc.pdf";
    private const string ValidHash     = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1e2";
    private const string InvalidHash63 = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1";  // 63 chars
    private const string InvalidHash65 = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1e23"; // 65 chars
    private const string NonHexHash    = "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz";   // 64 non-hex

    private readonly CloseFlowEpicCommandValidator _validator = new();

    // ── Valid command ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidUrl, ValidHash);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── FlowEpicId ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenFlowEpicIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.Empty, ValidUrl, ValidHash);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FlowEpicId);
    }

    // ── ProofUrl ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenProofUrlIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), string.Empty, ValidHash);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("just plain text")]
    public void Validate_WhenProofUrlIsNotAbsoluteUrl_ShouldHaveError(string badUrl)
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), badUrl, ValidHash);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofUrl);
    }

    // ── ProofHash ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenProofHashIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidUrl, string.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofHash);
    }

    [Fact]
    public void Validate_WhenProofHashIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidUrl, InvalidHash63);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofHash);
    }

    [Fact]
    public void Validate_WhenProofHashIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidUrl, InvalidHash65);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofHash);
    }

    [Fact]
    public void Validate_WhenProofHashContainsNonHexCharacters_ShouldHaveError()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidUrl, NonHexHash);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProofHash);
    }
}
