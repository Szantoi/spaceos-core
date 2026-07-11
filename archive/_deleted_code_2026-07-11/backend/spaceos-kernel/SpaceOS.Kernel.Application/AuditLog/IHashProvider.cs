// SpaceOS.Kernel.Application/AuditLog/IHashProvider.cs

using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Computes the hash of a given input string using the configured algorithm.
/// Abstracting the hash algorithm here allows a future migration to SHA3-256 (or beyond)
/// without touching the dispatcher or any other call site.
/// </summary>
public interface IHashProvider
{
    /// <summary>
    /// Computes the lowercase hex-encoded hash of the given UTF-8 encoded input string.
    /// </summary>
    /// <param name="input">The string to hash. Must not be <c>null</c>.</param>
    /// <returns>A lowercase hex string representing the hash digest.</returns>
    string ComputeHash(string input);

    /// <summary>Gets the algorithm type used by this provider.</summary>
    HashAlgorithmType AlgorithmType { get; }
}
