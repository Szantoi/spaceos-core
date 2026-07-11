using System.IO.Compression;
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;

/// <summary>
/// Fetches the requested Gyártásilap PDFs, zips them, stores in MinIO,
/// and persists a GyartasilapBatch record with the resulting path.
/// </summary>
public sealed class GenerateBatchCommandHandler
    : IRequestHandler<GenerateBatchCommand, Result<GenerateBatchResponse>>
{
    private readonly IGyartasilapRepository _gyartasilapRepository;
    private readonly IGyartasilapBatchRepository _batchRepository;
    private readonly IGyartasilapStorage _storage;
    private readonly ILogger<GenerateBatchCommandHandler> _logger;

    public GenerateBatchCommandHandler(
        IGyartasilapRepository gyartasilapRepository,
        IGyartasilapBatchRepository batchRepository,
        IGyartasilapStorage storage,
        ILogger<GenerateBatchCommandHandler> logger)
    {
        _gyartasilapRepository = gyartasilapRepository;
        _batchRepository = batchRepository;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<GenerateBatchResponse>> Handle(
        GenerateBatchCommand request,
        CancellationToken ct)
    {
        if (request.GyartasilapIds is null || request.GyartasilapIds.Count == 0)
            return Result<GenerateBatchResponse>.Invalid(
                new ValidationError("GyartasilapIds", "At least one Gyártásilap ID is required."));

        // 1. Create aggregate
        var createResult = GyartasilapBatch.Create(
            request.OrderId, request.TenantId, request.GyartasilapIds);

        if (!createResult.IsSuccess)
            return Result<GenerateBatchResponse>.Invalid(createResult.ValidationErrors);

        var batch = createResult.Value;
        batch.MarkGenerating();

        // 2. Fetch all PDFs from DB
        var zipBytes = await BuildZipAsync(
            request.TenantId, request.GyartasilapIds, ct).ConfigureAwait(false);

        // 3. Upload ZIP to MinIO
        string? zipPath = null;
        try
        {
            zipPath = await _storage.StoreZipAsync(
                request.TenantId, batch.Id, zipBytes, ct).ConfigureAwait(false);

            var readyResult = batch.MarkReady(zipPath);
            if (!readyResult.IsSuccess)
            {
                batch.MarkFailed();
                _logger.LogWarning(
                    "GyartasilapBatch {BatchId}: MarkReady failed — {Errors}",
                    batch.Id, string.Join(", ", readyResult.ValidationErrors.Select(e => e.ErrorMessage)));
            }
        }
        catch (Exception ex)
        {
            batch.MarkFailed();
            _logger.LogError(ex,
                "GyartasilapBatch {BatchId}: ZIP upload to MinIO failed.", batch.Id);
        }

        // 4. Persist batch record
        await _batchRepository.AddAsync(batch, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "GyartasilapBatch {BatchId} for order {OrderId} status={Status}.",
            batch.Id, request.OrderId, batch.Status);

        return Result<GenerateBatchResponse>.Success(new GenerateBatchResponse(
            batch.Id,
            batch.ZipStoragePath,
            (BatchStatusDto)(int)batch.Status));
    }

    private async Task<byte[]> BuildZipAsync(
        Guid tenantId,
        IReadOnlyList<Guid> ids,
        CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < ids.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var id = ids[i];
                var gyartasilap = await _gyartasilapRepository
                    .GetByIdAsync(id, tenantId, ct).ConfigureAwait(false);

                if (gyartasilap?.PdfContent is not { Length: > 0 } pdfBytes)
                {
                    _logger.LogWarning(
                        "Gyártásilap {Id} not found or has no PDF content — skipped in batch.", id);
                    continue;
                }

                var entryName = $"gyartasilap_{i + 1:D3}_{id:N}.pdf";
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(pdfBytes, ct).ConfigureAwait(false);
            }
        }

        ms.Position = 0;
        return ms.ToArray();
    }
}
