using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.RegisterOffcutBatch;

public sealed record RegisterOffcutBatchCommand(
    Guid TenantId,
    string SourceType,
    Guid SourceId,
    IReadOnlyList<OffcutItemDto> Items
) : IRequest<Result<RegisterOffcutBatchResponse>>;

public sealed record OffcutItemDto(
    Guid MaterialCatalogId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm
);

public sealed record RegisterOffcutBatchResponse(
    Guid BatchId,
    IReadOnlyList<Guid> OffcutIds,
    bool IsNew
);
