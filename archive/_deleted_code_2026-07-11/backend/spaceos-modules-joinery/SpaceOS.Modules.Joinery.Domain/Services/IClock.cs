namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Abstraction over wall-clock time. Keeps domain logic free of <c>DateTime.UtcNow</c> calls
/// and makes time-dependent tests deterministic.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTimeOffset UtcNow { get; }
}
