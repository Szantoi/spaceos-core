using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateOrderFromConversion;

public sealed class CreateOrderFromConversionCommandHandler(
    IDoorOrderRepository repo,
    IClock clock,
    IMediator mediator,
    ILogger<CreateOrderFromConversionCommandHandler> log)
    : IRequestHandler<CreateOrderFromConversionCommand, Result<CreateOrderFromConversionResult>>
{
    public async Task<Result<CreateOrderFromConversionResult>> Handle(
        CreateOrderFromConversionCommand cmd, CancellationToken ct)
    {
        // 1. Idempotency check
        var existing = await repo.FindBySourceQuoteIdAsync(cmd.TenantId, cmd.QuoteId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.SourceContentHash != cmd.ContentHash)
            {
                log.LogWarning(
                    "ContentHash mismatch {Scope} QuoteId={QuoteId} TenantId={TenantId} StoredHash={StoredHash} IncomingHash={IncomingHash}",
                    "internal", cmd.QuoteId, cmd.TenantId,
                    existing.SourceContentHash, cmd.ContentHash);
                return Result<CreateOrderFromConversionResult>.Conflict(
                    "ContentHash mismatch — idempotency conflict.");
            }
            return Result<CreateOrderFromConversionResult>.Success(
                new CreateOrderFromConversionResult(existing.Id, existing.ConfirmedFromSalesAt!.Value));
        }

        // 2. Map command lines to domain value objects
        var lineData = cmd.Lines
            .Select(l => new ConversionLineData(
                l.SourceTemplateId, l.Description, l.Quantity,
                l.UnitPriceNet, l.VatRate, l.DiscountPercent, l.SortOrder))
            .ToList();

        // 3. Aggregate factory — validates and builds DoorOrderConvertedLine objects internally
        var createResult = DoorOrder.CreateFromConversion(
            Guid.NewGuid(), cmd.TenantId, cmd.CustomerId, cmd.LinkedTenantId,
            cmd.QuoteId, cmd.ContentHash, cmd.Currency,
            cmd.TotalNet, cmd.TotalVat, cmd.TotalGross,
            lineData,
            clock);

        if (!createResult.IsSuccess)
            return Result<CreateOrderFromConversionResult>.Invalid(createResult.ValidationErrors);

        var order = createResult.Value;

        // 4. Persist (AddAsync already calls SaveChangesAsync internally)
        try
        {
            await repo.AddAsync(order, ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolationOnQuoteId(ex))
        {
            var concurrent = await repo.FindBySourceQuoteIdAsync(cmd.TenantId, cmd.QuoteId, ct)
                .ConfigureAwait(false);
            if (concurrent is null)
                return Result<CreateOrderFromConversionResult>.Error(
                    "Idempotency state inconsistent after concurrent insert.");
            return Result<CreateOrderFromConversionResult>.Success(
                new CreateOrderFromConversionResult(concurrent.Id, concurrent.ConfirmedFromSalesAt!.Value));
        }

        // 5. Domain events dispatch (Golden Rule #4)
        var events = order.PopDomainEvents();
        await DomainEventDispatcher.DispatchAsync(mediator, events, ct).ConfigureAwait(false);

        return Result<CreateOrderFromConversionResult>.Success(
            new CreateOrderFromConversionResult(order.Id, order.ConfirmedFromSalesAt!.Value));
    }

    // Checks for PostgreSQL unique constraint violation on the SourceQuoteId index
    // without referencing Npgsql directly from the Application layer.
    private static bool IsUniqueConstraintViolationOnQuoteId(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;

        // PostgresException (Npgsql) sets SqlState "23505" for unique violations
        // and exposes ConstraintName as a property.
        var sqlState = inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
        var constraintName = inner.GetType().GetProperty("ConstraintName")?.GetValue(inner) as string;

        return sqlState == "23505"
               && constraintName == "UX_DoorOrders_TenantId_SourceQuoteId";
    }
}
