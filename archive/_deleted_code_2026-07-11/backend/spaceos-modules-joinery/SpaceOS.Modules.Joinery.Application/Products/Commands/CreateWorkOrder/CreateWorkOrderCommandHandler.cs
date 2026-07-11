using System.Text.Json;
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Application.Products.Services;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, Result<CreateWorkOrderResponse>>
{
    private readonly IProductConfigurationRepository _configRepository;
    private readonly IProductTemplateRepository _templateRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IWorkOrderPdfService _pdfService;

    public CreateWorkOrderCommandHandler(
        IProductConfigurationRepository configRepository,
        IProductTemplateRepository templateRepository,
        IWorkOrderRepository workOrderRepository,
        IWorkOrderPdfService pdfService)
    {
        _configRepository = configRepository;
        _templateRepository = templateRepository;
        _workOrderRepository = workOrderRepository;
        _pdfService = pdfService;
    }

    public async Task<Result<CreateWorkOrderResponse>> Handle(CreateWorkOrderCommand request, CancellationToken ct)
    {
        // 1. Get configuration
        var config = await _configRepository.GetByIdAsync(request.ConfigurationId, request.TenantId, ct).ConfigureAwait(false);
        if (config is null)
            return Result<CreateWorkOrderResponse>.NotFound($"Configuration '{request.ConfigurationId}' not found.");

        // 2. Get template for lead time
        var template = await _templateRepository.GetByIdAsync(config.ProductType, ct).ConfigureAwait(false);
        var leadTimeDays = template?.LeadTimeDays ?? 7;

        // 3. Parse BOM from config and multiply by quantity
        var configBom = ParseBomItems(config.BomSnapshot);
        var workOrderBomItems = CalculateWorkOrderBom(configBom, request.Quantity);

        // 4. Calculate costs
        var totalMaterialCost = workOrderBomItems.Sum(i => i.TotalPrice);
        var laborPerUnit = template != null ? ParseLaborRate(template.PricingRules) : 5000m;
        var estimatedLabor = laborPerUnit * request.Quantity;
        var totalCost = totalMaterialCost + estimatedLabor;

        // 5. Calculate schedule
        var scheduledStart = request.DeliveryDate.AddDays(-leadTimeDays);
        var estimatedCompletion = request.DeliveryDate.AddDays(-1);

        // Ensure scheduled start is not in the past
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (scheduledStart < today)
            scheduledStart = today;

        // 6. Create work order
        var workOrder = WorkOrder.Create(
            request.TenantId,
            request.ConfigurationId,
            request.Quantity,
            request.DeliveryDate,
            request.CustomerRef,
            request.Notes,
            JsonSerializer.Serialize(workOrderBomItems),
            totalMaterialCost,
            estimatedLabor,
            totalCost,
            scheduledStart,
            estimatedCompletion,
            request.UserId);

        await _workOrderRepository.AddAsync(workOrder, ct).ConfigureAwait(false);

        // 7. Generate PDF
        var pdfUrl = await _pdfService.GenerateWorkOrderPdfAsync(workOrder, config, ct).ConfigureAwait(false);
        workOrder.SetPdfUrl(pdfUrl);
        await _workOrderRepository.UpdateAsync(workOrder, ct).ConfigureAwait(false);

        // 8. Build response
        var response = new CreateWorkOrderResponse(
            workOrder.Id.ToString(),
            pdfUrl,
            workOrderBomItems,
            totalMaterialCost,
            estimatedLabor,
            totalCost,
            scheduledStart,
            estimatedCompletion);

        return Result<CreateWorkOrderResponse>.Success(response);
    }

    private static IReadOnlyList<BomPreviewItem> ParseBomItems(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<BomPreviewItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<BomPreviewItem>();
        }
        catch
        {
            return new List<BomPreviewItem>();
        }
    }

    private static decimal ParseLaborRate(string pricingRulesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(pricingRulesJson);
            if (doc.RootElement.TryGetProperty("laborRate", out var laborRate) ||
                doc.RootElement.TryGetProperty("LaborRate", out laborRate))
            {
                return laborRate.GetDecimal();
            }
        }
        catch { }
        return 5000m;
    }

    private static IReadOnlyList<WorkOrderBomItem> CalculateWorkOrderBom(
        IReadOnlyList<BomPreviewItem> configBom,
        int quantity)
    {
        // Mock suppliers and inventory for Phase 1
        // In Phase 2, this will integrate with Inventory module via Orchestrator
        var suppliers = new Dictionary<string, string>
        {
            ["material"] = "HolzMaster Kft.",
            ["veneer"] = "Furnér Centrum",
            ["edge"] = "Élzáró Direct",
            ["fitting"] = "Vasalat Direct"
        };

        return configBom.Select((item, index) =>
        {
            var totalQuantity = item.Quantity * quantity;

            // Deterministic mock inventory calculation based on item hash (pure function)
            // This ensures repeatability without using Random
            var itemHash = (item.Name.GetHashCode() & 0x7FFFFFFF) % 100;
            var stockPercentage = itemHash / 100m;
            var inStock = (int)(totalQuantity * stockPercentage);
            var toOrder = Math.Max(0, (int)totalQuantity - inStock);

            return new WorkOrderBomItem(
                ItemType: item.ItemType,
                Name: item.Name,
                Quantity: totalQuantity,
                Unit: item.Unit,
                TotalPrice: item.TotalPrice * quantity,
                Supplier: suppliers.GetValueOrDefault(item.ItemType, "Unknown"),
                InStock: inStock,
                ToOrder: toOrder
            );
        }).ToList();
    }
}
