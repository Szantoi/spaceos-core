// SpaceOS.Kernel.Application/AuditLog/Sha256HashProvider.cs

using System.Security.Cryptography;
using System.Text;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Default <see cref="IHashProvider"/> implementation using SHA-256.
/// Registered as a singleton — stateless and thread-safe.
/// </summary>
internal sealed class Sha256HashProvider : IHashProvider
{
    /// <inheritdoc/>
    public HashAlgorithmType AlgorithmType => HashAlgorithmType.SHA256;

    /// <inheritdoc/>
    public string ComputeHash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
