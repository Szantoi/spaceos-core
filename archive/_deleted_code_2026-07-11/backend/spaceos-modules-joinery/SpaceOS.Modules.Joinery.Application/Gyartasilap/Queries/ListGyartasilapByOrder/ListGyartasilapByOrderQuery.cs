using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.ListGyartasilapByOrder;

public sealed record ListGyartasilapByOrderQuery(
    Guid TenantId,
    Guid JoineryOrderId,
    GyartasilapStatus? Status = null) : IRequest<Result<IReadOnlyList<ListGyartasilapItem>>>;
