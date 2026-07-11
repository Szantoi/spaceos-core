// SpaceOS.Kernel.Application/Auth/RefreshTokenHasher.cs

using System.Security.Cryptography;
using System.Text;

namespace SpaceOS.Kernel.Application.Auth;

/// <summary>
/// Pure BCL utility for generating and verifying opaque refresh tokens (BE-P15-05, BE-P15-12).
/// Lives in Application so CQRS handlers can use it without referencing Infrastructure.
/// No external NuGet dependencies — only BCL <c>System.Security.Cryptography</c>.
/// </summary>
public static class RefreshTokenHasher
{
    /// <summary>
    /// Generates a cryptographically secure opaque refresh token: 256-bit CSPRNG, Base64Url-encoded.
    /// Result is exactly 43 characters (no padding).
    /// </summary>
    /// <returns>A 43-character Base64Url-encoded random token string.</returns>
    public static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        // BCL Base64Url: replace + with -, / with _, remove padding
        return Convert.ToBase64String(bytes.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Computes the SHA-256 hex digest of the given token for secure DB storage.
    /// The result is 64 lowercase hexadecimal characters.
    /// </summary>
    /// <param name="token">The raw opaque token received from the client.</param>
    /// <returns>A 64-character lowercase hex string.</returns>
    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Timing-safe comparison between the hash of an incoming token and a stored hash (BE-P15-12).
    /// </summary>
    /// <param name="incoming">The raw opaque token received from the client.</param>
    /// <param name="storedHash">The SHA-256 hex hash stored in the database.</param>
    /// <returns><c>true</c> if hashes match; otherwise <c>false</c>.</returns>
    public static bool VerifyToken(string incoming, string storedHash)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(HashToken(incoming)),
            Encoding.UTF8.GetBytes(storedHash));
}
