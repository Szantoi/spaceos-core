// SpaceOS.Infrastructure/Auth/ConfigurationGenesisHashProvider.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Development implementation of <see cref="IGenesisHashProvider"/> backed by <c>IConfiguration</c>.
/// Reads <c>AuditChain:GenesisHash</c> from configuration. If the key is absent, a 64-character
/// random hex string is generated using <see cref="RandomNumberGenerator"/> and cached in-process
/// for the lifetime of the application. The generated value is logged so it can be captured and
/// pinned in appsettings if reproducibility is needed.
/// </summary>
internal sealed class ConfigurationGenesisHashProvider : IGenesisHashProvider
{
    private const string ConfigKey = "AuditChain:GenesisHash";

    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationGenesisHashProvider> _logger;
    private string? _cachedHash;

    /// <summary>
    /// Initialises a new <see cref="ConfigurationGenesisHashProvider"/>.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger used to surface the generated genesis hash.</param>
    public ConfigurationGenesisHashProvider(
        IConfiguration configuration,
        ILogger<ConfigurationGenesisHashProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _configuration = configuration;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public Task<string> GetGenesisHashAsync(CancellationToken ct = default)
    {
        if (_cachedHash is not null)
            return Task.FromResult(_cachedHash);

        var configured = _configuration[ConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _cachedHash = configured;
            return Task.FromResult(_cachedHash);
        }

        // Generate a random 32-byte (256-bit) genesis hash and cache it in-process.
        var bytes = RandomNumberGenerator.GetBytes(32);
        _cachedHash = Convert.ToHexString(bytes).ToLowerInvariant();

        _logger.LogWarning(
            "AuditChain:GenesisHash not configured — generated ephemeral genesis hash: {GenesisHash}. " +
            "Pin this value in appsettings if chain reproducibility is required.",
            _cachedHash);

        return Task.FromResult(_cachedHash);
    }
}
