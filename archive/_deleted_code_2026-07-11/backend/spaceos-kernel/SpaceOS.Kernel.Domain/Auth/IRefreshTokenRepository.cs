// SpaceOS.Kernel.Domain/Auth/IRefreshTokenRepository.cs

namespace SpaceOS.Kernel.Domain.Auth;

/// <summary>
/// Repository for <see cref="RefreshToken"/> persistence operations.
/// The interface lives in Domain so Application CQRS handlers can depend on it
/// without violating the Domain ← Application ← Infrastructure dependency rule.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves an active (non-revoked, non-expired) refresh token by its SHA-256 hash.
    /// Returns <c>null</c> if no matching active token exists.
    /// </summary>
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>
    /// Retrieves a refresh token by its SHA-256 hash regardless of revocation or expiry status.
    /// Returns <c>null</c> if no token with this hash exists.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>Persists a new <see cref="RefreshToken"/> entity.</summary>
    Task AddAsync(RefreshToken token, CancellationToken ct);

    /// <summary>Persists changes to an existing <see cref="RefreshToken"/> entity (e.g. revocation).</summary>
    Task UpdateAsync(RefreshToken token, CancellationToken ct);
}
