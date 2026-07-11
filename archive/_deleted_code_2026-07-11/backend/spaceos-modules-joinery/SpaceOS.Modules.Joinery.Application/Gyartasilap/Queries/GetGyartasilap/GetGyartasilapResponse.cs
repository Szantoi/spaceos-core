using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetGyartasilap;

public sealed record GetGyartasilapResponse(
    Guid Id,
    Guid JoineryOrderId,
    Guid? CuttingPlanId,
    string LabelVariant,
    string Version,
    GyartasilapStatus Status,
    string? StorageUrl,
    bool HasPdfContent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
