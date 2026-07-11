using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Application.Handlers;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Services;
using SpaceOS.Modules.Inventory.Domain.Specifications;
using SpaceOS.Modules.Inventory.Infrastructure.Observability;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="ReserveStockCommand"/>. Creates a new Active reservation or returns
/// the existing one when the same (TenantId, CorrelationId) already has an Active record
/// (idempotency).
/// </summary>
public sealed class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, Result<ReservationDto>>
{
    private const string UniqueConstraintSqlState = "23505";
    private const string CorrelationUniqueConstraint = "ux_reservations_tenant_correlation_active";

    private readonly InventoryDbContext _db;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ConsumerContextValidator _contextValidator;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        InventoryDbContext db,
        IModuleRegistry moduleRegistry,
        ConsumerContextValidator contextValidator,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _db = db;
        _moduleRegistry = moduleRegistry;
        _contextValidator = contextValidator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<ReservationDto>> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        // 1. Validate consumer module (I-12)
        if (!_moduleRegistry.IsKnownConsumerModule(request.ConsumerModule))
        {
            return Result<ReservationDto>.Invalid(
                new ValidationError($"Unknown consumer module: '{request.ConsumerModule}'."));
        }

        // 2. Validate consumer context JSON for XSS / PII (I-11)
        var contextValidation = _contextValidator.Validate(request.ConsumerContextJson);
        if (!contextValidation.IsSuccess)
        {
            return Result<ReservationDto>.Invalid(contextValidation.ValidationErrors.ToArray());
        }

        // 3. Idempotency check — return existing Active reservation if present
        var existing = await _db.Reservations
            .Include(r => r.Items)
            .AsNoTracking()
            .Where(ReservationByCorrelationActiveSpec.For(request.TenantId, request.CorrelationId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            _logger.LogInformation(
                "ReserveStock: idempotency hit — returning existing reservation {ReservationId} for tenant {TenantId} / correlation {CorrelationId}",
                existing.Id, request.TenantId, request.CorrelationId);
            ReservationMetrics.IdempotencyHits.Add(1,
                new KeyValuePair<string, object?>("tenant_id", request.TenantId.ToString()),
                new KeyValuePair<string, object?>("consumer_module", request.ConsumerModule));
            return Result<ReservationDto>.Success(existing.ToReservationDto());
        }

        // 4. Check all requested stock items exist for this tenant
        var requestedIds = request.Items.Select(i => i.StockItemId).Distinct().ToList();
        var foundIds = await _db.PanelStocks
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId && requestedIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var missingIds = requestedIds.Except(foundIds).ToList();
        if (missingIds.Count > 0)
        {
            return Result<ReservationDto>.NotFound(
                $"Stock items not found for tenant: {string.Join(", ", missingIds)}.");
        }

        // 5. Build domain aggregate
        var domainItems = request.Items
            .Select(i => (i.StockItemId, i.MaterialCode, i.Quantity))
            .ToList()
            .AsReadOnly();

        var reservation = Reservation.Reserve(
            request.TenantId,
            request.CorrelationId,
            request.ConsumerModule,
            request.ConsumerContextJson,
            request.CreatedByUserId,
            domainItems,
            request.Ttl);

        // 6. Persist — handle race-condition unique constraint violation
        try
        {
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, CorrelationUniqueConstraint))
        {
            // 7. Race: another request won the insert — re-fetch the winner
            _logger.LogWarning(
                "ReserveStock: unique constraint race for tenant {TenantId} / correlation {CorrelationId} — re-fetching winner",
                request.TenantId, request.CorrelationId);

            var winner = await _db.Reservations
                .Include(r => r.Items)
                .AsNoTracking()
                .Where(ReservationByCorrelationActiveSpec.For(request.TenantId, request.CorrelationId))
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return winner is not null
                ? Result<ReservationDto>.Success(winner.ToReservationDto())
                : Result<ReservationDto>.Conflict("Concurrent reservation conflict — please retry.");
        }

        // 8. Log domain events (no dispatcher wired yet)
        foreach (var ev in reservation.DomainEvents)
        {
            _logger.LogDebug("DomainEvent raised: {EventType} on reservation {ReservationId}",
                ev.GetType().Name, reservation.Id);
        }

        // 9. Record metrics
        ReservationMetrics.ReservationsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", request.TenantId.ToString()),
            new KeyValuePair<string, object?>("consumer_module", request.ConsumerModule));

        _logger.LogInformation(
            "ReserveStock: created reservation {ReservationId} for tenant {TenantId} / correlation {CorrelationId}",
            reservation.Id, request.TenantId, request.CorrelationId);

        return Result<ReservationDto>.Success(reservation.ToReservationDto());
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex, string constraintName)
    {
        return ex.InnerException is PostgresException pg
            && pg.SqlState == UniqueConstraintSqlState
            && pg.ConstraintName == constraintName;
    }
}
