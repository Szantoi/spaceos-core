using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.FinalizeGyartasilap;

public sealed record FinalizeGyartasilapCommand(
    Guid TenantId,
    Guid GyartasilapId) : IRequest<Result>;
