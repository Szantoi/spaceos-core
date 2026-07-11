using MediatR;
using SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;
using SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;
using SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;
using SpaceOS.Modules.Inventory.Application.Queries.GetStock;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Providers;

namespace SpaceOS.Modules.Inventory.Infrastructure.Adapters;

public class InventoryProviderAdapter : IInventoryProvider
{
    private readonly IMediator _mediator;
    private readonly IHttpContextTenantAccessor _tenantAccessor;

    public InventoryProviderAdapter(IMediator mediator, IHttpContextTenantAccessor tenantAccessor)
    {
        _mediator = mediator;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockQuery(materialType), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Errors.FirstOrDefault() ?? "Unknown error.");

        var offcutsResult = await _mediator.Send(new GetOffcutsQuery(materialType), ct).ConfigureAwait(false);
        var offcutDtos = offcutsResult.IsSuccess
            ? offcutsResult.Value.Select(o => new OffcutDto(o.Id, materialType, (int)o.WidthMm, (int)o.HeightMm, o.OriginCuttingSheetId ?? Guid.Empty)).ToList()
            : new List<OffcutDto>();

        return new PanelStockDto(result.Value.MaterialType, result.Value.FullPanelCount, result.Value.WidthMm, result.Value.HeightMm, offcutDtos);
    }

    public async Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOffcutsQuery(materialType), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Array.Empty<OffcutDto>();

        return result.Value.Select(o => new OffcutDto(o.Id, materialType, (int)o.WidthMm, (int)o.HeightMm, o.OriginCuttingSheetId ?? Guid.Empty)).ToList();
    }

    public async Task RecordConsumptionAsync(IReadOnlyList<StockMovementDto> items, CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.TenantId;
        foreach (var item in items)
        {
            var command = new RecordConsumptionCommand(tenantId, item.MaterialType, item.Thickness, item.Area, item.PanelCount, item.Reason, item.OccurredAt);
            await _mediator.Send(command, ct).ConfigureAwait(false);
        }
    }

    public async Task RecordInboundAsync(StockMovementDto delivery, CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.TenantId;
        var command = new RecordInboundCommand(tenantId, delivery.MaterialType, delivery.Thickness, delivery.Area, delivery.PanelCount, delivery.Reason, delivery.OccurredAt);
        await _mediator.Send(command, ct).ConfigureAwait(false);
    }

    public async Task RecordOffcutAsync(OffcutDto offcut, CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.TenantId;
        var command = new RecordOffcutCommand(tenantId, offcut.MaterialType, offcut.WidthMm, offcut.HeightMm, offcut.OriginCuttingSheetId == Guid.Empty ? null : offcut.OriginCuttingSheetId);
        await _mediator.Send(command, ct).ConfigureAwait(false);
    }

    public async Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateRange range, CancellationToken ct = default)
    {
        var query = new GetConsumptionTrendQuery("MDF 18mm", range.From, range.To);
        var result = await _mediator.Send(query, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return new ConsumptionTrendDto("Unknown", Array.Empty<DailyConsumptionDto>(), 0m);

        var daily = result.Value.DailyData.Select(d => new DailyConsumptionDto(d.Date, d.Area)).ToList();
        return new ConsumptionTrendDto(result.Value.MaterialType, daily, result.Value.AverageDailyConsumption);
    }
}
