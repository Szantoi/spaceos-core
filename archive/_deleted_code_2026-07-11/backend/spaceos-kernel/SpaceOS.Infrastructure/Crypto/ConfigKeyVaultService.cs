// SpaceOS.Infrastructure/Crypto/ConfigKeyVaultService.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SpaceOS.Modules.Abstractions.Crypto;

namespace SpaceOS.Infrastructure.Crypto;

/// <summary>
/// Configuration-based implementation of <see cref="IKeyVaultService"/> that reads
/// key material from application configuration (<c>Crypto:SigningKey</c> and
/// <c>Crypto:EncryptionKey</c>). When configuration values are absent in the
/// <c>Development</c> environment, a deterministic dev key is derived from a
/// well-known constant via SHA-256. In all other environments the absence of
/// explicit key configuration causes an <see cref="InvalidOperationException"/>.
/// </summary>
internal sealed class ConfigKeyVaultService : IKeyVaultService
{
    private readonly IConfiguration _config;
    private readonly bool _isDevelopment;

    /// <summary>Initialises the service with the supplied configuration and environment.</summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="environment">The host environment used to gate dev-only key fallbacks.</param>
    public ConfigKeyVaultService(IConfiguration config, IHostEnvironment environment)
    {
        _config = config;
        _isDevelopment = environment.IsDevelopment();
    }

    /// <inheritdoc/>
    public Task<byte[]> GetSigningKeyAsync(CancellationToken ct = default)
    {
        var key = _config["Crypto:SigningKey"];
        if (string.IsNullOrEmpty(key))
        {
            if (!_isDevelopment)
                throw new InvalidOperationException(
                    "Crypto:SigningKey is required in non-development environments. " +
                    "Configure it via environment variables or Azure Key Vault.");

            key = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes("spaceos-dev-signing-key-2026")));
        }
        return Task.FromResult(Convert.FromBase64String(key));
    }

    /// <inheritdoc/>
    public Task<byte[]> GetEncryptionKeyAsync(CancellationToken ct = default)
    {
        var key = _config["Crypto:EncryptionKey"];
        if (string.IsNullOrEmpty(key))
        {
            if (!_isDevelopment)
                throw new InvalidOperationException(
                    "Crypto:EncryptionKey is required in non-development environments. " +
                    "Configure it via environment variables or Azure Key Vault.");

            key = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes("spaceos-dev-encryption-key-2026")));
        }
        return Task.FromResult(Convert.FromBase64String(key));
    }
}
