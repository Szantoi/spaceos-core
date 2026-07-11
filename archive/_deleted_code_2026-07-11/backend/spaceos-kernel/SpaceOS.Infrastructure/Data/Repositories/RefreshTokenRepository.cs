// SpaceOS.Infrastructure/Data/Repositories/RefreshTokenRepository.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Auth;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRefreshTokenRepository"/>.
/// Uses <see cref="AppDbContext"/> — refresh tokens are not audit-sensitive
/// and do not require the restricted <c>spaceos_audit_writer</c> role.
/// </summary>
internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    /// <summary>Initialises a new <see cref="RefreshTokenRepository"/>.</summary>
    public RefreshTokenRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var now = DateTimeOffset.UtcNow;
        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash
                   && rt.RevokedAt == null
                   && rt.ExpiresAt > now,
                ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(token);
        await _context.RefreshTokens.AddAsync(token, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(RefreshToken token, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(token);
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }
}
