using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.SubmitDoorOrder;

public sealed record SubmitDoorOrderCommand(Guid TenantId, Guid OrderId) : IRequest<Result>;
