using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Services;

/// <summary>
/// Service for generating work order PDF documents.
/// </summary>
public interface IWorkOrderPdfService
{
    /// <summary>
    /// Generates a PDF for the work order and returns the URL.
    /// </summary>
    Task<string> GenerateWorkOrderPdfAsync(WorkOrder workOrder, ProductConfiguration config, CancellationToken ct);

    /// <summary>
    /// Gets the PDF stream for a work order.
    /// </summary>
    Task<Stream?> GetWorkOrderPdfStreamAsync(Guid workOrderId, Guid tenantId, CancellationToken ct);
}
