using System.Text.RegularExpressions;

namespace SpaceOS.Modules.Contracts.Inventory.Validation;

/// <summary>
/// Shared validation patterns for ConsumerContextJson (SEC-07, SEC-09).
/// Implementations MUST apply all three layers: schema check, XSS regex, PII regex.
/// TRUSTED ONLY — no user input, no PII, no secrets permitted.
/// </summary>
public static partial class ConsumerContextJsonSchema
{
    /// <summary>Maximum allowed byte size of the serialised ConsumerContextJson payload (4 KB).</summary>
    public const int MaxSizeBytes = 4000;

    /// <summary>Maximum allowed nesting depth of the JSON document.</summary>
    public const int MaxDepth = 5;

    /// <summary>
    /// XSS and script-injection pattern (SEC-07, Layer 2).
    /// Rejects payloads containing HTML tags, javascript: or data: URIs, or inline event handlers.
    /// </summary>
    public static readonly Regex XssPattern =
        new(@"(<|>|javascript:|data:|on[a-z]+=)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// PII detection pattern (SEC-09, Layer 2).
    /// Rejects payloads that appear to contain e-mail addresses or bearer token strings.
    /// </summary>
    public static readonly Regex PiiPattern =
        new(@"([a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}|bearer\s+[a-zA-Z0-9\-._~+/]+=*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Validates the raw JSON string against XSS and PII patterns.
    /// Returns true when the content is safe to store; false otherwise.
    /// </summary>
    /// <param name="rawJson">Serialised JSON payload to validate. May be null or empty.</param>
    /// <param name="violatedPattern">Set to the name of the first violated pattern, or null when valid.</param>
    /// <returns>True if the payload passes all checks; false if a violation is found.</returns>
    public static bool IsValid(string? rawJson, out string? violatedPattern)
    {
        violatedPattern = null;

        if (string.IsNullOrWhiteSpace(rawJson))
            return true;

        if (rawJson.Length > MaxSizeBytes)
        {
            violatedPattern = "MaxSizeBytes";
            return false;
        }

        if (XssPattern.IsMatch(rawJson))
        {
            violatedPattern = "XssPattern";
            return false;
        }

        if (PiiPattern.IsMatch(rawJson))
        {
            violatedPattern = "PiiPattern";
            return false;
        }

        return true;
    }
}
