using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class PanelStockTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldRaiseDomainEvent()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 10, "A1");
        stock.DomainEvents.Should().ContainSingle(e => e is StockLevelChangedEvent);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => PanelStock.Create(Guid.Empty, CatalogId, 2800, 2070, StockType.FullPanel, 10, "A1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddQuantity_ShouldIncreaseQuantity()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 10, "A1");
        stock.PopDomainEvents();
        stock.AddQuantity(5);
        stock.Quantity.Should().Be(15);
        stock.DomainEvents.Should().ContainSingle(e => e is StockLevelChangedEvent);
    }

    [Fact]
    public void ConsumeQuantity_ShouldDecreaseQuantity()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 10, "A1");
        stock.PopDomainEvents();
        stock.ConsumeQuantity(3);
        stock.Quantity.Should().Be(7);
    }

    [Fact]
    public void ConsumeQuantity_MoreThanAvailable_ShouldThrow()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 5, "A1");
        var act = () => stock.ConsumeQuantity(10);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConsumeQuantity_BelowThreshold_ShouldRaiseLowStockAlert()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 6, "A1");
        stock.PopDomainEvents();
        stock.ConsumeQuantity(3); // → 3 ≤ 5, triggers alert
        stock.DomainEvents.Should().Contain(e => e is LowStockAlertEvent);
    }

    [Fact]
    public void AddQuantity_WithZeroAmount_ShouldThrow()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 5, "A1");
        var act = () => stock.AddQuantity(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PopDomainEvents_ShouldClearEvents()
    {
        var stock = PanelStock.Create(TenantId, CatalogId, 2800, 2070, StockType.FullPanel, 5, "A1");
        stock.DomainEvents.Should().NotBeEmpty();
        stock.PopDomainEvents();
        stock.DomainEvents.Should().BeEmpty();
    }
}
