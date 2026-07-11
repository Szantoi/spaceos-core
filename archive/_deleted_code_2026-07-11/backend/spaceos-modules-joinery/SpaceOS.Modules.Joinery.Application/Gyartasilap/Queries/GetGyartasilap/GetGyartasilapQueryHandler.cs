using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetGyartasilap;

public sealed class GetGyartasilapQueryHandler
    : IRequestHandler<GetGyartasilapQuery, Result<GetGyartasilapResponse>>
{
    private readonly IGyartasilapRepository _repository;

    public GetGyartasilapQueryHandler(IGyartasilapRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetGyartasilapResponse>> Handle(
        GetGyartasilapQuery request,
        CancellationToken ct)
    {
        var gyartasilap = await _repository
            .GetByIdAsync(request.GyartasilapId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (gyartasilap is null)
            return Result<GetGyartasilapResponse>.NotFound(
                $"Gyártásilap {request.GyartasilapId} not found.");

        return Result<GetGyartasilapResponse>.Success(
            new GetGyartasilapResponse(
                gyartasilap.Id,
                gyartasilap.JoineryOrderId,
                gyartasilap.CuttingPlanId,
                gyartasilap.LabelVariant,
                gyartasilap.Version,
                gyartasilap.Status,
                gyartasilap.StorageUrl,
                gyartasilap.PdfContent is { Length: > 0 },
                gyartasilap.CreatedAt,
                gyartasilap.UpdatedAt));
    }
}
