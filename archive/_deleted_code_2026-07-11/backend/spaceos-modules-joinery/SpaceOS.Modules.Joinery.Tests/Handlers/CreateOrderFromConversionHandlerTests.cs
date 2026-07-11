using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateOrderFromConversion;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class CreateOrderFromConversionHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<CreateOrderFromConversionCommandHandler>> _log = new();
    private readonly CreateOrderFromConversionCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid QuoteId = Guid.NewGuid();

    public CreateOrderFromConversionHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        _repo.Setup(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.FindBySourceQuoteIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        _sut = new CreateOrderFromConversionCommandHandler(
            _repo.Object, _clock.Object, _mediator.Object, _log.Object);
    }

    private static CreateOrderFromConversionCommand ValidCommand(
        Guid? tenantId = null,
        Guid? customerId = null,
        Guid? quoteId = null,
        string currency = "HUF",
        IReadOnlyList<ConversionLineItemDto>? lines = null,
        string contentHash = "abc123hash") => new(
        QuoteId: quoteId ?? QuoteId,
        TenantId: tenantId ?? TenantId,
        CustomerId: customerId ?? CustomerId,
        LinkedTenantId: null,
        Currency: currency,
        TotalNet: 10000m,
        TotalVat: 2700m,
        TotalGross: 12700m,
        Lines: lines ?? new[] { new ConversionLineItemDto(null, "Door", 1m, 100m, 0.27m, null, 0) },
        ContentHash: contentHash);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess_WithOrderId()
    {
        // Arrange
        var cmd = ValidCommand();

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryAddOnce()
    {
        // Arrange
        var cmd = ValidCommand();

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _repo.Verify(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IdempotentReplay_SameHash_Returns200WithSameOrderId()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = BuildExistingOrder(orderId, QuoteId, TenantId, "abc123hash", FixedNow);

        _repo.Setup(r => r.FindBySourceQuoteIdAsync(TenantId, QuoteId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existingOrder);

        var cmd = ValidCommand(contentHash: "abc123hash");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.CreatedAt.Should().Be(FixedNow);
        // AddAsync must NOT be called — idempotent return
        _repo.Verify(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ContentHashMismatch_ReturnsConflict()
    {
        // Arrange
        var existingOrder = BuildExistingOrder(Guid.NewGuid(), QuoteId, TenantId, "original-hash", FixedNow);

        _repo.Setup(r => r.FindBySourceQuoteIdAsync(TenantId, QuoteId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existingOrder);

        var cmd = ValidCommand(contentHash: "different-hash");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Conflict);
    }

    [Fact]
    public async Task Handle_InvalidLine_ReturnsInvalid()
    {
        // Arrange — VatRate > 1 is invalid
        var badLine = new ConversionLineItemDto(null, "Door", 1m, 100m, 1.5m, null, 0);
        var cmd = ValidCommand(lines: new[] { badLine });

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_EmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var cmd = ValidCommand(tenantId: Guid.Empty);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.Identifier == "TenantId");
    }

    [Fact]
    public async Task Handle_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        var cmd = ValidCommand(lines: Array.Empty<ConversionLineItemDto>());

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Lines");
    }

    [Fact]
    public async Task Handle_InvalidCurrency_ReturnsInvalid()
    {
        // Arrange — 2-char currency
        var cmd = ValidCommand(currency: "HU");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Currency");
    }

    [Fact]
    public async Task Handle_FirstCallPersists_SecondCallIsIdempotent()
    {
        // Arrange — first call stores, second call finds the stored order
        var createdOrderId = Guid.NewGuid();
        DoorOrder? storedOrder = null;

        _repo.Setup(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Callback<DoorOrder, CancellationToken>((o, _) => storedOrder = o)
             .Returns(Task.CompletedTask);

        _repo.Setup(r => r.FindBySourceQuoteIdAsync(TenantId, QuoteId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(() => storedOrder);

        var cmd = ValidCommand();

        // Act
        var first = await _sut.Handle(cmd, CancellationToken.None);
        var second = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.OrderId.Should().Be(first.Value.OrderId);
        _repo.Verify(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConcurrentDuplicate_UniqueConstraintViolation_ReturnsSameOrderId()
    {
        // Arrange — simulate race condition: AddAsync throws 23505, then FindBySourceQuoteId returns winner
        var winnerId = Guid.NewGuid();
        var winnerOrder = BuildExistingOrder(winnerId, QuoteId, TenantId, "abc123hash", FixedNow);

        // First call to FindBySourceQuoteIdAsync returns null (no existing order)
        // Second call (after the exception) returns the winner
        var findCallCount = 0;
        _repo.Setup(r => r.FindBySourceQuoteIdAsync(TenantId, QuoteId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(() =>
             {
                 findCallCount++;
                 return findCallCount == 1 ? null : winnerOrder;
             });

        var pgEx = CreateFakePostgresException("23505", "UX_DoorOrders_TenantId_SourceQuoteId");
        var dbUpdateEx = new DbUpdateException("Unique constraint violation", pgEx);

        _repo.Setup(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(dbUpdateEx);

        var cmd = ValidCommand(contentHash: "abc123hash");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(winnerId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DoorOrder BuildExistingOrder(
        Guid id, Guid quoteId, Guid tenantId, string contentHash, DateTimeOffset confirmedAt)
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(confirmedAt);

        var result = DoorOrder.CreateFromConversion(
            id, tenantId, CustomerId, null, quoteId,
            contentHash, "HUF", 10000m, 2700m, 12700m,
            new[] { new ConversionLineData(null, "Door", 1m, 100m, 0.27m, null, 0) },
            clock.Object);

        return result.Value;
    }

    private static Exception CreateFakePostgresException(string sqlState, string constraintName)
    {
        // Create a fake exception that exposes SqlState and ConstraintName via reflection
        // so IsUniqueConstraintViolationOnQuoteId can detect it.
        return new FakePostgresException(sqlState, constraintName);
    }

    private sealed class FakePostgresException(string sqlState, string constraintName) : Exception("fake pg")
    {
        public string SqlState { get; } = sqlState;
        public string ConstraintName { get; } = constraintName;
    }
}
