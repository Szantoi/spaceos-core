using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetSnapshots;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetSnapshotsQuery"/>.
/// Lives in Infrastructure because it queries <see cref="JoineryDbContext"/> directly.
/// </summary>
public sealed class GetSnapshotsQueryHandler : IRequestHandler<GetSnapshotsQuery, Result<IReadOnlyList<SnapshotSummaryDto>>>
{
    private readonly JoineryDbContext _db;

    public GetSnapshotsQueryHandler(JoineryDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<SnapshotSummaryDto>>> Handle(GetSnapshotsQuery query, CancellationToken ct)
    {
        var orderExists = await _db.DoorOrders
            .AsNoTracking()
            .AnyAsync(o => o.Id == query.OrderId && o.TenantId == query.TenantId, ct)
            .ConfigureAwait(false);

        if (!orderExists)
            return Result<IReadOnlyList<SnapshotSummaryDto>>.NotFound("Order not found");

        var snapshots = await _db.CuttingListSnapshots
            .AsNoTracking()
            .Where(s => s.DoorOrderId == query.OrderId && s.IsLatest)
            .OrderBy(s => s.CalculatedAt)
            .Select(s => new SnapshotSummaryDto(
                s.Id,
                s.DoorItemId,
                s.TemplateName,
                s.TemplateVersion,
                s.InputWidth,
                s.InputHeight,
                s.ContentHash,
                s.CalculatedAt,
                s.Lines.Count))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<SnapshotSummaryDto>>.Success(snapshots);
    }
}
