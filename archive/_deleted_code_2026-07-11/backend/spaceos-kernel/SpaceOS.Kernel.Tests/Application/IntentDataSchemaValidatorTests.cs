// SpaceOS.Kernel.Tests/Application/IntentDataSchemaValidatorTests.cs

using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>
/// Unit tests for <see cref="IntentDataSchemaValidator"/>.
/// Validates all per-<see cref="TradeType"/> schemas, generic rules, size limit, and JSON parse failure.
/// </summary>
public sealed class IntentDataSchemaValidatorTests
{
    // -------------------------------------------------------------------------
    // TradeType.Joinery — valid schema
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryValidSchema_ReturnsNull()
    {
        var json = """{"material":"oak","dimensions":{}}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Joinery);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Joinery — missing material property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryMissingMaterial_ReturnsError()
    {
        var json = """{"dimensions":{}}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Joinery);

        Assert.NotNull(error);
        Assert.Contains("material", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Joinery — missing dimensions property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryMissingDimensions_ReturnsError()
    {
        var json = """{"material":"oak"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Joinery);

        Assert.NotNull(error);
        Assert.Contains("dimensions", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Plumbing — valid schema
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_PlumbingValidSchema_ReturnsNull()
    {
        var json = """{"pipeDiameter":25.4,"fluidType":"water"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Plumbing);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Plumbing — missing pipeDiameter property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_PlumbingMissingPipeDiameter_ReturnsError()
    {
        var json = """{"fluidType":"water"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Plumbing);

        Assert.NotNull(error);
        Assert.Contains("pipeDiameter", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Plumbing — missing fluidType property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_PlumbingMissingFluidType_ReturnsError()
    {
        var json = """{"pipeDiameter":25.4}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Plumbing);

        Assert.NotNull(error);
        Assert.Contains("fluidType", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Electrical — valid schema
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElectricalValidSchema_ReturnsNull()
    {
        var json = """{"voltage":230,"circuitCount":12}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Electrical);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Electrical — missing voltage property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElectricalMissingVoltage_ReturnsError()
    {
        var json = """{"circuitCount":12}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Electrical);

        Assert.NotNull(error);
        Assert.Contains("voltage", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Electrical — missing circuitCount property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElectricalMissingCircuitCount_ReturnsError()
    {
        var json = """{"voltage":230}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Electrical);

        Assert.NotNull(error);
        Assert.Contains("circuitCount", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Architecture — valid schema
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ArchitectureValidSchema_ReturnsNull()
    {
        var json = """{"floorPlan":"planA"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Architecture);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Architecture — missing floorPlan property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ArchitectureMissingFloorPlan_ReturnsError()
    {
        var json = """{"other":"value"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Architecture);

        Assert.NotNull(error);
        Assert.Contains("floorPlan", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Mep — valid schema
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MepValidSchema_ReturnsNull()
    {
        var json = """{"systems":["hvac","ventilation"]}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Mep);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Mep — missing systems property
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MepMissingSystems_ReturnsError()
    {
        var json = """{"other":"value"}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Mep);

        Assert.NotNull(error);
        Assert.Contains("systems", error);
    }

    // -------------------------------------------------------------------------
    // Generic (null TradeType) — valid JSON object → null
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_GenericNullTradeTypeValidObject_ReturnsNull()
    {
        var json = """{"anything":"goes"}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // Generic (null TradeType) — valid JSON array → null
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_GenericNullTradeTypeValidArray_ReturnsNull()
    {
        var json = """[1,2,3]""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // Generic — scalar JSON (not object or array) → error
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_GenericScalarJson_ReturnsError()
    {
        var json = """"42"""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
    }

    // -------------------------------------------------------------------------
    // Size > 64 KB → error before parse
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JsonExceedsMaxSize_ReturnsError()
    {
        // 65 537 characters → strictly over the 65 536 byte limit (all ASCII)
        var json = new string('x', 65_537);

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
        Assert.Contains("65536", error);
    }

    // -------------------------------------------------------------------------
    // Invalid JSON → error with parse message
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_InvalidJson_ReturnsError()
    {
        var json = "{not valid json}";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
        Assert.Contains("not valid JSON", error);
    }

    // -------------------------------------------------------------------------
    // TradeType.Joinery — root is not an object → error
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryRootIsArray_ReturnsError()
    {
        var json = """["oak"]""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Joinery);

        Assert.NotNull(error);
        Assert.Contains("object", error);
    }

    // -------------------------------------------------------------------------
    // parameters — nested object → error (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_NestedObjectInParameters_ReturnsError()
    {
        var json = """{"parameters": {"nested": {"a": 1}}}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
        Assert.Contains("parameters.nested", error);
    }

    // -------------------------------------------------------------------------
    // parameters — nested array → error (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ArrayValueInParameters_ReturnsError()
    {
        var json = """{"parameters": {"tags": ["a", "b"]}}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
        Assert.Contains("parameters.tags", error);
    }

    // -------------------------------------------------------------------------
    // parameters — scalar values only → null (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ScalarParametersOnly_ReturnsNull()
    {
        var json = """{"parameters": {"name": "test", "count": 42, "enabled": true, "ref": null}}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // parameters — null value → null (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_NullParameters_ReturnsNull()
    {
        var json = """{"parameters": null}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // parameters — absent key → null (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_AbsentParameters_ReturnsNull()
    {
        var json = """{"anything": "goes"}""";

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // parameters — 11 properties → error (maxProperties: 10) (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ElevenPropertiesInParameters_ReturnsError()
    {
        // 11 properties — exceeds maxProperties: 10
        var json = """
            {
                "parameters": {
                    "p1": "a", "p2": "b", "p3": "c", "p4": "d", "p5": "e",
                    "p6": "f", "p7": "g", "p8": "h", "p9": "i", "p10": "j",
                    "p11": "k"
                }
            }
            """;

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.NotNull(error);
        Assert.Contains("10", error);
    }

    // -------------------------------------------------------------------------
    // parameters — exactly 10 properties → null (boundary) (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_TenPropertiesInParameters_ReturnsNull()
    {
        // Exactly 10 properties — at the maxProperties boundary
        var json = """
            {
                "parameters": {
                    "p1": "a", "p2": "b", "p3": "c", "p4": "d", "p5": "e",
                    "p6": "f", "p7": "g", "p8": "h", "p9": "i", "p10": "j"
                }
            }
            """;

        var error = IntentDataSchemaValidator.Validate(json, tradeType: null);

        Assert.Null(error);
    }

    // -------------------------------------------------------------------------
    // parameters — nested object in Joinery trade type → error (T-06)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_JoineryNestedObjectInParameters_ReturnsError()
    {
        var json = """{"material":"oak","dimensions":{},"parameters":{"config":{"deep":1}}}""";

        var error = IntentDataSchemaValidator.Validate(json, TradeType.Joinery);

        Assert.NotNull(error);
        Assert.Contains("parameters.config", error);
    }
}
