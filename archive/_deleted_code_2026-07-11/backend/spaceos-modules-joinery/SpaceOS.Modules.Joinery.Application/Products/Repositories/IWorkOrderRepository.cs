using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Repositories;

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<WorkOrder?> GetWithOperationsAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(WorkOrder workOrder, CancellationToken ct);
    Task UpdateAsync(WorkOrder workOrder, CancellationToken ct);
    Task<List<WorkOrderOperation>> GetOperationsByWorkOrderIdAsync(Guid workOrderId, Guid tenantId, CancellationToken ct);
    Task UpdateOperationsAsync(List<WorkOrderOperation> operations, CancellationToken ct);
}
