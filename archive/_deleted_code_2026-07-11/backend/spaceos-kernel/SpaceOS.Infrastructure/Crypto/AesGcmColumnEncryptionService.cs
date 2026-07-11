// SpaceOS.Infrastructure/Crypto/AesGcmColumnEncryptionService.cs
using System.Security.Cryptography;
using System.Text;
using SpaceOS.Modules.Abstractions.Crypto;

namespace SpaceOS.Infrastructure.Crypto;

/// <summary>
/// AES-256-GCM implementation of <see cref="IColumnEncryptionService"/>.
/// Each call to <see cref="Encrypt"/> generates a fresh 12-byte nonce so
/// that no two ciphertexts share the same (key, nonce) pair.
/// </summary>
/// <remarks>
/// Wire format: <c>{base64_nonce}:{base64_tag}:{base64_ciphertext}</c>
/// <list type="bullet">
///   <item>Nonce — 12 bytes (AES-GCM standard)</item>
///   <item>Tag   — 16 bytes (full authentication tag)</item>
///   <item>Ciphertext — same length as plaintext</item>
/// </list>
/// </remarks>
internal sealed class AesGcmColumnEncryptionService : IColumnEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Initialises the service by resolving the AES key from <paramref name="keyVaultService"/>
    /// synchronously. Intended for singleton lifetime — the key is resolved once at startup.
    /// </summary>
    /// <param name="keyVaultService">The key vault used to retrieve the encryption key.</param>
    public AesGcmColumnEncryptionService(IKeyVaultService keyVaultService)
    {
        ArgumentNullException.ThrowIfNull(keyVaultService);
        _key = keyVaultService.GetEncryptionKeyAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    /// <inheritdoc/>
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        var parts = ciphertext.Split(':');
        if (parts.Length != 3)
            throw new CryptographicException("Invalid encrypted format — expected nonce:tag:ciphertext.");

        var nonce      = Convert.FromBase64String(parts[0]);
        var tag        = Convert.FromBase64String(parts[1]);
        var cipherBytes = Convert.FromBase64String(parts[2]);
        var plaintext  = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipherBytes, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
