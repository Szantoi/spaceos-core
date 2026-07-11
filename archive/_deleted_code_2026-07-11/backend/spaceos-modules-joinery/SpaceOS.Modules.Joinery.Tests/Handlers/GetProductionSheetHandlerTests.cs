using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProductionSheet;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Handlers;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class GetProductionSheetHandlerTests : IDisposable
{
    private readonly JoineryDbContext _db;
    private readonly Mock<IProductionSheetGenerator> _generator = new();
    private readonly string _tempDir;
    private readonly GetProductionSheetQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetProductionSheetHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseInMemoryDatabase($"pdf-tests-{Guid.NewGuid()}")
            .Options;
        _db = new JoineryDbContext(opts);

        _tempDir = Path.Combine(Path.GetTempPath(), $"joinery_pdf_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var pdfOptions = Options.Create(new PdfOptions { BasePath = _tempDir });

        _sut = new GetProductionSheetQueryHandler(
            _db,
            _generator.Object,
            pdfOptions,
            NullLogger<GetProductionSheetQueryHandler>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DoorOrder MakeCalculatedOrder()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-PDF", "PDF Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "P01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.Submit();
        order.MarkCalculating();
        order.MarkCalculated();
        return order;
    }

    private static CuttingListSnapshot MakeSnapshot(Guid orderId, Guid itemId)
    {
        var lines = new List<CuttingListLine>
        {
            new("Frame Top", "Frame", 900m, 100m, 902m, 102m, "MDF", 18m, 1, 1)
        };
        return CuttingListSnapshot.Create(
            TenantId, orderId, itemId,
            "template-v1", 1,
            900m, 2100m,
            null,
            DateTimeOffset.UtcNow,
            lines);
    }

    private static byte[] MakeFakePdfBytes() => [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var result = await _sut.Handle(
            new GetProductionSheetQuery(Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_OrderWithWrongTenant_ReturnsNotFound()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetProductionSheetQuery(order.Id, Guid.NewGuid()), // different tenant
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_OrderNotCalculated_ReturnsError()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-DRAFT", "Draft Order", Guid.NewGuid()).Value;
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetProductionSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("Calculated"));
    }

    [Fact]
    public async Task Handle_OrderCalculatedButNoSnapshots_ReturnsError()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetProductionSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("snapshot"));
    }

    [Fact]
    public async Task Handle_ValidOrderWithSnapshot_GeneratesPdfAndReturnsStream()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        var pdfBytes = MakeFakePdfBytes();
        _generator
            .Setup(g => g.Generate(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<CuttingListSnapshot>>()))
            .Returns(new MemoryStream(pdfBytes));

        var result = await _sut.Handle(
            new GetProductionSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var returnedBytes = new byte[pdfBytes.Length];
        _ = await result.Value.ReadAsync(returnedBytes);
        returnedBytes.Should().Equal(pdfBytes);

        _generator.Verify(g => g.Generate(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<CuttingListSnapshot>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidOrderWithSnapshot_SavesPdfFileToDisk()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        var pdfBytes = MakeFakePdfBytes();
        _generator
            .Setup(g => g.Generate(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<CuttingListSnapshot>>()))
            .Returns(new MemoryStream(pdfBytes));

        await _sut.Handle(new GetProductionSheetQuery(order.Id, TenantId), CancellationToken.None);

        var cacheEntry = _db.ProductionSheetCaches.SingleOrDefault();
        cacheEntry.Should().NotBeNull();
        File.Exists(cacheEntry!.FilePath).Should().BeTrue();
        (await File.ReadAllBytesAsync(cacheEntry.FilePath)).Should().Equal(pdfBytes);
    }

    [Fact]
    public async Task Handle_ValidOrderWithSnapshot_PersistsCacheRecord()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        _generator
            .Setup(g => g.Generate(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<CuttingListSnapshot>>()))
            .Returns(new MemoryStream(MakeFakePdfBytes()));

        await _sut.Handle(new GetProductionSheetQuery(order.Id, TenantId), CancellationToken.None);

        var cacheEntry = await _db.ProductionSheetCaches.SingleOrDefaultAsync();
        cacheEntry.Should().NotBeNull();
        cacheEntry!.SnapshotId.Should().Be(snapshot.Id);
        cacheEntry.TenantId.Should().Be(TenantId);
        cacheEntry.FileHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_CachedPdfExists_ReturnsCachedStreamWithoutGenerating()
    {
        var order = MakeCalculatedOrder();
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);

        // Create a real cached file
        var tenantDir = Path.Combine(_tempDir, TenantId.ToString("N"));
        Directory.CreateDirectory(tenantDir);
        var cachedFilePath = Path.Combine(tenantDir, $"{order.Id:N}_cached.pdf");
        var cachedBytes = MakeFakePdfBytes();
        await File.WriteAllBytesAsync(cachedFilePath, cachedBytes);

        var cache = ProductionSheetCache.Create(TenantId, snapshot.Id, cachedFilePath, "fakehash", DateTimeOffset.UtcNow);
        _db.ProductionSheetCaches.Add(cache);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetProductionSheetQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _generator.Verify(g => g.Generate(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<CuttingListSnapshot>>()), Times.Never);
    }
}
