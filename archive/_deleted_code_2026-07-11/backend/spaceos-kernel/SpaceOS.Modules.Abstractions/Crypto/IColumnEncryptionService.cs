// SpaceOS.Modules.Abstractions/Crypto/IColumnEncryptionService.cs
namespace SpaceOS.Modules.Abstractions.Crypto;

/// <summary>
/// Provides symmetric column-level encryption for sensitive fields stored in the
/// database. Implementations are expected to use an authenticated encryption scheme
/// (e.g. AES-256-GCM) with a key sourced from <c>IKeyVaultService</c>.
/// </summary>
public interface IColumnEncryptionService
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and returns a Base64-encoded ciphertext
    /// string suitable for storage.
    /// </summary>
    /// <param name="plaintext">The sensitive value to encrypt.</param>
    /// <returns>Base64-encoded authenticated ciphertext.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a Base64-encoded ciphertext previously produced by
    /// <see cref="Encrypt"/> and returns the original plaintext value.
    /// </summary>
    /// <param name="ciphertext">The Base64-encoded authenticated ciphertext.</param>
    /// <returns>The original plaintext value.</returns>
    string Decrypt(string ciphertext);
}
