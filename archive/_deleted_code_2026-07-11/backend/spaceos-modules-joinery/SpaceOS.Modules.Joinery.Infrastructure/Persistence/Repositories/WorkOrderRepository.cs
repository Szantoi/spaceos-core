using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

public sealed class WorkOrderRepository : IWorkOrderRepository
{
    private readonly JoineryDbContext _context;

    public WorkOrderRepository(JoineryDbContext context)
    {
        _context = context;
    }

    public async Task<WorkOrder?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _context.WorkOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(WorkOrder workOrder, CancellationToken ct)
    {
        await _context.WorkOrders.AddAsync(workOrder, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(WorkOrder workOrder, CancellationToken ct)
    {
        _context.WorkOrders.Update(workOrder);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<WorkOrder?> GetWithOperationsAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _context.WorkOrders
            .Include(w => w.Operations)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct)
            .ConfigureAwait(false);
    }

    public async Task<List<WorkOrderOperation>> GetOperationsByWorkOrderIdAsync(Guid workOrderId, Guid tenantId, CancellationToken ct)
    {
        return await _context.Set<WorkOrderOperation>()
            .Where(op => op.WorkOrderId == workOrderId && op.TenantId == tenantId)
            .OrderBy(op => op.Sequence)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateOperationsAsync(List<WorkOrderOperation> operations, CancellationToken ct)
    {
        _context.Set<WorkOrderOperation>().UpdateRange(operations);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
