using System.Text.Json;
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.SubmitDoorOrder;

public sealed class SubmitDoorOrderCommandHandler : IRequestHandler<SubmitDoorOrderCommand, Result>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;
    private readonly IMediator _mediator;
    private readonly ICuttingProvider _cuttingProvider;
    private readonly ILogger<SubmitDoorOrderCommandHandler> _logger;

    public SubmitDoorOrderCommandHandler(
        IDoorOrderRepository repository,
        IOutboxWriter outboxWriter,
        IClock clock,
        IMediator mediator,
        ICuttingProvider cuttingProvider,
        ILogger<SubmitDoorOrderCommandHandler> logger)
    {
        _repository = repository;
        _outboxWriter = outboxWriter;
        _clock = clock;
        _mediator = mediator;
        _cuttingProvider = cuttingProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(SubmitDoorOrderCommand request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result.NotFound($"DoorOrder {request.OrderId} not found.");

        var submitResult = order.Submit();
        if (!submitResult.IsSuccess)
            return Result.Invalid(submitResult.ValidationErrors);

        var now = _clock.UtcNow;
        var outboxEntries = order.Items
            .Select(item => JoineryOutboxEntry.Create(
                order.TenantId,
                "DoorItemCalculationRequested",
                JsonSerializer.Serialize(new
                {
                    doorOrderId = order.Id,
                    doorItemId = item.Id,
                    tenantId = order.TenantId,
                    templateName = item.DoorType.ToString(),
                    inputWidth = item.Dimensions.DoorWidth,
                    inputHeight = item.Dimensions.DoorHeight
                }),
                now))
            .ToList();

        _outboxWriter.AddRange(outboxEntries);
        await _outboxWriter.SaveAsync(ct).ConfigureAwait(false);

        var cuttingSheet = BuildCuttingSheet(order);
        try
        {
            await _cuttingProvider.SubmitCuttingSheetAsync(cuttingSheet, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "CuttingProvider failed for order {OrderId} — degrading gracefully, submit continues",
                order.Id);
        }

        var events = order.PopDomainEvents();
        await DomainEventDispatcher.DispatchAsync(_mediator, events, ct).ConfigureAwait(false);

        return Result.Success();
    }

    private static CuttingSheetDto BuildCuttingSheet(DoorOrder order)
    {
        var lines = order.Items.Select(item => new CuttingLineDto(
            Name:        item.Sorszam,
            PartType:    item.DoorType.ToString(),
            RawWidth:    item.Dimensions.DoorWidth,
            RawHeight:   item.Dimensions.DoorHeight,
            Thickness:   item.Dimensions.DoorThickness,
            Quantity:    item.Quantity,
            CanRotate:   false,
            EdgeBanding: null
        )).ToList();

        return new CuttingSheetDto(
            Id:            Guid.NewGuid(),
            TenantId:      order.TenantId,
            SourceOrderId: order.Id,
            Lines:         lines,
            MaterialType:  "DOOR",
            CreatedAt:     DateTime.UtcNow);
    }
}
