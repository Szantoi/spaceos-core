using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;

public sealed record RecordOffcutCommand(
    Guid TenantId,
    string MaterialType,
    decimal WidthMm,
    decimal HeightMm,
    Guid? OriginCuttingSheetId) : IRequest<Result>;
