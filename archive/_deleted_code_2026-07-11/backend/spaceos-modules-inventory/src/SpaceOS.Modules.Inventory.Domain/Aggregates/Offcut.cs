using SpaceOS.Modules.Inventory.Domain.Common;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

public class Offcut : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid MaterialCatalogId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;

    // Dimensions
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public decimal ThicknessMm { get; private set; }

    // Computed metrics (stored for query performance)
    public decimal VolumeM3 { get; private set; }
    public decimal WeightKg { get; private set; }

    // Origin
    public Guid? OriginCuttingSheetId { get; private set; }
    public Guid? CuttingJobId { get; private set; }

    // Lifecycle
    public OffcutStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public Guid? UsedInJobId { get; private set; }

    private Offcut() { }

    public static Offcut Register(
        Guid tenantId,
        Guid materialCatalogId,
        decimal widthMm,
        decimal heightMm,
        Guid? originCuttingSheetId)
        => Register(tenantId, materialCatalogId, string.Empty, widthMm, heightMm, 0m, 0m, 0m, originCuttingSheetId, null);

    public static Offcut Register(
        Guid tenantId,
        Guid materialCatalogId,
        string materialCode,
        decimal widthMm,
        decimal heightMm,
        decimal thicknessMm,
        decimal volumeM3,
        decimal weightKg,
        Guid? originCuttingSheetId,
        Guid? cuttingJobId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (materialCatalogId == Guid.Empty) throw new ArgumentException("MaterialCatalogId required.", nameof(materialCatalogId));
        if (widthMm <= 0) throw new ArgumentException("Width must be positive.", nameof(widthMm));
        if (heightMm <= 0) throw new ArgumentException("Height must be positive.", nameof(heightMm));

        var offcut = new Offcut
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaterialCatalogId = materialCatalogId,
            MaterialCode = materialCode,
            WidthMm = widthMm,
            HeightMm = heightMm,
            ThicknessMm = thicknessMm,
            VolumeM3 = volumeM3 > 0 ? volumeM3 : ComputeVolume(widthMm, heightMm, thicknessMm),
            WeightKg = weightKg,
            OriginCuttingSheetId = originCuttingSheetId,
            CuttingJobId = cuttingJobId,
            Status = OffcutStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        offcut.RaiseDomainEvent(new OffcutRegisteredEvent(offcut.Id, offcut.TenantId, offcut.MaterialCatalogId, offcut.WidthMm, offcut.HeightMm));
        return offcut;
    }

    public void Reserve()
    {
        if (Status != OffcutStatus.Available)
            throw new InvalidOperationException($"Cannot reserve offcut: status is {Status}.");
        Status = OffcutStatus.Reserved;
    }

    public void CancelReservation()
    {
        if (Status != OffcutStatus.Reserved)
            throw new InvalidOperationException($"Cannot cancel reservation: status is {Status}.");
        Status = OffcutStatus.Available;
    }

    public void MarkUsed(Guid jobId)
    {
        if (Status != OffcutStatus.Available && Status != OffcutStatus.Reserved)
            throw new InvalidOperationException($"Cannot mark offcut as Used when status is {Status}.");
        Status = OffcutStatus.Used;
        UsedAt = DateTime.UtcNow;
        UsedInJobId = jobId;
    }

    // Legacy overload — kept for backward compat with existing tests
    public void MarkUsed()
    {
        if (Status != OffcutStatus.Available)
            throw new InvalidOperationException($"Cannot mark offcut as Used when status is {Status}.");
        Status = OffcutStatus.Used;
        UsedAt = DateTime.UtcNow;
    }

    public void Scrap()
    {
        if (Status == OffcutStatus.Used)
            throw new InvalidOperationException("Cannot scrap a Used offcut.");
        Status = OffcutStatus.Scrapped;
    }

    // Legacy — kept for backward compat
    public void MarkWaste()
    {
        if (Status == OffcutStatus.Used)
            throw new InvalidOperationException("Cannot mark Used offcut as Waste.");
        Status = OffcutStatus.Waste;
    }

    public static decimal ComputeVolume(decimal widthMm, decimal heightMm, decimal thicknessMm)
        => thicknessMm <= 0 ? 0m : widthMm * heightMm * thicknessMm / 1_000_000_000m;
}
