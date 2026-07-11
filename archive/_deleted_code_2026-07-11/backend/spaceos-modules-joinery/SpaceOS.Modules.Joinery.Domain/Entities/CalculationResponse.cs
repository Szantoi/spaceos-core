namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Represents the response returned by the Orchestrator calculation endpoint.
/// </summary>
/// <param name="Success">Indicates whether the calculation succeeded.</param>
/// <param name="ErrorMessage">Error message if the calculation failed; null on success.</param>
public sealed record CalculationResponse(bool Success, string? ErrorMessage);
