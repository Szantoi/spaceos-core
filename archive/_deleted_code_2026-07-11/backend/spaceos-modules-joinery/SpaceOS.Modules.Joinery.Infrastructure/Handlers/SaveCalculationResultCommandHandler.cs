using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.SaveCalculationResult;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="SaveCalculationResultCommand"/>.
/// Lives in Infrastructure because it needs direct <see cref="JoineryDbContext"/> access
/// for the concurrency-safe snapshot upsert pattern (DB-03).
/// </summary>
public sealed class SaveCalculationResultCommandHandler : IRequestHandler<SaveCalculationResultCommand, Result>
{
    private readonly JoineryDbContext _db;
    private readonly IDoorOrderRepository _orderRepository;
    private readonly IClock _clock;
    private readonly IMediator _mediator;

    public SaveCalculationResultCommandHandler(
        JoineryDbContext db,
        IDoorOrderRepository orderRepository,
        IClock clock,
        IMediator mediator)
    {
        _db = db;
        _orderRepository = orderRepository;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<Result> Handle(SaveCalculationResultCommand request, CancellationToken ct)
    {
        // DB-03: demote existing latest snapshot for this item before creating a new one
        var oldSnapshot = await _db.CuttingListSnapshots
            .FirstOrDefaultAsync(s => s.DoorItemId == request.DoorItemId && s.IsLatest, ct)
            .ConfigureAwait(false);

        oldSnapshot?.MarkNotLatest();

        CuttingListSnapshot snapshot;
        try
        {
            snapshot = CuttingListSnapshot.Create(
                request.TenantId,
                request.DoorOrderId,
                request.DoorItemId,
                request.TemplateName,
                request.TemplateVersion,
                request.InputWidth,
                request.InputHeight,
                request.ParameterOverridesJson,
                _clock.UtcNow,
                request.Lines,
                request.CncInstructions,
                request.ProcessSteps);
        }
        catch (ArgumentException ex)
        {
            return Result.Error(ex.Message);
        }

        _db.CuttingListSnapshots.Add(snapshot);

        // Check whether all items now have a latest snapshot (order may be ready to transition)
        var order = await _orderRepository.GetByIdAsync(request.DoorOrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (order is null)
            return Result.NotFound($"DoorOrder {request.DoorOrderId} not found.");

        var allItemIds = order.Items.Select(i => i.Id).ToHashSet();

        var existingDoneItemIds = await _db.CuttingListSnapshots
            .AsNoTracking()
            .Where(s => s.DoorOrderId == request.DoorOrderId && s.IsLatest)
            .Select(s => s.DoorItemId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Include the item being saved now (not yet persisted)
        var updatedDoneIds = existingDoneItemIds.ToHashSet();
        updatedDoneIds.Add(request.DoorItemId);

        if (allItemIds.SetEquals(updatedDoneIds))
        {
            var markResult = order.MarkCalculated();
            if (!markResult.IsSuccess)
                return Result.Invalid(markResult.ValidationErrors);
        }

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // BE-01: another consumer already called MarkCalculated concurrently — will be retried by outbox
            return Result.Error("Concurrency conflict — will be retried");
        }

        await DomainEventDispatcher.DispatchAsync(_mediator, order.PopDomainEvents(), ct).ConfigureAwait(false);

        return Result.Success();
    }
}
