// SpaceOS.Infrastructure/Crypto/SyncSignalHasher.cs
using System.Security.Cryptography;
using System.Text;
using SpaceOS.Kernel.Application.Sync;
using SpaceOS.Modules.Abstractions.Crypto;

namespace SpaceOS.Infrastructure.Crypto;

/// <summary>
/// Computes HMAC-SHA256 hashes for the SyncSignal append-only chain.
/// The key is sourced from <see cref="IKeyVaultService.GetSigningKeyAsync"/> and
/// resolved once at construction time (singleton lifetime).
/// </summary>
internal sealed class SyncSignalHasher : ISyncSignalHasher
{
    private readonly byte[] _key;

    /// <summary>
    /// Initialises the hasher by resolving the signing key from <paramref name="keyVaultService"/>.
    /// </summary>
    /// <param name="keyVaultService">The key vault used to retrieve the HMAC key.</param>
    public SyncSignalHasher(IKeyVaultService keyVaultService)
    {
        ArgumentNullException.ThrowIfNull(keyVaultService);
        _key = keyVaultService.GetSigningKeyAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Computes an HMAC-SHA256 hex hash over the concatenated chain fields.
    /// Input format: <c>{previousHash}:{payloadJson}:{occurredAt:O}</c>
    /// </summary>
    /// <param name="previousHash">The hash of the preceding entry in the chain.</param>
    /// <param name="payloadJson">The JSON-serialised payload of the signal.</param>
    /// <param name="occurredAt">The UTC timestamp of the signal.</param>
    /// <returns>Lowercase hex-encoded HMAC-SHA256 digest.</returns>
    public string ComputeHash(string previousHash, string payloadJson, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(previousHash);
        ArgumentNullException.ThrowIfNull(payloadJson);

        var input = $"{previousHash}:{payloadJson}:{occurredAt:O}";
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = HMACSHA256.HashData(_key, inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
