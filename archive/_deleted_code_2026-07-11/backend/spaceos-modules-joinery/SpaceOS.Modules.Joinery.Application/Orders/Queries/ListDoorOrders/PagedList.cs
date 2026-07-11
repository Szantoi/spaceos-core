namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
