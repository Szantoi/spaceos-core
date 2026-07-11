using SpaceOS.Modules.Contracts.Cutting.DTOs;

namespace SpaceOS.Modules.Contracts.Cutting.Requests;

/// <summary>
/// Input DTO for submitting a cutting sheet. TenantId is NOT included — resolved from JWT (SEC-01).
/// Max constraints: Lines le 200, CncInstructions le 500, ProcessSteps le 50 (SEC-02).
/// ComponentName le 100 chars, MaterialCode le 20 chars (SEC-04).
/// ParameterOverridesJson le 10KB.
/// </summary>
public sealed record SubmitCuttingSheetRequest(
    Guid SourceEntityId,
    string SourceModuleType,
    Guid? SourceItemId,
    string TemplateName,
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string? ParameterOverridesJson,
    IReadOnlyList<CuttingLineDto> Lines,
    IReadOnlyList<CncInstructionDto> CncInstructions,
    IReadOnlyList<ProcessStepDto> ProcessSteps);
