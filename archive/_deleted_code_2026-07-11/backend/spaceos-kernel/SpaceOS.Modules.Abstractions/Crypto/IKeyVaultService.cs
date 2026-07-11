// SpaceOS.Modules.Abstractions/Crypto/IKeyVaultService.cs
namespace SpaceOS.Modules.Abstractions.Crypto;

/// <summary>
/// Retrieves cryptographic key material from a secure key store (e.g. Azure Key Vault).
/// All keys are returned as raw byte arrays so that callers remain independent of
/// any particular key serialisation format.
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Retrieves the current RSA private key used to sign inter-node JWTs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw key bytes.</returns>
    Task<byte[]> GetSigningKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current AES key used for column-level encryption.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw key bytes.</returns>
    Task<byte[]> GetEncryptionKeyAsync(CancellationToken ct = default);
}
