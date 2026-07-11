using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.ConfigureProduct;

public sealed record ConfigureProductCommand(
    Guid TenantId,
    string ProductType,
    DimensionsDto Dimensions,
    MaterialsDto Materials,
    FittingsDto Fittings,
    Guid? UserId
) : IRequest<Result<ConfigureProductResponse>>;
