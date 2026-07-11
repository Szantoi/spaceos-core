// SpaceOS.Infrastructure/Auth/DevRsaKeyManager.cs

using System.Security.Cryptography;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Manages the development RSA key pair. Auto-generates a 2048-bit key pair
/// on first use and caches it in memory for the process lifetime.
/// For development only — never use in production.
/// </summary>
public static class DevRsaKeyManager
{
    private static readonly RSA _rsa = CreateOrLoad();

    /// <summary>Gets the development RSA instance (contains both public and private key).</summary>
    public static RSA Instance => _rsa;

    private static RSA CreateOrLoad()
    {
        const string keyPath = "keys/dev-private-key.pem";
        try
        {
            if (File.Exists(keyPath))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(keyPath));
                return rsa;
            }
        }
        catch
        {
            // Corrupted or unreadable key file — fall through to generate a fresh key.
        }

        var newRsa = RSA.Create(2048);
        try
        {
            Directory.CreateDirectory("keys");
            File.WriteAllText(keyPath, newRsa.ExportPkcs8PrivateKeyPem());
        }
        catch
        {
            // File system not writable (read-only publish dir on VPS) — use in-memory key only.
            // Node JWTs remain valid for the process lifetime; a restart regenerates the key.
        }

        return newRsa;
    }
}
