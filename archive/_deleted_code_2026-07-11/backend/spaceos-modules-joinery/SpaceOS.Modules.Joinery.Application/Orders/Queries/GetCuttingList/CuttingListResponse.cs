using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;

public sealed record CuttingListResponse(
    Guid OrderId,
    IReadOnlyList<CuttingListItem> Items,
    int TotalItemCount);
