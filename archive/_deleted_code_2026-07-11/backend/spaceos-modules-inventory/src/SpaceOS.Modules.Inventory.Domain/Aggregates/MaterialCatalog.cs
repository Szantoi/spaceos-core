using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

public class MaterialCatalog : AggregateRoot
{
    public Guid Id { get; private set; }
    public string MaterialType { get; private set; } = string.Empty;
    public decimal StandardWidth { get; private set; }
    public decimal StandardHeight { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public decimal UnitCost { get; private set; }
    public string SupplierRef { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ReorderPoint { get; private set; } = 5;
    public int SuggestedOrderQuantity { get; private set; } = 10;
    public string UnitOfMeasure { get; private set; } = "pcs";
    public Guid? PreferredSupplierId { get; private set; }

    private MaterialCatalog() { }

    public static MaterialCatalog Create(
        string materialType,
        decimal standardWidth,
        decimal standardHeight,
        decimal thicknessMm,
        decimal unitCost,
        string supplierRef,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialType);
        if (standardWidth <= 0) throw new ArgumentException("Width must be positive.", nameof(standardWidth));
        if (standardHeight <= 0) throw new ArgumentException("Height must be positive.", nameof(standardHeight));
        if (thicknessMm <= 0) throw new ArgumentException("Thickness must be positive.", nameof(thicknessMm));

        return new MaterialCatalog
        {
            Id = Guid.NewGuid(),
            MaterialType = materialType,
            StandardWidth = standardWidth,
            StandardHeight = standardHeight,
            ThicknessMm = thicknessMm,
            UnitCost = unitCost,
            SupplierRef = supplierRef,
            Description = description
        };
    }
}
