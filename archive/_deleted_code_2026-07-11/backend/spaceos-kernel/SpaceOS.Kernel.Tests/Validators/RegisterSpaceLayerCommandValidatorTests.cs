using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.SpaceLayers.Commands;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public class RegisterSpaceLayerCommandValidatorTests
{
    [Fact]
    public void Validate_WhenFacilityIdEmpty_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.Empty,
            TradeType: TradeType.Joinery,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: "{}",
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FacilityId);
    }

    [Fact]
    public void Validate_WhenTradeTypeOutOfRange_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: (TradeType)999,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: "{}",
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TradeType);
    }

    [Fact]
    public void Validate_WhenIsExternalNodeTrueAndUrlMissing_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Electrical,
            IsExternalNode: true,
            ExternalSourceUrl: null,
            IntentDataJson: null,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExternalSourceUrl);
    }

    [Fact]
    public void Validate_WhenIsExternalNodeTrueAndUrlIsNotAbsoluteUri_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Electrical,
            IsExternalNode: true,
            ExternalSourceUrl: "not-a-valid-url",
            IntentDataJson: null,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExternalSourceUrl);
    }

    [Fact]
    public void Validate_WhenIsExternalNodeTrueAndUrlIsValid_ShouldNotHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Plumbing,
            IsExternalNode: true,
            ExternalSourceUrl: "https://external.spaceos.io/layers/abc",
            IntentDataJson: null,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExternalSourceUrl);
    }

    [Fact]
    public void Validate_WhenIsExternalNodeFalseAndJsonMissing_ShouldHaveError()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Architecture,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: null,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IntentDataJson);
    }

    [Fact]
    public void Validate_WhenIsExternalNodeFalseAndJsonIsProvided_ShouldNotHaveError()
    {
        // Arrange — Mep schema requires a "systems" array property
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Mep,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: """{"systems":["hvac","ventilation"]}""",
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IntentDataJson);
    }

    [Fact]
    public void Validate_WithFullyValidLocalCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange — Joinery schema requires "material" (string) and "dimensions" (object)
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Joinery,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: """{"material":"oak","dimensions":{"width":900,"height":2100}}""",
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithFullyValidExternalCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var validator = new RegisterSpaceLayerCommandValidator();
        var command = new RegisterSpaceLayerCommand(
            FacilityId: Guid.NewGuid(),
            TradeType: TradeType.Architecture,
            IsExternalNode: true,
            ExternalSourceUrl: "https://federated.spaceos.io/layers/arch-001",
            IntentDataJson: null,
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
