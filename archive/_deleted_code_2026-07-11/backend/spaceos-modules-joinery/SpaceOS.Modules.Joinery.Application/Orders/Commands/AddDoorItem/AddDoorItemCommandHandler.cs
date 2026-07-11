using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;

public sealed class AddDoorItemCommandHandler : IRequestHandler<AddDoorItemCommand, Result<Guid>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IMediator _mediator;

    public AddDoorItemCommandHandler(IDoorOrderRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(AddDoorItemCommand request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<Guid>.NotFound($"DoorOrder {request.OrderId} not found.");

        var dimsResult = DoorDimensions.Create(
            request.WallOpeningWidth,
            request.DoorWidth,
            request.WallOpeningHeight,
            request.DoorHeight,
            request.WallOpeningThickness,
            request.DoorThickness);

        if (!dimsResult.IsSuccess)
            return Result<Guid>.Invalid(dimsResult.ValidationErrors);

        var doorType = Enum.Parse<DoorType>(request.DoorType);
        var openingDirection = Enum.Parse<OpeningDirection>(request.OpeningDirection);

        var item = DoorItem.Create(order.Id, request.Sorszam, request.Quantity, doorType, openingDirection, dimsResult.Value);
        item.SetName(request.Name);

        var addResult = order.AddItem(item);
        if (!addResult.IsSuccess)
            return Result<Guid>.Invalid(addResult.ValidationErrors);

        await _repository.UpdateAsync(order, ct).ConfigureAwait(false);

        var events = order.PopDomainEvents();
        await DomainEventDispatcher.DispatchAsync(_mediator, events, ct).ConfigureAwait(false);

        return Result<Guid>.Success(item.Id);
    }
}
