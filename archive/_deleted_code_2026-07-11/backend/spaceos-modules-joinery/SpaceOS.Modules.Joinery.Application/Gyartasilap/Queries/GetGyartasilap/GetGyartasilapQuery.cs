using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetGyartasilap;

public sealed record GetGyartasilapQuery(
    Guid TenantId,
    Guid GyartasilapId) : IRequest<Result<GetGyartasilapResponse>>;
