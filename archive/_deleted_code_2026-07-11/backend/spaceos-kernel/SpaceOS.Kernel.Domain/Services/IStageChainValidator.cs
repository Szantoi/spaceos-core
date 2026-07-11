// SpaceOS.Kernel.Domain/Services/IStageChainValidator.cs
using System.Collections.Generic;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Domain service that validates a stage-advance operation against the ordered steps of an
/// assigned chain template (BE-01 / SEC-03).
/// </summary>
/// <remarks>
/// The implementation lives in Infrastructure. The Domain declares the contract so that
/// Application handlers can reference it without an Infrastructure dependency.
/// </remarks>
public interface IStageChainValidator
{
    /// <summary>
    /// Validates whether advancing <paramref name="epic"/> to <paramref name="targetStageCode"/> is
    /// permitted given the ordered <paramref name="chainSteps"/>.
    /// </summary>
    /// <param name="epic">The flow epic being advanced.</param>
    /// <param name="targetStageCode">The desired next stage code.</param>
    /// <param name="chainSteps">The ordered steps of the epic's assigned chain template.</param>
    /// <exception cref="DomainException">Thrown when the transition is invalid.</exception>
    void ValidateAdvance(
        FlowEpic epic,
        string targetStageCode,
        IReadOnlyList<StageChainStep> chainSteps);
}
