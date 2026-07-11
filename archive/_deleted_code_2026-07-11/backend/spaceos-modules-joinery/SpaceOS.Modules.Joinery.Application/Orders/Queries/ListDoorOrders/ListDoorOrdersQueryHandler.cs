using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;

public sealed class ListDoorOrdersQueryHandler : IRequestHandler<ListDoorOrdersQuery, Result<PagedList<DoorOrderDto>>>
{
    private readonly IDoorOrderRepository _repository;

    public ListDoorOrdersQueryHandler(IDoorOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedList<DoorOrderDto>>> Handle(ListDoorOrdersQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _repository.ListAsync(request.TenantId, request.Page, request.PageSize, ct).ConfigureAwait(false);

        var pagedList = new PagedList<DoorOrderDto>(items, totalCount, request.Page, request.PageSize);
        return Result<PagedList<DoorOrderDto>>.Success(pagedList);
    }
}
