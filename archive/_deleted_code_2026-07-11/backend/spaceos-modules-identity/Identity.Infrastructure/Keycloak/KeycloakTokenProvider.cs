// Identity.Infrastructure/Keycloak/KeycloakTokenProvider.cs

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Identity.Infrastructure.Keycloak.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Identity.Infrastructure.Keycloak;

public sealed class KeycloakTokenProvider : IKeycloakTokenProvider
{
    private const string CacheKey = "kc:admin:token";
    private const int TagSize = 16;
    private const int NonceSize = 12;

    private readonly HttpClient _http;
    private readonly IDatabase? _redis;
    private readonly KeycloakOptions _options;
    private readonly byte[] _encryptionKey;
    private readonly ILogger<KeycloakTokenProvider> _logger;

    public KeycloakTokenProvider(
        HttpClient http,
        IConnectionMultiplexer? redis,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakTokenProvider> logger)
    {
        _http = http;
        _redis = redis?.GetDatabase();
        _options = options.Value;
        _logger = logger;

        // AES-256-GCM key from ClientSecret (SHA-256 to ensure 32 bytes)
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(_options.ClientSecret));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Try Redis cache first (SEC-03: AES-256-GCM encrypted)
        if (_redis is not null)
        {
            try
            {
                var cached = await _redis.StringGetAsync(CacheKey).ConfigureAwait(false);
                if (cached.HasValue)
                    return Decrypt(cached!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for token cache read, falling back to direct CC grant");
            }
        }

        return await FetchAndCacheTokenAsync(ct).ConfigureAwait(false);
    }

    private async Task<string> FetchAndCacheTokenAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync(_options.TokenUrl, new FormUrlEncodedContent(form), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IdentityProviderException("KC token endpoint unreachable.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new IdentityProviderException(
                $"KC token grant failed: {(int)response.StatusCode}",
                (int)response.StatusCode);

        var tokenResponse = await response.Content.ReadFromJsonAsync<KcTokenResponse>(
            cancellationToken: ct).ConfigureAwait(false)
            ?? throw new IdentityProviderException("KC token response was empty.");

        // Cache with TTL = expires_in − 60s (SEC-03)
        var ttl = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 30));
        if (_redis is not null)
        {
            try
            {
                var encrypted = Encrypt(tokenResponse.AccessToken);
                await _redis.StringSetAsync(CacheKey, encrypted, ttl).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for token cache write, continuing without cache");
            }
        }

        return tokenResponse.AccessToken;
    }

    // AES-256-GCM encrypt: [nonce(12)] + [ciphertext] + [tag(16)]
    private byte[] Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);
        return result;
    }

    private string Decrypt(byte[] data)
    {
        var nonce = data[..NonceSize];
        var tag = data[^TagSize..];
        var ciphertext = data[NonceSize..^TagSize];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
