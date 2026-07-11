using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Joinery.Infrastructure.Cutting;

/// <summary>
/// Stub implementation of <see cref="ICuttingProvider"/> for Soft Launch.
/// Logs the call and returns success without calling the real service.
/// Replace with HttpAdapter in Q3 when Cutting service is stable.
/// </summary>
public sealed class CuttingProviderStub : ICuttingProvider
{
    private readonly ILogger<CuttingProviderStub> _logger;

    public CuttingProviderStub(ILogger<CuttingProviderStub> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default)
    {
        var stubId = Guid.NewGuid();
        _logger.LogInformation(
            "[CuttingProviderStub] SubmitCuttingSheetAsync called for order {SourceOrderId} with {LineCount} lines → stub ID {StubId}",
            sheet.SourceOrderId, sheet.Lines.Count, stubId);
        return Task.FromResult(stubId);
    }

    /// <inheritdoc/>
    public Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default)
        => throw new NotImplementedException("Not required for Soft Launch");

    /// <inheritdoc/>
    public Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default)
        => throw new NotImplementedException("Not required for Soft Launch");

    /// <inheritdoc/>
    public Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default)
        => throw new NotImplementedException("Not required for Soft Launch");
}
