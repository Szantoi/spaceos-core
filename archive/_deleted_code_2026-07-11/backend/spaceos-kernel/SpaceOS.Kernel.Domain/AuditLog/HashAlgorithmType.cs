// SpaceOS.Kernel.Domain/AuditLog/HashAlgorithmType.cs

namespace SpaceOS.Kernel.Domain.AuditLog;

/// <summary>
/// Identifies the cryptographic hash algorithm used to compute an <see cref="AuditEvent"/>'s
/// <c>StateHash</c>. Stored alongside each record so a future algorithm migration can recompute
/// hashes incrementally without ambiguity about which algorithm produced a given hash.
/// </summary>
public enum HashAlgorithmType
{
    /// <summary>SHA-256 (current default). Produces a 64-character lowercase hex digest.</summary>
    SHA256 = 1,

    /// <summary>SHA3-256 (migration target). Produces a 64-character lowercase hex digest.</summary>
    SHA3_256 = 2
}
