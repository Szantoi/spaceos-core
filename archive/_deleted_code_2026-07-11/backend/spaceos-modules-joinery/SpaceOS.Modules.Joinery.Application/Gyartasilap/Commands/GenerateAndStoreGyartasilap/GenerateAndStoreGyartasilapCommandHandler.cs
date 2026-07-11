using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;

public sealed class GenerateAndStoreGyartasilapCommandHandler
    : IRequestHandler<GenerateAndStoreGyartasilapCommand, Result<GenerateAndStoreGyartasilapResponse>>
{
    private readonly IDoorOrderRepository _orderRepository;
    private readonly IGyartasilapRepository _gyartasilapRepository;
    private readonly IGyartasilapPdfBuilder _pdfBuilder;
    private readonly IGyartasilapStorage _storage;
    private readonly ILogger<GenerateAndStoreGyartasilapCommandHandler> _logger;

    public GenerateAndStoreGyartasilapCommandHandler(
        IDoorOrderRepository orderRepository,
        IGyartasilapRepository gyartasilapRepository,
        IGyartasilapPdfBuilder pdfBuilder,
        IGyartasilapStorage storage,
        ILogger<GenerateAndStoreGyartasilapCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _gyartasilapRepository = gyartasilapRepository;
        _pdfBuilder = pdfBuilder;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<GenerateAndStoreGyartasilapResponse>> Handle(
        GenerateAndStoreGyartasilapCommand request,
        CancellationToken ct)
    {
        // 1. Fetch the JoineryOrder
        var order = await _orderRepository
            .GetByIdAsync(request.JoineryOrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (order is null)
            return Result<GenerateAndStoreGyartasilapResponse>.NotFound(
                $"JoineryOrder {request.JoineryOrderId} not found.");

        // 2. Create the domain aggregate (validates variant)
        var createResult = Domain.Core.Gyartasilap.Create(
            request.TenantId,
            request.JoineryOrderId,
            request.CuttingPlanId,
            request.LabelVariant);

        if (!createResult.IsSuccess)
            return Result<GenerateAndStoreGyartasilapResponse>.Invalid(createResult.ValidationErrors);

        var gyartasilap = createResult.Value;

        // 3. Build PDF
        var pdfBytes = await _pdfBuilder.GeneratePdfAsync(
            order.Id.ToString("N")[..8].ToUpperInvariant(),
            order.ProjectName,
            request.LabelVariant,
            cancellationToken: ct).ConfigureAwait(false);

        // 4. Store in MinIO WORM bucket (fire-and-forget safe — fallback to BYTEA)
        string? storageUrl = null;
        try
        {
            var planId = request.CuttingPlanId ?? Guid.Empty;
            storageUrl = await _storage.StoreAsync(
                request.TenantId,
                planId,
                request.LabelVariant,
                pdfBytes,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // MinIO failure is non-critical — PDF is stored in DB as fallback
            _logger.LogWarning(ex,
                "MinIO upload failed for Gyártásilap {GyartasilapId}. Falling back to DB BYTEA storage.",
                gyartasilap.Id);
        }

        // 5. Update aggregate with storage info
        var updateResult = gyartasilap.UpdateStorage(pdfBytes, storageUrl);
        if (!updateResult.IsSuccess)
            return Result<GenerateAndStoreGyartasilapResponse>.Invalid(updateResult.ValidationErrors);

        // 6. Persist
        await _gyartasilapRepository.AddAsync(gyartasilap, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Gyártásilap {Id} generated for order {OrderId} variant {Variant}. StorageUrl={Url}",
            gyartasilap.Id, request.JoineryOrderId, request.LabelVariant, storageUrl ?? "DB");

        return Result<GenerateAndStoreGyartasilapResponse>.Success(
            new GenerateAndStoreGyartasilapResponse(
                gyartasilap.Id,
                gyartasilap.StorageUrl,
                gyartasilap.Status,
                gyartasilap.LabelVariant,
                gyartasilap.CreatedAt));
    }
}
