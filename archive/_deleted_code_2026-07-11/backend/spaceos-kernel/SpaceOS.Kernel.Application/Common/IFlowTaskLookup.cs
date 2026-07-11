// SpaceOS.Kernel.Application/Common/IFlowTaskLookup.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Abstraction for looking up FlowTask tenant ownership across the module boundary.
/// The FlowTask entity lives in <c>SpaceOS.Modules.FlowManagement</c> — this interface
/// allows the Kernel Application layer to verify tenant ownership without a direct module dependency.
/// </summary>
public interface IFlowTaskLookup
{
    /// <summary>
    /// Returns the tenant identifier of the FlowTask with the given <paramref name="flowTaskId"/>,
    /// or <see langword="null"/> if the task does not exist.
    /// </summary>
    /// <param name="flowTaskId">The FlowTask identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Guid?> GetTenantIdAsync(Guid flowTaskId, CancellationToken ct);
}
