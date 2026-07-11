using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Tests.Helpers;

/// <summary>Deterministic IClock for unit tests.</summary>
public sealed class FakeClock(DateTimeOffset? fixedTime = null) : IClock
{
    public DateTimeOffset UtcNow { get; set; } =
        fixedTime ?? new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
