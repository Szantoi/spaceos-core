// Identity.Application/Common/IKcSyncOutboxProcessor.cs

namespace Identity.Application.Common;

public interface IKcSyncOutboxProcessor
{
    Task<IReadOnlyList<KcSyncOutboxEntry>> GetPendingAsync(
        int maxAttempts, int limit, CancellationToken ct = default);

    Task MarkProcessedAsync(Guid entryId, CancellationToken ct = default);

    Task IncrementAttemptAsync(Guid entryId, CancellationToken ct = default);
}
