// Identity.Domain/Interfaces/ISpaceOSUserRepository.cs

using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Interfaces;

public interface ISpaceOSUserRepository
{
    Task<SpaceOSUser?> GetByIdAsync(SpaceOSUserId id, CancellationToken ct = default);
    Task<SpaceOSUser?> GetByEmailAsync(Email email, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaceOSUser>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(SpaceOSUser user, CancellationToken ct = default);
    Task UpdateAsync(SpaceOSUser user, CancellationToken ct = default);
}
