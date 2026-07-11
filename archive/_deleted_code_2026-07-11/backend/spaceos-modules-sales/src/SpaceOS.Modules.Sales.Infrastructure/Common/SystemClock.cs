using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Infrastructure.Common;

/// <summary>Production IClock implementation that reads the real UTC wall-clock time.</summary>
internal sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
