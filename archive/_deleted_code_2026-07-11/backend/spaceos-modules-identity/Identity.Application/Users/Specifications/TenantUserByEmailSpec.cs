// Identity.Application/Users/Specifications/TenantUserByEmailSpec.cs

using Ardalis.Specification;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Users.Specifications;

public sealed class TenantUserByEmailSpec : Specification<SpaceOSUser>
{
    public TenantUserByEmailSpec(Email email, Guid tenantId)
    {
        Query
            .Where(u => u.Email == email && u.TenantId == tenantId)
            .AsNoTracking();
    }
}
