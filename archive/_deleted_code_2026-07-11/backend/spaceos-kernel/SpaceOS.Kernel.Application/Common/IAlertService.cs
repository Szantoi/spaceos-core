// SpaceOS.Kernel.Application/Common/IAlertService.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Sends operational alerts triggered by anomaly detection or chain verification failures.
/// Implementations must be non-throwing — a delivery failure must not propagate exceptions
/// to the calling anomaly detector.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Sends an alert of the given type with the provided message.
    /// </summary>
    /// <param name="alertType">A short identifier for the alert category (e.g. "AuditGap", "ChainBreak").</param>
    /// <param name="message">A human-readable description of the alert condition.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAlertAsync(string alertType, string message, CancellationToken ct = default);
}
