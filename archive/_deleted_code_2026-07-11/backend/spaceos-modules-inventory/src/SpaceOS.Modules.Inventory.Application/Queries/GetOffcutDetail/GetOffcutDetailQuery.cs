using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutDetail;

public record GetOffcutDetailQuery(Guid OffcutId) : IRequest<Result<GetOffcutDetailResponse>>;

public record GetOffcutDetailResponse(
    Guid Id,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    decimal VolumeM3,
    decimal WeightKg,
    string Status,
    DateTime CreatedAt,
    DateTime? UsedAt,
    Guid? UsedInJobId,
    Guid? CuttingJobId,
    IReadOnlyList<ReservationHistoryItem> ReservationHistory);

public record ReservationHistoryItem(
    Guid ReservationId,
    Guid JobId,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
