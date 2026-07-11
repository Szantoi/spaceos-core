// SpaceOS.Infrastructure/Auth/AzureKeyVaultRsaPublicKeyProvider.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Production RSA public key provider.
/// Currently reads from <c>Jwt:RsaPublicKeyPem</c> configuration (environment variable or Key Vault reference).
/// Azure Key Vault SDK integration is planned for Sprint 1.
/// </summary>
internal sealed class AzureKeyVaultRsaPublicKeyProvider : IRsaPublicKeyProvider
{
    private readonly string _pem;

    /// <summary>Initialises a new <see cref="AzureKeyVaultRsaPublicKeyProvider"/>.</summary>
    /// <param name="configuration">The application configuration.</param>
    public AzureKeyVaultRsaPublicKeyProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _pem = configuration["Jwt:RsaPublicKeyPem"]
            ?? throw new InvalidOperationException(
                "Jwt:RsaPublicKeyPem is not configured. Set via environment variable or Key Vault reference.");
    }

    /// <inheritdoc/>
    public Task<RSA> GetPublicKeyAsync(CancellationToken ct = default)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_pem);
        return Task.FromResult(rsa);
    }
}
