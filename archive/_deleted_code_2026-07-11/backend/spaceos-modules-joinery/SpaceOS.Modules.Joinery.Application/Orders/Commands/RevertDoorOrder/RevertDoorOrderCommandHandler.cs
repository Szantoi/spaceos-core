using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.RevertDoorOrder;

public sealed class RevertDoorOrderCommandHandler : IRequestHandler<RevertDoorOrderCommand, Result>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IMediator _mediator;

    public RevertDoorOrderCommandHandler(IDoorOrderRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Result> Handle(RevertDoorOrderCommand request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result.NotFound($"DoorOrder {request.OrderId} not found.");

        var revertResult = order.RevertToDraft();
        if (!revertResult.IsSuccess)
            return Result.Invalid(revertResult.ValidationErrors);

        await _repository.UpdateAsync(order, ct).ConfigureAwait(false);

        var events = order.PopDomainEvents();
        await DomainEventDispatcher.DispatchAsync(_mediator, events, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
