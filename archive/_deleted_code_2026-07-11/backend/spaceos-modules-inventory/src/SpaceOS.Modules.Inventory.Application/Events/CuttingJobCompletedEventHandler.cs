using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Application.Events;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Events;

/// <summary>
/// Creates an Available offcut record when a cutting job completes.
/// Stub implementation: uses 15% waste estimate and half-sheet dimensions.
/// Will be upgraded to consume real Cutting module events in CUTTING-028.
/// </summary>
public sealed class CuttingJobCompletedEventHandler
    : INotificationHandler<CuttingJobCompletedEvent>
{
    // Default MDF/panel density — used until MaterialCatalog tracks density
    private const decimal DefaultDensityKgPerM3 = 750m;

    private readonly IInventoryRepository _repository;

    public CuttingJobCompletedEventHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CuttingJobCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.WastePercent <= 0m)
            return;

        // v1 integration stub: CUTTING service sends zero dimensions until v1.5 ships real material data.
        // Skip offcut creation when panel dimensions are unavailable — acknowledged without side effects.
        if (notification.WidthMm <= 0m || notification.HeightMm <= 0m || notification.ThicknessMm <= 0m)
            return;

        var totalVolume = Offcut.ComputeVolume(
            notification.WidthMm,
            notification.HeightMm,
            notification.ThicknessMm);

        var wasteVolume = totalVolume * notification.WastePercent;
        var weightKg    = wasteVolume * DefaultDensityKgPerM3;

        // Stub: offcut piece is modelled as half the original job sheet
        var offcut = Offcut.Register(
            notification.TenantId,
            notification.MaterialCatalogId,
            notification.MaterialCode,
            notification.WidthMm  * 0.5m,
            notification.HeightMm * 0.5m,
            notification.ThicknessMm,
            wasteVolume,
            weightKg,
            originCuttingSheetId: null,
            cuttingJobId: notification.JobId);

        await _repository.AddOffcutAsync(offcut, cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
