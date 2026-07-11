using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Events;

/// <summary>
/// Integration event: fired when a cutting job finishes so the Inventory module
/// can create offcut records for reuse tracking.
/// Stub: published locally until CUTTING-028 delivers real cross-module event bus.
/// </summary>
public sealed record CuttingJobCompletedEvent(
    Guid JobId,
    Guid MaterialCatalogId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    decimal WastePercent,
    Guid TenantId) : INotification;
