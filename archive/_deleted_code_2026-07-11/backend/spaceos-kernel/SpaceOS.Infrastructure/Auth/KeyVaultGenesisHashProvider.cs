// SpaceOS.Infrastructure/Auth/KeyVaultGenesisHashProvider.cs

using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Production implementation of <see cref="IGenesisHashProvider"/> backed by Azure Key Vault
/// via <see cref="ISecretProvider"/>.
/// Fetches the secret named <c>audit-chain-genesis-hash</c> on first access and caches it
/// in-process for the singleton lifetime. Creates a DI scope per access to safely resolve
/// the scoped <see cref="ISecretProvider"/>.
/// </summary>
internal sealed class KeyVaultGenesisHashProvider : IGenesisHashProvider
{
    private const string SecretRef = "audit-chain-genesis-hash";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeyVaultGenesisHashProvider> _logger;
    private string? _cachedHash;

    /// <summary>
    /// Initialises a new <see cref="KeyVaultGenesisHashProvider"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory used to resolve the scoped <see cref="ISecretProvider"/>.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public KeyVaultGenesisHashProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<KeyVaultGenesisHashProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetGenesisHashAsync(CancellationToken ct = default)
    {
        if (_cachedHash is not null)
            return _cachedHash;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var secretProvider = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        var secret = await secretProvider.GetSecretAsync(SecretRef, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(secret))
        {
            _cachedHash = secret;
            return _cachedHash;
        }

        // Secret absent — generate and log. Storing to Key Vault is a stub
        // until the write path of ISecretProvider is implemented.
        var bytes = RandomNumberGenerator.GetBytes(32);
        _cachedHash = Convert.ToHexString(bytes).ToLowerInvariant();

        _logger.LogWarning(
            "Key Vault secret '{SecretRef}' not found — generated ephemeral genesis hash: {GenesisHash}. " +
            "Store this value in Key Vault under '{SecretRef}' before the first audit event is recorded.",
            SecretRef, _cachedHash, SecretRef);

        return _cachedHash;
    }
}
