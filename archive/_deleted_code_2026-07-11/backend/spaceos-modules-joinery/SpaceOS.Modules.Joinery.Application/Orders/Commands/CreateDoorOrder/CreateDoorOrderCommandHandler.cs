using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateDoorOrder;

public sealed class CreateDoorOrderCommandHandler : IRequestHandler<CreateDoorOrderCommand, Result<Guid>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IMediator _mediator;

    public CreateDoorOrderCommandHandler(IDoorOrderRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(CreateDoorOrderCommand request, CancellationToken ct)
    {
        var createResult = DoorOrder.Create(
            request.TenantId,
            request.ProjectId,
            request.ProjectName,
            request.FlowEpicId);

        if (!createResult.IsSuccess)
            return Result<Guid>.Invalid(createResult.ValidationErrors);

        var order = createResult.Value;

        await _repository.AddAsync(order, ct).ConfigureAwait(false);

        var events = order.PopDomainEvents();
        await DomainEventDispatcher.DispatchAsync(_mediator, events, ct).ConfigureAwait(false);

        return Result<Guid>.Success(order.Id);
    }
}
