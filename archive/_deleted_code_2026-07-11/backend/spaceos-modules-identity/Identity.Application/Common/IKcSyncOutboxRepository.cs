// Identity.Application/Common/IKcSyncOutboxRepository.cs

namespace Identity.Application.Common;

public enum KcSyncOperation
{
    CreateUser,
    UpdateUser,
    DisableUser,
    EnableUser,
    ResetPassword
}

public sealed class KcSyncOutboxEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public KcSyncOperation Operation { get; init; }
    public string? Payload { get; init; }
    public int AttemptCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public interface IKcSyncOutboxRepository
{
    Task InsertAsync(KcSyncOutboxEntry entry, CancellationToken ct = default);
}
