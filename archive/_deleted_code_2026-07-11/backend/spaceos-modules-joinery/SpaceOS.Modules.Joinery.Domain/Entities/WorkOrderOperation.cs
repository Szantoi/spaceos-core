using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Work order operation (assembly step) for production planning.
/// Supports drag-and-drop reordering with optimistic locking.
/// </summary>
public sealed class WorkOrderOperation : TenantScopedEntity
{
    public Guid WorkOrderId { get; private set; }
    public int Sequence { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TimeSpan EstimatedDuration { get; private set; }
    public string OperationType { get; private set; } = string.Empty;
    public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Navigation
    public WorkOrder? WorkOrder { get; private set; }

    private WorkOrderOperation() { } // EF Core

    public static WorkOrderOperation Create(
        Guid tenantId,
        Guid workOrderId,
        int sequence,
        string description,
        TimeSpan estimatedDuration,
        string operationType)
    {
        return new WorkOrderOperation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkOrderId = workOrderId,
            Sequence = sequence,
            Description = description,
            EstimatedDuration = estimatedDuration,
            OperationType = operationType,
            LastModified = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates the sequence number and last modified timestamp.
    /// Used for drag-and-drop reordering.
    /// </summary>
    public void UpdateSequence(int newSequence)
    {
        Sequence = newSequence;
        LastModified = DateTimeOffset.UtcNow;
    }
}
