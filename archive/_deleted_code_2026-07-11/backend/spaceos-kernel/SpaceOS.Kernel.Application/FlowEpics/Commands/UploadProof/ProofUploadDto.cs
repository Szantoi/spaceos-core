// SpaceOS.Kernel.Application/FlowEpics/Commands/UploadProof/ProofUploadDto.cs

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;

/// <summary>
/// Result returned after a proof document is successfully stored in immutable storage.
/// </summary>
/// <param name="ProofUrl">The storage URL of the uploaded proof document.</param>
/// <param name="ProofHash">The lowercase hex-encoded SHA-256 hash of the proof document content.</param>
public sealed record ProofUploadDto(string ProofUrl, string ProofHash);
