using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;

namespace SpaceOS.Modules.Joinery.Application.WorkOrders.Commands.UpdateAssemblySequence;

/// <summary>
/// Handler for UpdateAssemblySequenceCommand.
/// Implements optimistic locking, sequence validation, and concurrent modification detection.
/// </summary>
public sealed class UpdateAssemblySequenceCommandHandler
    : IRequestHandler<UpdateAssemblySequenceCommand, Result<UpdateAssemblySequenceResponse>>
{
    private readonly IWorkOrderRepository _repository;

    public UpdateAssemblySequenceCommandHandler(IWorkOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UpdateAssemblySequenceResponse>> Handle(
        UpdateAssemblySequenceCommand request,
        CancellationToken ct)
    {
        // 1. Load work order (without operations - we'll load them separately for update)
        var workOrder = await _repository
            .GetByIdAsync(request.WorkOrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (workOrder is null)
            return Result<UpdateAssemblySequenceResponse>.NotFound(
                $"Work order '{request.WorkOrderId}' not found.");

        // 2. Load operations for update (with tracking)
        var operations = await _repository
            .GetOperationsByWorkOrderIdAsync(request.WorkOrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (operations.Count == 0)
            return Result<UpdateAssemblySequenceResponse>.Invalid(
                new ValidationError("No operations found for this work order."));

        // 3. Validate all operation IDs exist
        var requestedOperationIds = request.Operations.Select(o => o.Id).ToHashSet();
        var existingOperationIds = operations.Select(o => o.Id).ToHashSet();

        if (!requestedOperationIds.SetEquals(existingOperationIds))
        {
            var unknownIds = requestedOperationIds.Except(existingOperationIds);
            return Result<UpdateAssemblySequenceResponse>.Invalid(
                new ValidationError
                {
                    Identifier = "operations",
                    ErrorMessage = $"Unknown operation IDs: {string.Join(", ", unknownIds)}"
                });
        }

        // 4. Optimistic locking check - compare request timestamp with ALL operations' LastModified
        var latestModification = operations.Max(o => o.LastModified);
        if (latestModification.UtcDateTime > request.Timestamp.ToUniversalTime())
        {
            return Result<UpdateAssemblySequenceResponse>.Conflict(
                $"Work order was modified by another user at {latestModification:O}. Please refresh and try again.");
        }

        // 5. Update sequences
        foreach (var operation in operations)
        {
            var newSequence = request.Operations.First(o => o.Id == operation.Id).Sequence;
            operation.UpdateSequence(newSequence);
        }

        // 6. Save changes
        await _repository.UpdateOperationsAsync(operations, ct).ConfigureAwait(false);

        // 7. Calculate duration change (stub for Phase 1)
        var totalDuration = operations.Aggregate(TimeSpan.Zero, (sum, op) => sum + op.EstimatedDuration);
        var durationChange = "+0min"; // TODO: Implement dependency-based calculation in Phase 2

        // 8. Build response
        var updatedOps = operations
            .OrderBy(o => o.Sequence)
            .Select(o => new UpdatedOperationDto(
                o.Id,
                o.Sequence,
                o.Description,
                o.EstimatedDuration,
                o.LastModified.UtcDateTime))
            .ToList();

        var response = new UpdateAssemblySequenceResponse(
            updatedOps,
            durationChange,
            totalDuration);

        return Result<UpdateAssemblySequenceResponse>.Success(response);
    }
}
