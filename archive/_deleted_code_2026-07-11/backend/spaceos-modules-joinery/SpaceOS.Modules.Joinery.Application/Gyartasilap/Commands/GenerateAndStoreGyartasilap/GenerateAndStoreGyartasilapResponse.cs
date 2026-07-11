using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;

public sealed record GenerateAndStoreGyartasilapResponse(
    Guid GyartasilapId,
    string? StorageUrl,
    GyartasilapStatus Status,
    string LabelVariant,
    DateTimeOffset CreatedAt);
