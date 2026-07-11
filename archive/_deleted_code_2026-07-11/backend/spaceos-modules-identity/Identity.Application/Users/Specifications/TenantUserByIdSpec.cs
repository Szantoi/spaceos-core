// Identity.Application/Users/Specifications/TenantUserByIdSpec.cs

using Ardalis.Specification;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Users.Specifications;

public sealed class TenantUserByIdSpec : Specification<SpaceOSUser>
{
    public TenantUserByIdSpec(SpaceOSUserId id)
    {
        Query.Where(u => u.Id == id).AsNoTracking();
    }
}
