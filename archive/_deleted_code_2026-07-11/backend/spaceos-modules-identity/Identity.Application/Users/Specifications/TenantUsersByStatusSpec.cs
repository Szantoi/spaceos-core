// Identity.Application/Users/Specifications/TenantUsersByStatusSpec.cs

using Ardalis.Specification;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Users.Specifications;

public sealed class TenantUsersByStatusSpec : Specification<SpaceOSUser>
{
    public TenantUsersByStatusSpec(Guid tenantId, UserStatus? status = null)
    {
        Query.Where(u => u.TenantId == tenantId);

        if (status.HasValue)
            Query.Where(u => u.Status == status.Value);

        Query.AsNoTracking();
    }
}
