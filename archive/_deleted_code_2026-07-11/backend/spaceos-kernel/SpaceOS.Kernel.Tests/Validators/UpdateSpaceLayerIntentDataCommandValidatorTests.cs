// SpaceOS.Kernel.Tests/Validators/UpdateSpaceLayerIntentDataCommandValidatorTests.cs

using SpaceOS.Kernel.Application.SpaceLayers.Commands;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>
/// Unit tests for <see cref="UpdateSpaceLayerIntentDataCommandValidator"/>.
/// Covers required-field rules and per-<see cref="TradeType"/> JSON schema rules.
/// </summary>
public sealed class UpdateSpaceLayerIntentDataCommandValidatorTests
{
    private readonly UpdateSpaceLayerIntentDataCommandValidator _validator = new();

    // -------------------------------------------------------------------------
    // SpaceLayerId is required
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptySpaceLayerId_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(Guid.Empty, """{"anything":1}""");

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(cmd.SpaceLayerId));
    }

    // -------------------------------------------------------------------------
    // IntentDataJson is required
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyIntentDataJson_FailsValidation(string json)
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(Guid.NewGuid(), json);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(cmd.IntentDataJson));
    }

    // -------------------------------------------------------------------------
    // Generic (no TradeType) — valid JSON → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_NoTradeTypeValidJson_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(Guid.NewGuid(), """{"key":"value"}""");

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Generic (no TradeType) — invalid JSON → fails
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_NoTradeTypeInvalidJson_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(Guid.NewGuid(), "{not json}");

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // TradeType.Joinery — valid schema → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryValidSchema_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"material":"oak","dimensions":{}}""",
            TradeType.Joinery);

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // TradeType.Joinery — missing required property → fails with IntentDataJson error
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryMissingMaterial_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"dimensions":{}}""",
            TradeType.Joinery);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // TradeType.Plumbing — valid schema → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_PlumbingValidSchema_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"pipeDiameter":25.4,"fluidType":"water"}""",
            TradeType.Plumbing);

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // TradeType.Plumbing — missing required property → fails
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_PlumbingMissingFluidType_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"pipeDiameter":25.4}""",
            TradeType.Plumbing);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // TradeType.Electrical — valid schema → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElectricalValidSchema_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"voltage":230,"circuitCount":12}""",
            TradeType.Electrical);

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // TradeType.Electrical — missing required property → fails
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElectricalMissingCircuitCount_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"voltage":230}""",
            TradeType.Electrical);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // TradeType.Architecture — valid schema → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ArchitectureValidSchema_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"floorPlan":"planA"}""",
            TradeType.Architecture);

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // TradeType.Architecture — missing required property → fails
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ArchitectureMissingFloorPlan_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"other":"value"}""",
            TradeType.Architecture);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // TradeType.Mep — valid schema → passes
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MepValidSchema_PassesValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"systems":["hvac"]}""",
            TradeType.Mep);

        var result = _validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // TradeType.Mep — missing required property → fails
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MepMissingSystems_FailsValidation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"other":"value"}""",
            TradeType.Mep);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntentDataJson");
    }

    // -------------------------------------------------------------------------
    // Error message contains schema violation detail from IntentDataSchemaValidator
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryMissingMaterial_ErrorMessageDescribesViolation()
    {
        var cmd = new UpdateSpaceLayerIntentDataCommand(
            Guid.NewGuid(),
            """{"dimensions":{}}""",
            TradeType.Joinery);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == "IntentDataJson");
        Assert.Contains("material", error.ErrorMessage);
    }
}
