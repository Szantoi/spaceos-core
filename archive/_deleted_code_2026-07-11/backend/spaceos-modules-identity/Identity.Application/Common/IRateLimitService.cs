// Identity.Application/Common/IRateLimitService.cs

namespace Identity.Application.Common;

public interface IRateLimitService
{
    /// <summary>
    /// Increments the counter for the given key and checks if the limit is exceeded.
    /// Returns true if the action is allowed, false if rate limited.
    /// </summary>
    Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken ct = default);
}
