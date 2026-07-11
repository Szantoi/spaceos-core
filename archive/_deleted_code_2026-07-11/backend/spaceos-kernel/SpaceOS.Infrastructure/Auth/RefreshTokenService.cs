// SpaceOS.Infrastructure/Auth/RefreshTokenService.cs

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Stateless helpers for generating and verifying opaque refresh tokens (BE-P15-05, BE-P15-12).
/// All members are pure and thread-safe — register as a static utility rather than a DI service.
/// </summary>
public static class RefreshTokenService
{
    /// <summary>
    /// Generates a cryptographically secure opaque token: 256-bit CSPRNG data, Base64Url-encoded.
    /// The resulting string is exactly 43 characters (no padding).
    /// The raw value is sent to the client and must never be stored — only its hash should be persisted.
    /// </summary>
    /// <returns>A 43-character Base64Url-encoded random token string.</returns>
    public static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes.ToArray());
    }

    /// <summary>
    /// Computes the SHA-256 hex digest of the given token for secure DB storage.
    /// The result is 64 lowercase hexadecimal characters.
    /// </summary>
    /// <param name="token">The raw opaque token received from the client.</param>
    /// <returns>A 64-character lowercase hex string representing the SHA-256 hash.</returns>
    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Performs a timing-safe comparison between the hash of an incoming token
    /// and a stored hash to prevent timing-attack-based token enumeration (BE-P15-12).
    /// </summary>
    /// <param name="incoming">The raw opaque token received from the client.</param>
    /// <param name="storedHash">The SHA-256 hex hash stored in the database.</param>
    /// <returns><c>true</c> if the hashes match; otherwise <c>false</c>.</returns>
    public static bool VerifyToken(string incoming, string storedHash)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(HashToken(incoming)),
            Encoding.UTF8.GetBytes(storedHash));
}
