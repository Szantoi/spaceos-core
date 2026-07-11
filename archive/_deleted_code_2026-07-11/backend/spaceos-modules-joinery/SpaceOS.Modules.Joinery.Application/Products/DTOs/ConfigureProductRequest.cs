namespace SpaceOS.Modules.Joinery.Application.Products.DTOs;

public sealed record ConfigureProductRequest(
    string ProductType,
    DimensionsDto Dimensions,
    MaterialsDto Materials,
    FittingsDto Fittings
);

public sealed record DimensionsDto(
    decimal Width,
    decimal Height,
    decimal Thickness
);

public sealed record MaterialsDto(
    string Core,
    string Veneer,
    string Edge
);

public sealed record FittingsDto(
    string Hinge,
    string Handle,
    string Lock
);
