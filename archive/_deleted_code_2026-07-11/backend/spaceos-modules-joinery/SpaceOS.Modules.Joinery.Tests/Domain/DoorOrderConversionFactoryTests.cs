using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

public class DoorOrderConversionFactoryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    private static IClock MakeClock()
    {
        var mock = new Mock<IClock>();
        mock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        return mock.Object;
    }

    private static ConversionLineData ValidLine() => new(
        SourceTemplateId: null,
        Description: "Standard door",
        Quantity: 2m,
        UnitPriceNet: 100m,
        VatRate: 0.27m,
        DiscountPercent: null,
        SortOrder: 0);

    private static (Guid id, Guid tenantId, Guid customerId, Guid quoteId) ValidIds() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void CreateFromConversion_ValidArgs_ReturnsSuccess_WithConfirmedFromSalesStatus()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();
        var clock = MakeClock();

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            "abc123hash", "HUF", 10000m, 2700m, 12700m,
            new[] { ValidLine() }, clock);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DoorOrderStatus.ConfirmedFromSales);
        result.Value.ConfirmedFromSalesAt.Should().Be(FixedNow);
        result.Value.Id.Should().Be(id);
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.SourceQuoteId.Should().Be(quoteId);
        result.Value.ConvertedLines.Should().HaveCount(1);
    }

    [Fact]
    public void CreateFromConversion_ValidArgs_RaisesCreatedFromConversionEvent()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            "hash", "EUR", 500m, 100m, 600m,
            new[] { ValidLine() }, MakeClock());

        // Assert
        var events = result.Value.PopDomainEvents();
        events.Should().ContainSingle(e => e is DoorOrderCreatedFromConversion);
        var evt = (DoorOrderCreatedFromConversion)events[0];
        evt.OrderId.Should().Be(id);
        evt.TenantId.Should().Be(tenantId);
        evt.CustomerId.Should().Be(customerId);
        evt.SourceQuoteId.Should().Be(quoteId);
    }

    [Fact]
    public void CreateFromConversion_EmptySourceQuoteId_ReturnsInvalid()
    {
        // Arrange
        var (id, tenantId, customerId, _) = ValidIds();

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, Guid.Empty,
            "hash", "HUF", 100m, 27m, 127m,
            new[] { ValidLine() }, MakeClock());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "SourceQuoteId");
    }

    [Fact]
    public void CreateFromConversion_EmptyContentHash_ReturnsInvalid()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            "", "HUF", 100m, 27m, 127m,
            new[] { ValidLine() }, MakeClock());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "ContentHash");
    }

    [Fact]
    public void CreateFromConversion_ContentHashTooLong_ReturnsInvalid()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();
        var tooLong = new string('x', 257);

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            tooLong, "HUF", 100m, 27m, 127m,
            new[] { ValidLine() }, MakeClock());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "ContentHash");
    }

    [Fact]
    public void CreateFromConversion_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();

        // Act
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            "hash", "HUF", 100m, 27m, 127m,
            Array.Empty<ConversionLineData>(), MakeClock());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Lines");
    }

    [Fact]
    public void CreateFromConversion_InvalidCurrency_ReturnsInvalid()
    {
        // Arrange
        var (id, tenantId, customerId, quoteId) = ValidIds();

        // Act — 2-char currency is invalid (must be 3)
        var result = DoorOrder.CreateFromConversion(
            id, tenantId, customerId, null, quoteId,
            "hash", "HU", 100m, 27m, 127m,
            new[] { ValidLine() }, MakeClock());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Currency");
    }

    [Fact]
    public void CreateFromConversion_DoesNotAffectExistingCreateFactory()
    {
        // Arrange & Act — the original Create factory still works normally
        var result = DoorOrder.Create(Guid.NewGuid(), "PRJ-001", "Test", Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DoorOrderStatus.Draft);
        result.Value.ConvertedLines.Should().BeEmpty();
        result.Value.SourceQuoteId.Should().BeNull();
    }
}
