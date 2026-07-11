using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.RevertDoorOrder;

/// <summary>
/// Reverts a Calculated or CalculationFailed order back to Draft for re-submission.
/// </summary>
public sealed record RevertDoorOrderCommand(Guid TenantId, Guid OrderId) : IRequest<Result>;
