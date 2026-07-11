using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

/// <summary>
/// Unit tests for GenerateAndStoreGyartasilapCommandHandler covering all handler paths.
/// </summary>
public class GenerateAndStoreGyartasilapHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly byte[] FakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

    private readonly Mock<IDoorOrderRepository> _orderRepo = new();
    private readonly Mock<IGyartasilapRepository> _gyartasilapRepo = new();
    private readonly Mock<IGyartasilapPdfBuilder> _pdfBuilder = new();
    private readonly Mock<IGyartasilapStorage> _storage = new();

    private GenerateAndStoreGyartasilapCommandHandler CreateHandler() =>
        new(
            _orderRepo.Object,
            _gyartasilapRepo.Object,
            _pdfBuilder.Object,
            _storage.Object,
            NullLogger<GenerateAndStoreGyartasilapCommandHandler>.Instance);

    private DoorOrder CreateValidOrder()
    {
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test Project", Guid.NewGuid());
        return result.Value;
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), "L1",
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(TenantId, PlanId, "L1", FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tenant/plan/gyartasilap_L1.pdf");
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.LabelVariant.Should().Be("L1");
        result.Value.StorageUrl.Should().Be("tenant/plan/gyartasilap_L1.pdf");
        result.Value.Status.Should().Be(GyartasilapStatus.Draft);
    }

    [Theory]
    [InlineData("L1")]
    [InlineData("L2")]
    [InlineData("L3")]
    [InlineData("L4")]
    public async Task Handle_AllValidVariants_Succeed(string variant)
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), variant,
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(TenantId, It.IsAny<Guid>(), variant, FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync($"tenant/plan/gyartasilap_{variant}.pdf");
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, null, variant);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LabelVariant.Should().Be(variant);
    }

    [Fact]
    public async Task Handle_WithNullPlanId_GeneratesWithEmptyGuid()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(TenantId, Guid.Empty, "L1", FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tenant/00000000/gyartasilap_L1.pdf");
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, null, "L1");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GyartasilapId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_PersistsToRepository_OnSuccess()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("some/url");
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: repository was called with the gyartasilap
        _gyartasilapRepo.Verify(r => r.AddAsync(
            It.Is<Gyartasilap>(g =>
                g.JoineryOrderId == OrderId &&
                g.TenantId == TenantId &&
                g.LabelVariant == "L1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Order Not Found

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoorOrder?)null);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_NeverCallsPdfBuilder()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoorOrder?)null);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _pdfBuilder.Verify(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<MaterialItem>?>(),
            It.IsAny<IReadOnlyList<JobItem>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region MinIO Failure Fallback

    [Fact]
    public async Task Handle_WhenMinIOFails_StillSucceedsWithDbFallback()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MinIO connection refused"));
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: handler succeeds with DB fallback (storageUrl is null)
        result.IsSuccess.Should().BeTrue();
        result.Value.StorageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenMinIOFails_PdfBytesStoredInDb()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: the persisted gyartasilap has pdf bytes (DB fallback)
        _gyartasilapRepo.Verify(r => r.AddAsync(
            It.Is<Gyartasilap>(g => g.PdfContent != null && g.PdfContent.Length > 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Response Shape Tests

    [Fact]
    public async Task Handle_ResponseContainsExpectedFields()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), "L3",
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(TenantId, PlanId, "L3", FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tenant/plan/gyartasilap_L3.pdf");
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L3");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GyartasilapId.Should().NotBeEmpty();
        result.Value.LabelVariant.Should().Be("L3");
        result.Value.Status.Should().Be(GyartasilapStatus.Draft);
        result.Value.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_StorageUrlInResponse_MatchesMinIOReturn()
    {
        // Arrange
        const string expectedUrl = "abc123/planXYZ/gyartasilap_L4.pdf";
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), "L4",
            null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePdfBytes);
        _storage.Setup(s => s.StoreAsync(TenantId, PlanId, "L4", FakePdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);
        _gyartasilapRepo.Setup(r => r.AddAsync(It.IsAny<Gyartasilap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L4");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.StorageUrl.Should().Be(expectedUrl);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Handle_WhenCancelled_ThrowsOperationCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var order = CreateValidOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(OrderId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _pdfBuilder.Setup(b => b.GeneratePdfAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        cts.Cancel();

        var command = new GenerateAndStoreGyartasilapCommand(TenantId, OrderId, PlanId, "L1");
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    #endregion
}
