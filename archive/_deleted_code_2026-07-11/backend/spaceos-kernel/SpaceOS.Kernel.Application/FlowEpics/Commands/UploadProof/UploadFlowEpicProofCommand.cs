// SpaceOS.Kernel.Application/FlowEpics/Commands/UploadProof/UploadFlowEpicProofCommand.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;

/// <summary>
/// Uploads a proof document for a <see cref="Domain.Entities.FlowEpic"/> to immutable storage
/// and returns the storage URL and SHA-256 hash.
/// This command does NOT close the epic — call <see cref="CloseFlowEpic.CloseFlowEpicCommand"/> as a separate step.
/// </summary>
/// <param name="FlowEpicId">The identifier of the flow epic this proof belongs to.</param>
/// <param name="FileName">The logical file name under which to store the proof document.</param>
/// <param name="Content">The raw binary content of the proof document.</param>
public sealed record UploadFlowEpicProofCommand(
    Guid   FlowEpicId,
    string FileName,
    byte[] Content) : IRequest<Result<ProofUploadDto>>;
