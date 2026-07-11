using System.Text.Json;
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Application.Products.Services;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.ConfigureProduct;

public sealed class ConfigureProductCommandHandler : IRequestHandler<ConfigureProductCommand, Result<ConfigureProductResponse>>
{
    private readonly IProductTemplateRepository _templateRepository;
    private readonly IProductConfigurationRepository _configRepository;
    private readonly IProductConfiguratorService _configuratorService;

    public ConfigureProductCommandHandler(
        IProductTemplateRepository templateRepository,
        IProductConfigurationRepository configRepository,
        IProductConfiguratorService configuratorService)
    {
        _templateRepository = templateRepository;
        _configRepository = configRepository;
        _configuratorService = configuratorService;
    }

    public async Task<Result<ConfigureProductResponse>> Handle(ConfigureProductCommand request, CancellationToken ct)
    {
        // 1. Get template
        var template = await _templateRepository.GetByIdAsync(request.ProductType, ct).ConfigureAwait(false);
        if (template is null)
            return Result<ConfigureProductResponse>.NotFound($"Product type '{request.ProductType}' not found.");

        // 2. Validate against template rules
        var validationResult = _configuratorService.ValidateConfiguration(template, request.Dimensions, request.Materials, request.Fittings);
        if (!validationResult.IsSuccess)
            return Result<ConfigureProductResponse>.Invalid(validationResult.ValidationErrors);

        // 3. Calculate BOM
        var bomItems = _configuratorService.CalculateBom(template, request.Dimensions, request.Materials, request.Fittings);

        // 4. Calculate price
        var estimatedPrice = _configuratorService.CalculatePrice(template, bomItems);

        // 5. Save configuration
        var paramsJson = JsonSerializer.Serialize(new
        {
            dimensions = request.Dimensions,
            materials = request.Materials,
            fittings = request.Fittings
        });

        var bomSnapshotJson = JsonSerializer.Serialize(bomItems);

        var config = ProductConfiguration.Create(
            request.TenantId,
            request.ProductType,
            paramsJson,
            bomSnapshotJson,
            estimatedPrice,
            previewUrl: null,
            request.UserId);

        await _configRepository.AddAsync(config, ct).ConfigureAwait(false);

        // 6. Build response
        var response = new ConfigureProductResponse(
            config.Id.ToString(),
            $"/api/products/preview/{config.Id}.jpg",
            estimatedPrice,
            bomItems);

        return Result<ConfigureProductResponse>.Success(response);
    }
}
