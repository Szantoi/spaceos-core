using System.Security.Cryptography;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProductionSheet;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetProductionSheetQuery"/>.
/// Lives in Infrastructure because it needs direct <see cref="JoineryDbContext"/> access
/// and PDF cache file I/O via <see cref="IProductionSheetGenerator"/>.
/// </summary>
public sealed class GetProductionSheetQueryHandler : IRequestHandler<GetProductionSheetQuery, Result<Stream>>
{
    private readonly JoineryDbContext _db;
    private readonly IProductionSheetGenerator _generator;
    private readonly PdfOptions _pdfOptions;
    private readonly ILogger<GetProductionSheetQueryHandler> _logger;

    public GetProductionSheetQueryHandler(
        JoineryDbContext db,
        IProductionSheetGenerator generator,
        IOptions<PdfOptions> pdfOptions,
        ILogger<GetProductionSheetQueryHandler> logger)
    {
        _db = db;
        _generator = generator;
        _pdfOptions = pdfOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Stream>> Handle(GetProductionSheetQuery query, CancellationToken ct)
    {
        var order = await _db.DoorOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.OrderId && o.TenantId == query.TenantId, ct)
            .ConfigureAwait(false);

        if (order is null)
            return Result<Stream>.NotFound("Order not found");

        if (order.Status != DoorOrderStatus.Calculated)
            return Result<Stream>.Error("Order is not in Calculated status");

        var snapshots = await _db.CuttingListSnapshots
            .Include(s => s.Lines)
            .Include(s => s.CncInstructions)
            .Include(s => s.ProcessSteps)
            .Where(s => s.DoorOrderId == query.OrderId && s.IsLatest)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (snapshots.Count == 0)
            return Result<Stream>.Error("No calculation snapshots found for this order");

        // Check PDF cache using the first (representative) snapshot id
        var firstSnapshotId = snapshots[0].Id;
        var cache = await _db.ProductionSheetCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.SnapshotId == firstSnapshotId, ct)
            .ConfigureAwait(false);

        if (cache is not null && File.Exists(cache.FilePath))
        {
            _logger.LogDebug("Returning cached PDF for order {OrderId} from {FilePath}", query.OrderId, cache.FilePath);
            return Result<Stream>.Success(File.OpenRead(cache.FilePath));
        }

        // Generate PDF
        var pdfStream = _generator.Generate(order, snapshots);

        // Compute SHA-256 of PDF content
        var pdfBytes = ReadAllBytes(pdfStream);
        var hash = Convert.ToHexString(SHA256.HashData(pdfBytes)).ToLowerInvariant();

        // Persist to disk: {basePath}/{tenantId}/{orderId}_{hash}.pdf
        var tenantDir = Path.Combine(_pdfOptions.BasePath, order.TenantId.ToString("N"));
        Directory.CreateDirectory(tenantDir);
        var fileName = $"{query.OrderId:N}_{hash}.pdf";
        var filePath = Path.Combine(tenantDir, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes, ct).ConfigureAwait(false);

        // Persist cache record (remove stale entry for same snapshot if it exists)
        var staleCache = await _db.ProductionSheetCaches
            .FirstOrDefaultAsync(c => c.SnapshotId == firstSnapshotId, ct)
            .ConfigureAwait(false);

        if (staleCache is not null)
            _db.ProductionSheetCaches.Remove(staleCache);

        var cacheEntry = ProductionSheetCache.Create(
            order.TenantId,
            firstSnapshotId,
            filePath,
            hash,
            DateTimeOffset.UtcNow);

        _db.ProductionSheetCaches.Add(cacheEntry);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Generated PDF for order {OrderId}, saved to {FilePath}", query.OrderId, filePath);

        return Result<Stream>.Success(new MemoryStream(pdfBytes));
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
