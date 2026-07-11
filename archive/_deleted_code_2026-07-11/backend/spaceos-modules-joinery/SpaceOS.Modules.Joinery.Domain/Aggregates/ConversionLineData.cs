namespace SpaceOS.Modules.Joinery.Domain.Aggregates;

/// <summary>
/// Value object carrying line data into <see cref="DoorOrder.CreateFromConversion"/>.
/// Keeps raw DTO values out of the entity while the Domain builds validated
/// <see cref="Entities.DoorOrderConvertedLine"/> instances internally.
/// </summary>
public sealed record ConversionLineData(
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceNet,
    decimal VatRate,
    decimal? DiscountPercent,
    int SortOrder);
