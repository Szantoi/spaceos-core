// SpaceOS.Kernel.Application/Common/ICurrentRequestContext.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Provides access to the identity and network context of the current HTTP request.
/// Allows the Application layer to capture audit metadata without a direct HTTP dependency.
/// </summary>
public interface ICurrentRequestContext
{
    /// <summary>
    /// Gets the authenticated user's identifier (JWT <c>sub</c> claim), or <c>null</c>
    /// when the request is unauthenticated or originates from an internal system event.
    /// </summary>
    string? ActorId { get; }

    /// <summary>
    /// Gets the remote IP address of the caller, taking <c>X-Forwarded-For</c> into account,
    /// or <c>null</c> when not available (e.g. system-initiated events).
    /// </summary>
    string? SourceIp { get; }

    /// <summary>
    /// Gets the validated brand identifier from the <c>X-SpaceOS-Brand</c> header,
    /// or <c>null</c> when the header is missing or its value is not in the allowlist.
    /// </summary>
    string? SourceBrand { get; }
}
