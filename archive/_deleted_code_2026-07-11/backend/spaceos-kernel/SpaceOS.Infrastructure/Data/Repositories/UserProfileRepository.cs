// SpaceOS.Infrastructure/Data/Repositories/UserProfileRepository.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.UserProfiles;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserProfileRepository"/>.
/// </summary>
internal sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _context;

    /// <summary>Initialises a new <see cref="UserProfileRepository"/>.</summary>
    /// <param name="context">The EF Core database context.</param>
    public UserProfileRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<UserProfile?> GetByExternalUserIdAsync(
        string externalUserId,
        Guid tenantId,
        CancellationToken ct = default) =>
        await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                up => up.ExternalUserId == externalUserId && up.TenantId == tenantId,
                ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(UserProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await _context.UserProfiles.AddAsync(profile, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(UserProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _context.UserProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
