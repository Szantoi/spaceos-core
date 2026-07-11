using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetBatchStatus;

/// <summary>
/// Returns the current generation status for a GyartasilapBatch.
/// RLS is enforced at the DB level via TenantId — the tenantId param is included
/// as an extra guard at application level.
/// </summary>
public sealed class GetBatchStatusQueryHandler
    : IRequestHandler<GetBatchStatusQuery, Result<GetBatchStatusResponse>>
{
    private readonly IGyartasilapBatchRepository _batchRepository;

    public GetBatchStatusQueryHandler(IGyartasilapBatchRepository batchRepository)
    {
        _batchRepository = batchRepository;
    }

    public async Task<Result<GetBatchStatusResponse>> Handle(
        GetBatchStatusQuery request,
        CancellationToken ct)
    {
        var batch = await _batchRepository
            .GetByIdAsync(request.BatchId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (batch is null)
            return Result<GetBatchStatusResponse>.NotFound(
                $"GyartasilapBatch {request.BatchId} not found.");

        return Result<GetBatchStatusResponse>.Success(new GetBatchStatusResponse(
            batch.Id,
            (BatchStatusDto)(int)batch.Status,
            batch.ZipStoragePath,
            batch.CreatedAt));
    }
}
