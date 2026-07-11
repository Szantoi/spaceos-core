using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateOrderFromConversion;

public sealed record CreateOrderFromConversionCommand(
    Guid QuoteId,
    Guid TenantId,
    Guid CustomerId,
    Guid? LinkedTenantId,
    string Currency,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    IReadOnlyList<ConversionLineItemDto> Lines,
    string ContentHash)
    : IRequest<Result<CreateOrderFromConversionResult>>;

public sealed record ConversionLineItemDto(
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceNet,
    decimal VatRate,
    decimal? DiscountPercent,
    int SortOrder);

public sealed record CreateOrderFromConversionResult(Guid OrderId, DateTimeOffset CreatedAt);
