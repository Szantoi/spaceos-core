using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IClock"/> that delegates to <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
internal sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
