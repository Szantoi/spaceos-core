using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Domain.Aggregates;

namespace SpaceOS.Modules.Joinery.Application.Orders.Repositories;

/// <summary>Counts of records deleted during a tenant reset operation.</summary>
public sealed record TenantDeletedCounts(int DoorOrders, int CuttingListSnapshots);

public interface IDoorOrderRepository
{
    Task<DoorOrder?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(DoorOrder order, CancellationToken ct);
    Task UpdateAsync(DoorOrder order, CancellationToken ct);
    Task<(IReadOnlyList<DoorOrderDto> Items, int TotalCount)> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Deletes all DoorOrders (and cascaded CuttingListSnapshots) for the given tenant.
    /// Used exclusively by the internal test-reset endpoint (BE-TEST-03).
    /// </summary>
    Task<TenantDeletedCounts> DeleteAllByTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Finds an existing order by its originating quote ID for idempotency checks.
    /// </summary>
    Task<DoorOrder?> FindBySourceQuoteIdAsync(Guid tenantId, Guid sourceQuoteId, CancellationToken ct);
}
