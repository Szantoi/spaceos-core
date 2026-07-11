// SpaceOS.Modules.Abstractions/Actors/INodeUrlValidator.cs
namespace SpaceOS.Modules.Abstractions.Actors;

/// <summary>
/// Contract for validating a node server URL before it is persisted or used in
/// outbound HTTP calls — primary SSRF-prevention boundary.
/// </summary>
public interface INodeUrlValidator
{
    /// <summary>
    /// Validates that <paramref name="serverUrl"/> is a well-formed, non-private,
    /// non-loopback HTTPS URL suitable for inter-node communication.
    /// </summary>
    /// <param name="serverUrl">The URL to validate.</param>
    /// <returns>
    /// <c>null</c> when the URL is valid; otherwise a human-readable error message
    /// describing why the URL was rejected.
    /// </returns>
    string? Validate(string serverUrl);
}
