using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;

namespace SpaceOS.Modules.Joinery.Application.WorkOrders.Commands.UpdateAssemblySequence;

/// <summary>
/// Command to update assembly operation sequence (drag-and-drop reordering).
/// Implements optimistic locking via timestamp comparison.
/// </summary>
public sealed record UpdateAssemblySequenceCommand(
    Guid TenantId,
    Guid WorkOrderId,
    List<OperationSequenceUpdate> Operations,
    DateTime Timestamp
) : IRequest<Result<UpdateAssemblySequenceResponse>>;
