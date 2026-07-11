using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;

public sealed record RecordInboundCommand(
    Guid TenantId,
    string MaterialType,
    decimal Thickness,
    decimal Area,
    int PanelCount,
    string Reference,
    DateTime OccurredAt) : IRequest<Result>;
