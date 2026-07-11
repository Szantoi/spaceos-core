using SpaceOS.Modules.Contracts.Cutting.Enums;

namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Current shop-floor execution status of a cutting sheet.</summary>
public sealed record ExecutionStatusDto(
    Guid SheetId,
    CuttingExecutionStatus Status,
    Guid? OperatorId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
