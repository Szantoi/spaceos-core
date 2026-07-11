using SpaceOS.Modules.Contracts.Cutting.Enums;

namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>
/// Output DTO representing a complete cutting sheet.
/// TenantId present as read-only (SEC-01 output side).
/// ContentHash is a SHA-256 hash of the sheet content for audit/immutability verification.
/// </summary>
public sealed record CuttingSheetDto(
    Guid Id,
    Guid TenantId,
    Guid SourceEntityId,
    string SourceModuleType,
    Guid? SourceItemId,
    string TemplateName,
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string? ParameterOverridesJson,
    string ContentHash,
    DateTimeOffset CalculatedAt,
    CuttingSheetStatus Status,
    IReadOnlyList<CuttingLineDto> Lines,
    IReadOnlyList<CncInstructionDto> CncInstructions,
    IReadOnlyList<ProcessStepDto> ProcessSteps);
