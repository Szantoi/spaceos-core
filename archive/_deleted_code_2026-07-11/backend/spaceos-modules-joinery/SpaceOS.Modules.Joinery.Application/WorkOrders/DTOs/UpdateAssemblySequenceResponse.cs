namespace SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;

/// <summary>
/// Response DTO for assembly sequence update.
/// </summary>
public sealed record UpdateAssemblySequenceResponse(
    List<UpdatedOperationDto> UpdatedOperations,
    string EstimatedDurationChange,
    TimeSpan TotalDuration
);

/// <summary>
/// Updated operation DTO with full details.
/// </summary>
public sealed record UpdatedOperationDto(
    Guid Id,
    int Sequence,
    string Description,
    TimeSpan EstimatedDuration,
    DateTime LastModified
);
