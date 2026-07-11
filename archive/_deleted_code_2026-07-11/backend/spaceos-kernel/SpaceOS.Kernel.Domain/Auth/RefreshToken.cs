// SpaceOS.Kernel.Domain/Auth/RefreshToken.cs

namespace SpaceOS.Kernel.Domain.Auth;

/// <summary>
/// Represents an opaque refresh token issued to an authenticated user.
/// The raw token value is never stored — only its SHA-256 hex digest (<see cref="TokenHash"/>).
/// Supports token rotation: on each refresh the existing token is revoked and a new pair is issued.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>Gets the surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the user who owns this token.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the SHA-256 hex digest of the raw opaque token (64 lowercase hex chars).
    /// The plaintext token is never persisted.
    /// </summary>
    public string TokenHash { get; private set; } = default!;

    /// <summary>Gets the UTC timestamp at which this token expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp at which this token was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp at which this token was revoked, or <c>null</c> if still active.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Gets a value indicating whether this token has been explicitly revoked.</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>Gets a value indicating whether this token has passed its expiry time.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>Gets a value indicating whether this token is valid for use in a token refresh.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // EF Core parameterless constructor.
    private RefreshToken() { }

    /// <summary>
    /// Creates a new <see cref="RefreshToken"/> with a new surrogate ID and creation timestamp.
    /// </summary>
    /// <param name="userId">The user to whom the token belongs.</param>
    /// <param name="tokenHash">SHA-256 hex digest of the raw opaque token (64 lowercase hex chars).</param>
    /// <param name="expiresAt">Absolute expiry timestamp.</param>
    /// <returns>A new, unpersisted <see cref="RefreshToken"/> entity.</returns>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        return new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Marks this token as revoked by recording the current UTC time.
    /// Idempotent — calling on an already-revoked token has no effect.
    /// </summary>
    public void Revoke()
    {
        if (!IsRevoked)
            RevokedAt = DateTimeOffset.UtcNow;
    }
}
