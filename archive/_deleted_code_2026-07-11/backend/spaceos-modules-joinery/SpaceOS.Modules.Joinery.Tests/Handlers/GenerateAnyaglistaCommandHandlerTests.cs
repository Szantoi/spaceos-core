using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Commands.GenerateAnyaglista;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Services;
using AnyaglistaEntity = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

/// <summary>
/// Unit tests for GenerateAnyaglistaCommandHandler.
/// </summary>
public class GenerateAnyaglistaCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly byte[] FakePdfBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF

    private readonly Mock<IDoorOrderRepository> _orderRepo = new();
    private readonly Mock<IAnyaglistaRepository> _anyaglistaRepo = new();
    private readonly Mock<IAnyaglistaPdfBuilder> _pdfBuilder = new();
    private readonly Mock<IGyartasilapStorage> _storage = new();

    private GenerateAnyaglistaCommandHandler CreateHandler() =>
        new(_orderRepo.Object,
            _anyaglistaRepo.Object,
            _pdfBuilder.Object,
            _storage.Object,
            NullLogger<GenerateAnyaglistaCommandHandler>.Instance);

    private DoorOrder CreateValidOrder()
    {
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test Project", Guid.NewGuid());
        return result.Value;
    }

    [Fact]
    public async Task Handle_HappyPath_GeneratesPdfStoresAndSavesEntity()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdf(It.IsAny<AnyaglistaData>()))
            .Returns(FakePdfBytes);
        _storage.Setup(s => s.StoreAnyaglistaPdfAsync(TenantId, OrderId, FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync("anyaglista/tenant/order/anyaglista.pdf");
        _anyaglistaRepo.Setup(r => r.AddAsync(It.IsAny<AnyaglistaEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AnyaglistaId.Should().NotBeEmpty();
        result.Value.StorageUrl.Should().Be("anyaglista/tenant/order/anyaglista.pdf");
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoorOrder?)null);

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_OrderNotFound_NeverCallsPdfBuilder()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoorOrder?)null);

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        _pdfBuilder.Verify(b => b.GeneratePdf(It.IsAny<AnyaglistaData>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StorageFailure_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdf(It.IsAny<AnyaglistaData>()))
            .Returns(FakePdfBytes);
        _storage.Setup(s => s.StoreAnyaglistaPdfAsync(TenantId, OrderId, FakePdfBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MinIO not configured"));

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
    }

    [Fact]
    public async Task Handle_StorageFailure_DoesNotPersistAnyaglista()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdf(It.IsAny<AnyaglistaData>()))
            .Returns(FakePdfBytes);
        _storage.Setup(s => s.StoreAnyaglistaPdfAsync(TenantId, OrderId, FakePdfBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert: entity is NOT persisted when storage fails
        _anyaglistaRepo.Verify(r => r.AddAsync(It.IsAny<AnyaglistaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PersistsAnyaglistaWithCorrectFields()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdf(It.IsAny<AnyaglistaData>()))
            .Returns(FakePdfBytes);
        _storage.Setup(s => s.StoreAnyaglistaPdfAsync(TenantId, OrderId, FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync("url");
        _anyaglistaRepo.Setup(r => r.AddAsync(It.IsAny<AnyaglistaEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateAnyaglistaCommand(TenantId, OrderId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        _anyaglistaRepo.Verify(r => r.AddAsync(
            It.Is<AnyaglistaEntity>(a =>
                a.TenantId == TenantId &&
                a.OrderId == OrderId &&
                a.PdfContent != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
