using SpaceOS.Modules.Inventory.Domain.Common;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

public class PanelStock : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid MaterialCatalogId { get; private set; }
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public StockType StockType { get; private set; }
    public int Quantity { get; private set; }
    public string LocationCode { get; private set; } = string.Empty;

    private PanelStock() { }

    public static PanelStock Create(
        Guid tenantId,
        Guid materialCatalogId,
        decimal widthMm,
        decimal heightMm,
        StockType stockType,
        int quantity,
        string locationCode)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (materialCatalogId == Guid.Empty) throw new ArgumentException("MaterialCatalogId required.", nameof(materialCatalogId));
        if (widthMm <= 0) throw new ArgumentException("Width must be positive.", nameof(widthMm));
        if (heightMm <= 0) throw new ArgumentException("Height must be positive.", nameof(heightMm));
        if (quantity < 0) throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));

        var stock = new PanelStock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaterialCatalogId = materialCatalogId,
            WidthMm = widthMm,
            HeightMm = heightMm,
            StockType = stockType,
            Quantity = quantity,
            LocationCode = locationCode
        };
        stock.RaiseDomainEvent(new StockLevelChangedEvent(stock.Id, stock.TenantId, stock.MaterialCatalogId, stock.Quantity));
        return stock;
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
        Quantity += amount;
        RaiseDomainEvent(new StockLevelChangedEvent(Id, TenantId, MaterialCatalogId, Quantity));
    }

    public void ConsumeQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (amount > Quantity) throw new InvalidOperationException("Insufficient stock.");
        Quantity -= amount;
        RaiseDomainEvent(new StockLevelChangedEvent(Id, TenantId, MaterialCatalogId, Quantity));
        if (Quantity <= 5)
            RaiseDomainEvent(new LowStockAlertEvent(Id, TenantId, MaterialCatalogId, Quantity));
    }
}
