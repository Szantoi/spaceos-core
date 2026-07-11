using Ardalis.Result;
using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Core;

/// <summary>
/// Gyártásilap (Manufacturing Spec Sheet) aggregate — production floor PDF document
/// with 4 label variants (L1: Basic, L2: Premium+QR, L3: Barcode, L4: Full).
/// Immutable storage in MinIO WORM bucket; database tracks metadata + status.
/// </summary>
public sealed class Gyartasilap : TenantScopedEntity
{
    public Guid JoineryOrderId { get; private set; }
    public Guid? CuttingPlanId { get; private set; }
    public string Version { get; private set; } = "v1.0";

    /// <summary>PDF content (kept in DB for small-to-medium PDFs; large PDFs → MinIO only)</summary>
    public byte[]? PdfContent { get; private set; }

    /// <summary>MinIO WORM path: gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf</summary>
    public string? StorageUrl { get; private set; }

    /// <summary>Label variant used: L1 (Basic), L2 (Premium+QR), L3 (Barcode), L4 (Full)</summary>
    public string LabelVariant { get; private set; } = "L1";

    /// <summary>Status: Draft | Finalized | Archived</summary>
    public GyartasilapStatus Status { get; private set; } = GyartasilapStatus.Draft;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Gyartasilap() { } // EF Core

    /// <summary>Factory method: create a draft Gyártásilap</summary>
    public static Result<Gyartasilap> Create(
        Guid tenantId,
        Guid joynerOrderId,
        Guid? cuttingPlanId = null,
        string labelVariant = "L1")
    {
        if (joynerOrderId == Guid.Empty)
            return Result<Gyartasilap>.Invalid(new ValidationError("JoineryOrderId", "Order ID cannot be empty"));

        if (!IsValidLabelVariant(labelVariant))
            return Result<Gyartasilap>.Invalid(
                new ValidationError("LabelVariant", "Label variant must be L1, L2, L3, or L4"));

        var now = DateTimeOffset.UtcNow;
        return Result<Gyartasilap>.Success(new Gyartasilap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            JoineryOrderId = joynerOrderId,
            CuttingPlanId = cuttingPlanId,
            LabelVariant = labelVariant,
            Status = GyartasilapStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    /// <summary>Update PDF content and storage URL after successful generation</summary>
    public Result UpdateStorage(byte[]? pdfContent, string? storageUrl)
    {
        if (Status == GyartasilapStatus.Archived)
            return Result.Invalid(new ValidationError("Status", "Cannot update archived Gyártásilap"));

        PdfContent = pdfContent;
        StorageUrl = storageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>Transition from Draft to Finalized (immutable thereafter)</summary>
    public Result Finalize()
    {
        if (Status != GyartasilapStatus.Draft)
            return Result.Invalid(new ValidationError("Status", "Can only finalize Draft Gyártásilap"));

        Status = GyartasilapStatus.Finalized;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>Archive this Gyártásilap (for data retention; soft-delete pattern)</summary>
    public Result Archive()
    {
        if (Status == GyartasilapStatus.Archived)
            return Result.Invalid(new ValidationError("Status", "Gyártásilap is already archived"));

        Status = GyartasilapStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    private static bool IsValidLabelVariant(string variant) =>
        variant is "L1" or "L2" or "L3" or "L4";
}

/// <summary>Status enum for Gyártásilap lifecycle</summary>
public enum GyartasilapStatus
{
    Draft = 0,
    Finalized = 1,
    Archived = 2
}
