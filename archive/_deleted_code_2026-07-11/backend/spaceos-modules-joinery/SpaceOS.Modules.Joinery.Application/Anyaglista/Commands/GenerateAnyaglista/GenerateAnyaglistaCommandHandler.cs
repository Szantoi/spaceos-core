using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Anyaglista.Commands.GenerateAnyaglista;

/// <summary>
/// Fetches the DoorOrder, aggregates material rows from DoorItems,
/// builds a PDF via IAnyaglistaPdfBuilder, uploads to MinIO,
/// and persists an Anyaglista entity.
/// </summary>
public sealed class GenerateAnyaglistaCommandHandler
    : IRequestHandler<GenerateAnyaglistaCommand, Result<GenerateAnyaglistaResponse>>
{
    private readonly IDoorOrderRepository _orderRepository;
    private readonly IAnyaglistaRepository _anyaglistaRepository;
    private readonly IAnyaglistaPdfBuilder _pdfBuilder;
    private readonly IGyartasilapStorage _storage;
    private readonly ILogger<GenerateAnyaglistaCommandHandler> _logger;

    public GenerateAnyaglistaCommandHandler(
        IDoorOrderRepository orderRepository,
        IAnyaglistaRepository anyaglistaRepository,
        IAnyaglistaPdfBuilder pdfBuilder,
        IGyartasilapStorage storage,
        ILogger<GenerateAnyaglistaCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _anyaglistaRepository = anyaglistaRepository;
        _pdfBuilder = pdfBuilder;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<GenerateAnyaglistaResponse>> Handle(
        GenerateAnyaglistaCommand request,
        CancellationToken ct)
    {
        // 1. Fetch the JoineryOrder
        var order = await _orderRepository
            .GetByIdAsync(request.OrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (order is null)
            return Result<GenerateAnyaglistaResponse>.NotFound(
                $"JoineryOrder {request.OrderId} not found.");

        // 2. Aggregate material rows from DoorItems
        var rows = order.Items
            .Select(item => new AnyaglistaRow(
                MaterialType: item.DoorType.ToString(),
                SupplierCode: item.DoorType.ToString(),
                Quantity: item.Quantity,
                Unit: "db",
                Notes: null))
            .ToList();

        var data = new AnyaglistaData(
            OrderId: order.Id,
            CustomerName: order.ProjectName ?? order.ProjectId,
            GeneratedAt: DateTimeOffset.UtcNow,
            Rows: rows);

        // 3. Build PDF (synchronous, pure function)
        var pdfBytes = _pdfBuilder.GeneratePdf(data);

        // 4. Upload to MinIO
        string storageUrl;
        try
        {
            storageUrl = await _storage.StoreAnyaglistaPdfAsync(
                request.TenantId, request.OrderId, pdfBytes, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Anyaglista upload failed for order {OrderId}.", request.OrderId);
            return Result<GenerateAnyaglistaResponse>.Error(
                "Failed to store Anyaglista PDF.");
        }

        // 5. Create and persist entity
        var anyaglista = Domain.Core.Anyaglista.Create(request.OrderId, request.TenantId);
        anyaglista.SetPdf(pdfBytes, storageUrl);

        await _anyaglistaRepository.AddAsync(anyaglista, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Anyaglista {Id} generated for order {OrderId}. StorageUrl={Url}",
            anyaglista.Id, request.OrderId, storageUrl);

        return Result<GenerateAnyaglistaResponse>.Success(
            new GenerateAnyaglistaResponse(anyaglista.Id, storageUrl));
    }
}
