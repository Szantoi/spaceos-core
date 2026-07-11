namespace SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;

/// <summary>
/// Request DTO for updating assembly operation sequence (drag-and-drop reordering).
/// </summary>
public sealed record UpdateAssemblySequenceRequest(
    List<OperationSequenceUpdate> Operations,
    DateTime Timestamp
);

/// <summary>
/// Single operation sequence update.
/// </summary>
public sealed record OperationSequenceUpdate(
    Guid Id,
    int Sequence
);
