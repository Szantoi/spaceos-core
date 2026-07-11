using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetDoorOrder;

public sealed class GetDoorOrderQueryHandler : IRequestHandler<GetDoorOrderQuery, Result<DoorOrderDto>>
{
    private readonly IDoorOrderRepository _repository;

    public GetDoorOrderQueryHandler(IDoorOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DoorOrderDto>> Handle(GetDoorOrderQuery request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<DoorOrderDto>.NotFound($"DoorOrder {request.OrderId} not found.");

        var dto = new DoorOrderDto(
            order.Id,
            order.TenantId,
            order.FlowEpicId,
            order.ProjectId,
            order.ProjectName ?? string.Empty,
            order.Status.ToString(),
            order.Items.Count,
            null,
            DateTime.UtcNow);

        return Result<DoorOrderDto>.Success(dto);
    }
}
