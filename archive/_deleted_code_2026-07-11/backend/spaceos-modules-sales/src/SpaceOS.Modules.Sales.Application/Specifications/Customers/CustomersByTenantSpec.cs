using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.Specifications.Customers;

/// <summary>Filtered, paged list of non-archived customers for a tenant.</summary>
public sealed class CustomersByTenantSpec : Specification<Customer>
{
    public CustomersByTenantSpec(
        Guid tenantId, CustomerStatus? status, string? search, int skip, int take)
    {
        Query.Where(c => c.TenantId == tenantId && !c.IsArchived).AsNoTracking();
        if (status is not null)
            Query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            Query.Where(c => c.DisplayName.Contains(search) || c.ContactName.Contains(search));
        Query.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(take);
    }
}
