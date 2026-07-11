// SpaceOS.Infrastructure/Auth/PemFileRsaPublicKeyProvider.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Loads the RSA public key from a PEM-encoded value in configuration.
/// Used in development and testing. The PEM is read from <c>Jwt:RsaPublicKeyPem</c>.
/// Falls back to <see cref="DevRsaKeyManager"/> when the configuration value is absent or empty.
/// </summary>
internal sealed class PemFileRsaPublicKeyProvider : IRsaPublicKeyProvider
{
    private readonly string _pem;

    /// <summary>Initialises a new <see cref="PemFileRsaPublicKeyProvider"/>.</summary>
    /// <param name="configuration">The application configuration.</param>
    public PemFileRsaPublicKeyProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _pem = configuration["Jwt:RsaPublicKeyPem"] ?? string.Empty;
    }

    /// <inheritdoc/>
    public Task<RSA> GetPublicKeyAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_pem))
        {
            // Dev fallback: export only the public parameters from the auto-generated key.
            var rsa = RSA.Create();
            rsa.ImportParameters(DevRsaKeyManager.Instance.ExportParameters(includePrivateParameters: false));
            return Task.FromResult(rsa);
        }

        var rsaFromPem = RSA.Create();
        rsaFromPem.ImportFromPem(_pem);
        return Task.FromResult(rsaFromPem);
    }
}
