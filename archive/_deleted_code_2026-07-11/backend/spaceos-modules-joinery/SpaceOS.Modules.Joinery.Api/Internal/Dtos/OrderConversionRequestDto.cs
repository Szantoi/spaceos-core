using SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateOrderFromConversion;

namespace SpaceOS.Modules.Joinery.Api.Internal.Dtos;

public sealed record OrderConversionRequestDto(
    Guid QuoteId,
    Guid TenantId,
    Guid CustomerId,
    Guid? LinkedTenantId,
    string Currency,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    IReadOnlyList<OrderConversionLineDto> Lines,
    string ContentHash)
{
    public CreateOrderFromConversionCommand ToCommand() => new(
        QuoteId, TenantId, CustomerId, LinkedTenantId,
        Currency, TotalNet, TotalVat, TotalGross,
        Lines.Select(l => new ConversionLineItemDto(
            l.SourceTemplateId, l.Description, l.Quantity,
            l.UnitPriceNet, l.VatRate, l.DiscountPercent, l.SortOrder))
        .ToList().AsReadOnly(),
        ContentHash);
}

public sealed record OrderConversionLineDto(
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceNet,
    decimal VatRate,
    decimal? DiscountPercent,
    int SortOrder);
