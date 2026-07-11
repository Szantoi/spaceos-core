// SpaceOS.Kernel.Application/FlowEpics/Commands/CloseFlowEpic/CloseFlowEpicCommand.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;

/// <summary>
/// Closes a <see cref="Domain.Entities.FlowEpic"/> that is in the Delivery phase,
/// recording the uploaded proof document URL and its SHA-256 hash.
/// </summary>
/// <param name="FlowEpicId">The identifier of the flow epic to close.</param>
/// <param name="ProofUrl">The URL of the uploaded proof document.</param>
/// <param name="ProofHash">The lowercase hex-encoded SHA-256 hash of the proof document content.</param>
public sealed record CloseFlowEpicCommand(
    Guid   FlowEpicId,
    string ProofUrl,
    string ProofHash) : IRequest<Result>;
