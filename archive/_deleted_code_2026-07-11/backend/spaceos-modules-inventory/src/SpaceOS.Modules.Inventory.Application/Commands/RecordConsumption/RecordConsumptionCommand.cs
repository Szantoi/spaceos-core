using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;

public sealed record RecordConsumptionCommand(
    Guid TenantId,
    string MaterialType,
    decimal Thickness,
    decimal Area,
    int PanelCount,
    string Reason,
    DateTime OccurredAt) : IRequest<Result>;
