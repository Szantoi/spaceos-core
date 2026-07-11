using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetManufacturingSheet;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetManufacturingSheetQuery"/>.
/// Generates a manufacturing sheet PDF from the order's DoorItems — no snapshots required.
/// Lives in Infrastructure because it needs <see cref="JoineryDbContext"/> and
/// <see cref="IProductionSheetGenerator"/>.
/// </summary>
public sealed class GetManufacturingSheetQueryHandler
    : IRequestHandler<GetManufacturingSheetQuery, Result<Stream>>
{
    private readonly JoineryDbContext _db;
    private readonly IProductionSheetGenerator _generator;
    private readonly ILogger<GetManufacturingSheetQueryHandler> _logger;

    public GetManufacturingSheetQueryHandler(
        JoineryDbContext db,
        IProductionSheetGenerator generator,
        ILogger<GetManufacturingSheetQueryHandler> logger)
    {
        _db = db;
        _generator = generator;
        _logger = logger;
    }

    public async Task<Result<Stream>> Handle(GetManufacturingSheetQuery query, CancellationToken ct)
    {
        var order = await _db.DoorOrders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.OrderId && o.TenantId == query.TenantId, ct)
            .ConfigureAwait(false);

        if (order is null)
            return Result<Stream>.NotFound("Order not found");

        var pdfStream = _generator.GenerateManufacturingSheet(order);

        _logger.LogInformation(
            "Generated manufacturing sheet for order {OrderId} ({ItemCount} items)",
            query.OrderId, order.Items.Count);

        return Result<Stream>.Success(pdfStream);
    }
}
