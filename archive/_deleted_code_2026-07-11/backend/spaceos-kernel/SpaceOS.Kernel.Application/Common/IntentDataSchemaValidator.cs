// SpaceOS.Kernel.Application/Common/IntentDataSchemaValidator.cs
using System.Text.Json;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Validates the structural schema of an <c>IntentDataJson</c> string per <see cref="TradeType"/>.
/// All validation is performed with <see cref="JsonDocument"/> — no third-party schema library required.
/// </summary>
internal static class IntentDataSchemaValidator
{
    private const int MaxGenericDepth      = 10;
    private const int MaxGenericSizeBytes  = 65_536; // 64 KB
    private const int MaxParametersCount   = 10;     // maxProperties constraint on the "parameters" object

    /// <summary>
    /// Returns <c>null</c> when the JSON is structurally valid for the given trade type;
    /// returns a human-readable error message when validation fails.
    /// </summary>
    /// <param name="json">The raw JSON string to validate.</param>
    /// <param name="tradeType">
    /// Trade type that selects the structural schema. Pass <c>null</c> to apply generic rules.
    /// </param>
    public static string? Validate(string json, TradeType? tradeType)
    {
        if (json.Length > MaxGenericSizeBytes)
            return $"IntentDataJson exceeds maximum size of {MaxGenericSizeBytes} bytes.";

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return $"IntentDataJson is not valid JSON: {ex.Message}";
        }

        using (doc)
        {
            return tradeType switch
            {
                TradeType.Joinery      => ValidateJoinery(doc.RootElement),
                TradeType.Plumbing     => ValidatePlumbing(doc.RootElement),
                TradeType.Electrical   => ValidateElectrical(doc.RootElement),
                TradeType.Architecture => ValidateArchitecture(doc.RootElement),
                TradeType.Mep          => ValidateMep(doc.RootElement),
                _                      => ValidateGeneric(doc.RootElement),
            };
        }
    }

    // -------------------------------------------------------------------------
    // Per-type validators
    // -------------------------------------------------------------------------

    private static string? ValidateJoinery(JsonElement el) =>
        RequireObject(el)
        ?? ValidateParameters(el)
        ?? RequireStringProperty(el, "material")
        ?? RequireObjectProperty(el, "dimensions");

    private static string? ValidatePlumbing(JsonElement el) =>
        RequireObject(el)
        ?? ValidateParameters(el)
        ?? RequireNumberProperty(el, "pipeDiameter")
        ?? RequireStringProperty(el, "fluidType");

    private static string? ValidateElectrical(JsonElement el) =>
        RequireObject(el)
        ?? ValidateParameters(el)
        ?? RequireNumberProperty(el, "voltage")
        ?? RequireNumberProperty(el, "circuitCount");

    private static string? ValidateArchitecture(JsonElement el) =>
        RequireObject(el)
        ?? ValidateParameters(el)
        ?? RequireStringProperty(el, "floorPlan");

    private static string? ValidateMep(JsonElement el) =>
        RequireObject(el)
        ?? ValidateParameters(el)
        ?? RequireArrayProperty(el, "systems");

    private static string? ValidateGeneric(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object && el.ValueKind != JsonValueKind.Array)
            return "IntentDataJson must be a JSON object or array.";

        if (GetDepth(el) > MaxGenericDepth)
            return $"IntentDataJson exceeds maximum nesting depth of {MaxGenericDepth}.";

        if (el.ValueKind == JsonValueKind.Object)
            return ValidateParameters(el);

        return null;
    }

    // -------------------------------------------------------------------------
    // Parameters object validation (cross-cutting — all trade types)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates the optional top-level <c>parameters</c> property when present.
    /// <list type="bullet">
    ///   <item>Must be a JSON object (if present and non-null).</item>
    ///   <item>Must not exceed <see cref="MaxParametersCount"/> properties (maxProperties: 10).</item>
    ///   <item>Each property value must be a scalar: string, number, boolean, or null.
    ///         Nested objects and arrays are rejected.</item>
    /// </list>
    /// Returns <c>null</c> when the constraint is satisfied or the key is absent / null.
    /// </summary>
    private static string? ValidateParameters(JsonElement el)
    {
        if (!el.TryGetProperty("parameters", out var parameters))
            return null; // "parameters" key absent — valid

        if (parameters.ValueKind == JsonValueKind.Null)
            return null; // explicit null — valid

        if (parameters.ValueKind != JsonValueKind.Object)
            return "IntentDataJson 'parameters' must be a JSON object.";

        var count = 0;
        foreach (var prop in parameters.EnumerateObject())
        {
            count++;
            if (count > MaxParametersCount)
                return $"IntentDataJson 'parameters' must not exceed {MaxParametersCount} properties.";

            var kind = prop.Value.ValueKind;
            if (kind == JsonValueKind.Object || kind == JsonValueKind.Array)
                return $"IntentDataJson 'parameters.{prop.Name}' must be a scalar value (string, number, boolean, or null), not an object or array.";
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Primitive checks
    // -------------------------------------------------------------------------

    private static string? RequireObject(JsonElement el) =>
        el.ValueKind != JsonValueKind.Object
            ? "IntentDataJson must be a JSON object."
            : null;

    private static string? RequireStringProperty(JsonElement el, string name) =>
        !el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String
            ? $"IntentDataJson must contain a string property '{name}'."
            : null;

    private static string? RequireNumberProperty(JsonElement el, string name) =>
        !el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number
            ? $"IntentDataJson must contain a numeric property '{name}'."
            : null;

    private static string? RequireObjectProperty(JsonElement el, string name) =>
        !el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Object
            ? $"IntentDataJson must contain an object property '{name}'."
            : null;

    private static string? RequireArrayProperty(JsonElement el, string name) =>
        !el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array
            ? $"IntentDataJson must contain an array property '{name}'."
            : null;

    // -------------------------------------------------------------------------
    // Depth helper
    // -------------------------------------------------------------------------

    private static int GetDepth(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            var max = 0;
            foreach (var prop in el.EnumerateObject())
            {
                var d = GetDepth(prop.Value);
                if (d > max) max = d;
            }
            return max + 1;
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            var max = 0;
            foreach (var item in el.EnumerateArray())
            {
                var d = GetDepth(item);
                if (d > max) max = d;
            }
            return max + 1;
        }

        return 0;
    }
}
