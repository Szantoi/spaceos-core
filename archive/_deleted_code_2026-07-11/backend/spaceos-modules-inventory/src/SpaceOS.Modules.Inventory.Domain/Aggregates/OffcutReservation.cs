using SpaceOS.Modules.Inventory.Domain.Common;
using SpaceOS.Modules.Inventory.Domain.Enums;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

public class OffcutReservation : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid OffcutId { get; private set; }
    public Guid JobId { get; private set; }
    public Guid TenantId { get; private set; }
    public OffcutReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    private OffcutReservation() { }

    public static OffcutReservation Create(Guid offcutId, Guid jobId, Guid tenantId)
    {
        if (offcutId == Guid.Empty) throw new ArgumentException("OffcutId required.", nameof(offcutId));
        if (jobId == Guid.Empty) throw new ArgumentException("JobId required.", nameof(jobId));
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));

        return new OffcutReservation
        {
            Id = Guid.NewGuid(),
            OffcutId = offcutId,
            JobId = jobId,
            TenantId = tenantId,
            Status = OffcutReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(DefaultExpiry)
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public void Approve()
    {
        if (Status != OffcutReservationStatus.Pending)
            throw new InvalidOperationException($"Cannot approve reservation with status {Status}.");
        if (IsExpired)
            throw new InvalidOperationException("Cannot approve an expired reservation.");
        Status = OffcutReservationStatus.Approved;
    }

    public void Cancel()
    {
        if (Status == OffcutReservationStatus.Cancelled)
            throw new InvalidOperationException("Reservation is already cancelled.");
        Status = OffcutReservationStatus.Cancelled;
    }
}
