using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

public sealed class DoorOrderConvertedLine
{
    public Guid Id { get; private set; }
    public Guid DoorOrderId { get; private set; }
    public Guid? SourceTemplateId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitPriceNet { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public int SortOrder { get; private set; }

    private DoorOrderConvertedLine() { } // EF Core

    internal static Result<DoorOrderConvertedLine> Create(
        Guid id,
        Guid? sourceTemplateId,
        string description,
        decimal quantity,
        decimal unitPriceNet,
        decimal vatRate,
        decimal? discountPercent,
        int sortOrder)
    {
        if (id == Guid.Empty)
            return Result.Invalid(new ValidationError("Id", "Id required."));
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
            return Result.Invalid(new ValidationError("Description", "Description: 1..500 char required."));
        if (quantity <= 0)
            return Result.Invalid(new ValidationError("Quantity", "Quantity must be > 0."));
        if (unitPriceNet < 0)
            return Result.Invalid(new ValidationError("UnitPriceNet", "UnitPriceNet must be >= 0."));
        if (vatRate is < 0 or > 1)
            return Result.Invalid(new ValidationError("VatRate", "VatRate: 0..1 range required."));
        if (discountPercent.HasValue && discountPercent.Value is < 0 or > 100)
            return Result.Invalid(new ValidationError("DiscountPercent", "DiscountPercent: 0..100 range required."));

        return Result.Success(new DoorOrderConvertedLine
        {
            Id = id,
            SourceTemplateId = sourceTemplateId,
            Description = description,
            Quantity = quantity,
            UnitPriceNet = unitPriceNet,
            VatRate = vatRate,
            DiscountPercent = discountPercent,
            SortOrder = sortOrder
        });
    }
}
