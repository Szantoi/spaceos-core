using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareList;

public sealed record HardwareListResponse(
    Guid OrderId,
    IReadOnlyList<HardwareListItem> Items);
