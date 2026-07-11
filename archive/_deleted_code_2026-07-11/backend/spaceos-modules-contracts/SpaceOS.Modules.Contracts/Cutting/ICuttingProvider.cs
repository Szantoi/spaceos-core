using Ardalis.Result;
using SpaceOS.Modules.Contracts.Cutting.DTOs;
using SpaceOS.Modules.Contracts.Cutting.Requests;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting;

/// <summary>
/// Contract for cutting services. Implementations: SpaceOS built-in, OptiCut, CutRite, manual.
/// TenantId is NOT in request DTOs — the implementation resolves it from JWT (SEC-01).
/// Consumer MUST verify Capabilities before calling optional methods (SEC-05).
/// </summary>
public interface ICuttingProvider : IModuleProvider
{
    /// <summary>
    /// Submits a new cutting sheet for processing.
    /// Requires <see cref="ProviderCapability.CuttingSubmit"/> capability.
    /// </summary>
    /// <param name="request">The cutting sheet to submit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new cutting sheet identifier on success.</returns>
    Task<Result<Guid>> SubmitCuttingSheetAsync(SubmitCuttingSheetRequest request, CancellationToken ct);

    /// <summary>
    /// Retrieves a cutting sheet by its identifier.
    /// </summary>
    /// <param name="sheetId">The cutting sheet identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cutting sheet DTO on success.</returns>
    Task<Result<CuttingSheetDto>> GetCuttingSheetAsync(Guid sheetId, CancellationToken ct);

    /// <summary>
    /// Queries all cutting sheets produced for a given source entity.
    /// Needed by Joinery and Cabinet modules (BE-01).
    /// </summary>
    /// <param name="sourceEntityId">Identifier of the source entity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All cutting sheets for the entity.</returns>
    Task<Result<IReadOnlyList<CuttingSheetDto>>> GetCuttingSheetsBySourceAsync(Guid sourceEntityId, CancellationToken ct);

    /// <summary>
    /// Retrieves the nesting result for a cutting sheet.
    /// Requires <see cref="ProviderCapability.CuttingNesting"/> capability.
    /// </summary>
    /// <param name="sheetId">The cutting sheet identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The nesting result on success.</returns>
    Task<Result<NestingResultDto>> GetNestingResultAsync(Guid sheetId, CancellationToken ct);

    /// <summary>
    /// Retrieves the current shop-floor execution status of a cutting sheet.
    /// Requires <see cref="ProviderCapability.CuttingExecution"/> capability.
    /// </summary>
    /// <param name="sheetId">The cutting sheet identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The execution status on success.</returns>
    Task<Result<ExecutionStatusDto>> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct);

    /// <summary>
    /// Retrieves a waste report for the specified time window.
    /// Requires <see cref="ProviderCapability.CuttingWaste"/> capability.
    /// Max 5s response time (BE-02).
    /// </summary>
    /// <param name="from">Start of the reporting window (inclusive).</param>
    /// <param name="to">End of the reporting window (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The aggregated waste report on success.</returns>
    Task<Result<WasteReportDto>> GetWasteReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);

    /// <summary>
    /// Submits a cutting sheet from an anonymous or partner channel.
    /// Requires <see cref="ProviderCapability.CuttingAnonymous"/> capability (SEC-05).
    /// v1: DIM throws NotSupportedException — providers opt-in by overriding.
    /// v1.5 (FreeTier): CuttingProviderHttpAdapter overrides with synthetic tenant context.
    /// </summary>
    /// <param name="request">The anonymous sheet request including channel metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new cutting sheet identifier on success.</returns>
    Task<Result<Guid>> SubmitAnonymousSheetAsync(AnonymousSheetRequest request, CancellationToken ct)
    {
        throw new NotSupportedException(
            $"Provider does not support anonymous sheet submission. " +
            $"Check {nameof(ProviderCapability)}.{nameof(ProviderCapability.CuttingAnonymous)} before calling.");
    }
}
