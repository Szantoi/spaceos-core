// SpaceOS.Infrastructure/Auth/InMemorySecretProvider.cs
using Microsoft.Extensions.Configuration;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Development-only implementation of <see cref="ISecretProvider"/> that resolves secrets
/// from the <c>Secrets</c> section of <see cref="IConfiguration"/>.
/// Falls back to returning the reference name as-is when the key is absent — useful in test
/// scenarios where a real secret is not required.
/// </summary>
internal sealed class InMemorySecretProvider : ISecretProvider
{
    private const string ConfigSection = "Secrets";

    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initialises a new <see cref="InMemorySecretProvider"/>.
    /// </summary>
    /// <param name="configuration">Application configuration providing the <c>Secrets</c> section.</param>
    public InMemorySecretProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
            return Task.FromResult<string?>(null);

        var value = _configuration[$"{ConfigSection}:{secretRef}"];

        // Fall back to the ref name itself so tests that do not need a real secret still work.
        return Task.FromResult<string?>(value ?? secretRef);
    }
}
