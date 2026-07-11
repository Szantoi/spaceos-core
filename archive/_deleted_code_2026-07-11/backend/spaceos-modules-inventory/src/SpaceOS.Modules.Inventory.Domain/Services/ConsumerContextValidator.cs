using System.Text.Json;
using System.Text.RegularExpressions;
using Ardalis.Result;

namespace SpaceOS.Modules.Inventory.Domain.Services;

/// <summary>
/// Validates the <c>ConsumerContextJson</c> field of a reservation (I-11).
/// Checks:
/// <list type="bullet">
///   <item>Valid JSON (if not null/empty).</item>
///   <item>No XSS payloads: <c>&lt;</c>, <c>&gt;</c>, <c>javascript:</c>, <c>data:</c>, inline event handlers.</item>
///   <item>No PII: email addresses or Bearer tokens.</item>
/// </list>
/// </summary>
public sealed class ConsumerContextValidator
{
    // XSS patterns: angle brackets, javascript: URI, data: URI, on<event>= inline handlers
    private static readonly Regex XssPattern = new(
        @"(<|>|javascript:|data:|on[a-z]+=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // PII patterns
    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex BearerPattern = new(
        @"Bearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates <paramref name="contextJson"/>.
    /// Returns <see cref="Result.Success()"/> when valid or when the value is null/empty.
    /// Returns <see cref="Result.Invalid(ValidationError[])"/> with a descriptive error otherwise.
    /// </summary>
    public Result Validate(string? contextJson)
    {
        if (string.IsNullOrEmpty(contextJson))
            return Result.Success();

        // Must be valid JSON
        try
        {
            using var doc = JsonDocument.Parse(contextJson);
        }
        catch (JsonException)
        {
            return Result.Invalid(new ValidationError("ConsumerContextJson must be valid JSON."));
        }

        // XSS check
        if (XssPattern.IsMatch(contextJson))
            return Result.Invalid(new ValidationError("ConsumerContextJson contains disallowed characters or patterns (XSS)."));

        // PII check: email
        if (EmailPattern.IsMatch(contextJson))
            return Result.Invalid(new ValidationError("ConsumerContextJson must not contain email addresses (PII)."));

        // PII check: Bearer token
        if (BearerPattern.IsMatch(contextJson))
            return Result.Invalid(new ValidationError("ConsumerContextJson must not contain Bearer tokens (PII)."));

        return Result.Success();
    }
}
