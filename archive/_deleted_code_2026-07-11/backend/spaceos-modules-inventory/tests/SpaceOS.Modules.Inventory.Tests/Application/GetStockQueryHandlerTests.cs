using FluentAssertions;
using Moq;
using SpaceOS.Modules.Inventory.Application.Queries.GetStock;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Application;

public class GetStockQueryHandlerTests
{
    private readonly Mock<IInventoryRepository> _repoMock = new();
    private readonly GetStockQueryHandler _handler;

    public GetStockQueryHandlerTests()
    {
        _handler = new GetStockQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingMaterial_ShouldReturnSuccess()
    {
        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        var stock = PanelStock.Create(Guid.NewGuid(), catalog.Id, 2800, 2070, StockType.FullPanel, 5, "A1");

        _repoMock.Setup(r => r.GetMaterialCatalogByTypeAsync("MDF 18mm", default)).ReturnsAsync(catalog);
        _repoMock.Setup(r => r.GetStockByMaterialTypeAsync("MDF 18mm", default)).ReturnsAsync(new[] { stock });

        var result = await _handler.Handle(new GetStockQuery("MDF 18mm"), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.FullPanelCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithUnknownMaterial_ShouldReturnNotFound()
    {
        _repoMock.Setup(r => r.GetMaterialCatalogByTypeAsync("UNKNOWN", default)).ReturnsAsync((MaterialCatalog?)null);

        var result = await _handler.Handle(new GetStockQuery("UNKNOWN"), default);
        result.IsSuccess.Should().BeFalse();
    }
}
