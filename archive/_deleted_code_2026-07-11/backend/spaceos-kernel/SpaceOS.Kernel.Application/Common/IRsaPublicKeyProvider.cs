// SpaceOS.Kernel.Application/Common/IRsaPublicKeyProvider.cs

using System.Security.Cryptography;

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Provides the RSA public key used to validate JWT Bearer tokens.
/// Production implementations load from Azure Key Vault; development loads from a PEM file.
/// </summary>
public interface IRsaPublicKeyProvider
{
    /// <summary>Returns the RSA public key for JWT signature validation.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>An <see cref="RSA"/> instance containing the public key.</returns>
    Task<RSA> GetPublicKeyAsync(CancellationToken ct = default);
}
