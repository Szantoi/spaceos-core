// SpaceOS.Kernel.Application/Sync/ISyncSignalHasher.cs
namespace SpaceOS.Kernel.Application.Sync;

/// <summary>
/// Computes HMAC-SHA256 fingerprints for entries in the SyncSignal append-only hash chain.
/// Implemented in Infrastructure; consumed by the Application layer handler.
/// </summary>
public interface ISyncSignalHasher
{
    /// <summary>
    /// Computes an HMAC-SHA256 hex hash over the concatenated chain fields.
    /// Input format: <c>{previousHash}:{payloadJson}:{occurredAt:O}</c>
    /// </summary>
    /// <param name="previousHash">The hash of the preceding entry in the chain.</param>
    /// <param name="payloadJson">The JSON-serialised payload of the signal.</param>
    /// <param name="occurredAt">The UTC timestamp of the signal.</param>
    /// <returns>Lowercase hex-encoded HMAC-SHA256 digest.</returns>
    string ComputeHash(string previousHash, string payloadJson, DateTimeOffset occurredAt);
}
