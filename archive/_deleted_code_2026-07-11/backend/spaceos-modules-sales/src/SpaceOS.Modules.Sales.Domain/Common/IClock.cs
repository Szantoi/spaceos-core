namespace SpaceOS.Modules.Sales.Domain.Common;

/// <summary>Abstraction over system time to enable deterministic testing.</summary>
public interface IClock
{
    /// <summary>Returns the current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
