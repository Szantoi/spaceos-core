using Ardalis.Result;
using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Core;

/// <summary>Lifecycle status of a batch ZIP generation job.</summary>
public enum BatchStatus { Pending = 0, Generating = 1, Ready = 2, Failed = 3 }

/// <summary>
/// Aggregate representing a ZIP batch of multiple Gyártásilap PDFs for a single order.
/// Created when the user requests bulk download; tracks generation status and storage path.
/// </summary>
public sealed class GyartasilapBatch : TenantScopedEntity
{
    public Guid OrderId { get; private set; }
    private List<Guid> _gyartasilapIds = [];
    public IReadOnlyList<Guid> GyartasilapIds => _gyartasilapIds.AsReadOnly();
    public BatchStatus Status { get; private set; }
    public string? ZipStoragePath { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private GyartasilapBatch() { } // EF Core

    /// <summary>
    /// Creates a new pending batch for the given gyartasilap IDs.
    /// </summary>
    public static Result<GyartasilapBatch> Create(
        Guid orderId,
        Guid tenantId,
        IReadOnlyList<Guid> ids)
    {
        if (orderId == Guid.Empty)
            return Result<GyartasilapBatch>.Invalid(
                new ValidationError("OrderId", "OrderId cannot be empty."));

        if (tenantId == Guid.Empty)
            return Result<GyartasilapBatch>.Invalid(
                new ValidationError("TenantId", "TenantId cannot be empty."));

        if (ids is null || ids.Count == 0)
            return Result<GyartasilapBatch>.Invalid(
                new ValidationError("GyartasilapIds", "At least one Gyártásilap ID is required."));

        return Result<GyartasilapBatch>.Success(new GyartasilapBatch
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TenantId = tenantId,
            _gyartasilapIds = ids.ToList(),
            Status = BatchStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Transitions the batch from Pending to Generating.</summary>
    public Result MarkGenerating()
    {
        if (Status != BatchStatus.Pending)
            return Result.Invalid(new ValidationError("Status",
                "Can only start generating from Pending status."));

        Status = BatchStatus.Generating;
        return Result.Success();
    }

    /// <summary>Marks the batch as Ready with the final ZIP storage path.</summary>
    public Result MarkReady(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            return Result.Invalid(new ValidationError("ZipStoragePath", "Zip storage path cannot be empty."));

        if (Status == BatchStatus.Ready)
            return Result.Invalid(new ValidationError("Status",
                "Batch is already marked as Ready."));

        Status = BatchStatus.Ready;
        ZipStoragePath = zipPath;
        CompletedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>Marks the batch as Failed (e.g. storage error).</summary>
    public Result MarkFailed()
    {
        if (Status == BatchStatus.Failed)
            return Result.Invalid(new ValidationError("Status",
                "Batch is already marked as Failed."));

        Status = BatchStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}
