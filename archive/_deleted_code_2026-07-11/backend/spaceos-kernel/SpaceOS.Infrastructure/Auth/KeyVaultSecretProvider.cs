// SpaceOS.Infrastructure/Auth/KeyVaultSecretProvider.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Production stub for <see cref="ISecretProvider"/> targeting Azure Key Vault.
/// Sprint 2 will implement the real <c>Azure.Security.KeyVault.Secrets.SecretClient</c> call.
/// Until then, logs a warning and returns <c>null</c> when <c>KeyVault:VaultUri</c> is absent;
/// otherwise returns <c>null</c> for every request (real retrieval deferred to Sprint 2).
/// </summary>
internal sealed class KeyVaultSecretProvider : ISecretProvider
{
    private const string VaultUriKey = "KeyVault:VaultUri";

    private readonly ILogger<KeyVaultSecretProvider> _logger;
    private readonly bool _isConfigured;

    /// <summary>
    /// Initialises a new <see cref="KeyVaultSecretProvider"/>.
    /// </summary>
    /// <param name="configuration">Application configuration used to detect the Key Vault URI.</param>
    /// <param name="logger">Logger for configuration warnings.</param>
    public KeyVaultSecretProvider(
        IConfiguration configuration,
        ILogger<KeyVaultSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _logger       = logger;
        _isConfigured = !string.IsNullOrWhiteSpace(configuration[VaultUriKey]);

        if (!_isConfigured)
        {
            _logger.LogWarning(
                "KeyVaultSecretProvider: '{Key}' is not configured — Sprint 2 will wire up Azure Key Vault.",
                VaultUriKey);
        }
    }

    /// <inheritdoc/>
    public Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default)
    {
        // Sprint 2: replace with SecretClient.GetSecretAsync(secretRef, cancellationToken: ct).
        return Task.FromResult<string?>(null);
    }
}
