using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetManufacturingSheet;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Handlers;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class GetManufacturingSheetHandlerTests : IDisposable
{
    private readonly JoineryDbContext _db;
    private readonly Mock<IProductionSheetGenerator> _generator = new();
    private readonly GetManufacturingSheetQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetManufacturingSheetHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseInMemoryDatabase($"mfg-sheet-tests-{Guid.NewGuid()}")
            .Options;
        _db = new JoineryDbContext(opts);

        _sut = new GetManufacturingSheetQueryHandler(
            _db,
            _generator.Object,
            NullLogger<GetManufacturingSheetQueryHandler>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static DoorOrder MakeDraftOrder()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-MFG", "Mfg Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 860m, 2100m, 2060m, 120m, 40m).Value;
        order.AddItem(DoorItem.Create(order.Id, "M01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var result = await _sut.Handle(
            new GetManufacturingSheetQuery(Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsNotFound()
    {
        var order = MakeDraftOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetManufacturingSheetQuery(order.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidOrder_CallsGeneratorAndReturnsPdfStream()
    {
        var order = MakeDraftOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var fakeBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _generator
            .Setup(g => g.GenerateManufacturingSheet(It.IsAny<DoorOrder>()))
            .Returns(new MemoryStream(fakeBytes));

        var result = await _sut.Handle(
            new GetManufacturingSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Length.Should().Be(fakeBytes.Length);

        _generator.Verify(g => g.GenerateManufacturingSheet(It.IsAny<DoorOrder>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftOrderWithItems_ReturnsPdfStream()
    {
        // Draft order — no calculation required for manufacturing sheet
        var order = MakeDraftOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        _generator
            .Setup(g => g.GenerateManufacturingSheet(It.IsAny<DoorOrder>()))
            .Returns(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

        var result = await _sut.Handle(
            new GetManufacturingSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("manufacturing sheet must work for Draft orders");
    }
}
