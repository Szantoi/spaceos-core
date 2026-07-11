using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Application.Handlers;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetReservationsQuery"/>.
/// </summary>
public sealed class GetReservationsQueryHandler
    : IRequestHandler<GetReservationsQuery, Result<IReadOnlyList<ReservationDto>>>
{
    private const int MaxTake = 500;

    private readonly InventoryDbContext _db;

    public GetReservationsQueryHandler(InventoryDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ReservationDto>>> Handle(
        GetReservationsQuery request, CancellationToken ct)
    {
        var filter = request.Filter;

        // DoS guard: at least one filter field must be specified
        if (filter.ConsumerModule is null
            && filter.Status is null
            && filter.CorrelationId is null
            && filter.CreatedAfter is null
            && filter.CreatedBefore is null)
        {
            return Result<IReadOnlyList<ReservationDto>>.Invalid(
                new ValidationError("At least one filter field is required."));
        }

        var clampedTake = Math.Min(filter.Take, MaxTake);

        var query = _db.Reservations
            .Include(r => r.Items)
            .AsNoTracking()
            .Where(r => r.TenantId == request.TenantId);

        if (filter.ConsumerModule is not null)
            query = query.Where(r => r.ConsumerModule == filter.ConsumerModule);

        if (filter.Status is not null)
            query = query.Where(r => (int)r.Status == (int)filter.Status.Value);

        if (filter.CorrelationId is not null)
            query = query.Where(r => r.CorrelationId == filter.CorrelationId.Value);

        if (filter.CreatedAfter is not null)
            query = query.Where(r => r.CreatedAt >= filter.CreatedAfter.Value);

        if (filter.CreatedBefore is not null)
            query = query.Where(r => r.CreatedAt <= filter.CreatedBefore.Value);

        var reservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(filter.Skip)
            .Take(clampedTake)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<ReservationDto> result = reservations
            .Select(r => r.ToReservationDto())
            .ToList();

        return Result<IReadOnlyList<ReservationDto>>.Success(result);
    }
}
