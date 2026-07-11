using System.Text.Json;

namespace SpaceOS.Kernel.Application.Common;

/// <summary>Shared JSON validation utilities for FluentValidation rules.</summary>
internal static class JsonValidationHelper
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="json"/> is a non-empty, parseable JSON document.
    /// </summary>
    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}
