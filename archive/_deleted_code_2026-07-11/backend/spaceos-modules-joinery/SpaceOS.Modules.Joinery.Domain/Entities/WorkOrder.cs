using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Work order (Gyártási lap) entity for production scheduling.
/// Links to a product configuration and stores production details.
/// </summary>
public sealed class WorkOrder : TenantScopedEntity
{
    public Guid ConfigurationId { get; private set; }
    public int Quantity { get; private set; }
    public DateOnly DeliveryDate { get; private set; }
    public string? CustomerRef { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Full BOM items with inventory status as JSON array.
    /// </summary>
    public string BomItems { get; private set; } = "[]";

    public decimal TotalMaterialCost { get; private set; }
    public decimal EstimatedLabor { get; private set; }
    public decimal TotalCost { get; private set; }

    public DateOnly ScheduledStart { get; private set; }
    public DateOnly EstimatedCompletion { get; private set; }

    public string? PdfUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; private set; }

    // Assembly operations collection
    private readonly List<WorkOrderOperation> _operations = new();
    public IReadOnlyList<WorkOrderOperation> Operations => _operations.AsReadOnly();

    private WorkOrder() { } // EF Core

    public static WorkOrder Create(
        Guid tenantId,
        Guid configurationId,
        int quantity,
        DateOnly deliveryDate,
        string? customerRef,
        string? notes,
        string bomItemsJson,
        decimal totalMaterialCost,
        decimal estimatedLabor,
        decimal totalCost,
        DateOnly scheduledStart,
        DateOnly estimatedCompletion,
        Guid? createdBy)
    {
        return new WorkOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigurationId = configurationId,
            Quantity = quantity,
            DeliveryDate = deliveryDate,
            CustomerRef = customerRef,
            Notes = notes,
            BomItems = bomItemsJson,
            TotalMaterialCost = totalMaterialCost,
            EstimatedLabor = estimatedLabor,
            TotalCost = totalCost,
            ScheduledStart = scheduledStart,
            EstimatedCompletion = estimatedCompletion,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void SetPdfUrl(string pdfUrl) => PdfUrl = pdfUrl;
}
