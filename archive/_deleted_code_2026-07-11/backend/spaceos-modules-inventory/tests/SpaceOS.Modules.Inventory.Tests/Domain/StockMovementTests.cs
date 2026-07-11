using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class StockMovementTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();

    [Fact]
    public void Record_WithValidData_ShouldSucceed()
    {
        var movement = StockMovement.Record(TenantId, MovementType.Inbound, CatalogId, 10m, DateTime.UtcNow, "REF-001");
        movement.Id.Should().NotBeEmpty();
        movement.MovementType.Should().Be(MovementType.Inbound);
    }

    [Fact]
    public void Record_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => StockMovement.Record(Guid.Empty, MovementType.Inbound, CatalogId, 10m, DateTime.UtcNow, "REF");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_WithZeroQuantity_ShouldThrow()
    {
        var act = () => StockMovement.Record(TenantId, MovementType.Inbound, CatalogId, 0m, DateTime.UtcNow, "REF");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_IsAppendOnly_NoDomainEvents()
    {
        var movement = StockMovement.Record(TenantId, MovementType.Consumption, CatalogId, 5m, DateTime.UtcNow, "CUT-001");
        movement.DomainEvents.Should().BeEmpty();
    }
}
