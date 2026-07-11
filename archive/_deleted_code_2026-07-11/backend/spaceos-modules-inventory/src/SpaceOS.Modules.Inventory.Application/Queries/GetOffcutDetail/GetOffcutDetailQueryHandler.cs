using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutDetail;

public sealed class GetOffcutDetailQueryHandler
    : IRequestHandler<GetOffcutDetailQuery, Result<GetOffcutDetailResponse>>
{
    private readonly IInventoryRepository _repository;

    public GetOffcutDetailQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetOffcutDetailResponse>> Handle(GetOffcutDetailQuery request, CancellationToken ct)
    {
        var offcut = await _repository.GetOffcutByIdAsync(request.OffcutId, ct).ConfigureAwait(false);
        if (offcut is null)
            return Result<GetOffcutDetailResponse>.NotFound($"Offcut {request.OffcutId} not found.");

        var reservations = await _repository
            .GetReservationsByOffcutIdAsync(request.OffcutId, ct)
            .ConfigureAwait(false);

        var history = reservations
            .Select(r => new ReservationHistoryItem(r.Id, r.JobId, r.Status.ToString(), r.CreatedAt, r.ExpiresAt))
            .ToList();

        return Result<GetOffcutDetailResponse>.Success(new GetOffcutDetailResponse(
            offcut.Id, offcut.MaterialCode, offcut.WidthMm, offcut.HeightMm, offcut.ThicknessMm,
            offcut.VolumeM3, offcut.WeightKg, offcut.Status.ToString(),
            offcut.CreatedAt, offcut.UsedAt, offcut.UsedInJobId, offcut.CuttingJobId, history));
    }
}
