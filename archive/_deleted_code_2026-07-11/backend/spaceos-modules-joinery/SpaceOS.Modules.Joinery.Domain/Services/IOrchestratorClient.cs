using Ardalis.Result;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Abstraction for sending calculation requests to the Orchestrator service.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IOrchestratorClient
{
    /// <summary>
    /// Sends a calculation request for the given outbox entry to the Orchestrator.
    /// </summary>
    /// <param name="entry">The outbox entry containing the payload to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success with a <see cref="CalculationResponse"/> on success; error details otherwise.</returns>
    Task<Result<CalculationResponse>> CalculateAsync(JoineryOutboxEntry entry, CancellationToken ct);
}
