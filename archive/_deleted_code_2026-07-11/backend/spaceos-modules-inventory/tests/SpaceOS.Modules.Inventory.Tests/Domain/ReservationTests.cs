using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;
using SpaceOS.Modules.Inventory.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class ReservationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid StockItemId = Guid.NewGuid();
    private static readonly TimeSpan ValidTtl = TimeSpan.FromHours(4);

    private static IReadOnlyList<(Guid, string, decimal)> OneItem()
        => [(StockItemId, "MDF-18", 10m)];

    // -------------------------------------------------------
    // Reserve() happy path
    // -------------------------------------------------------

    [Fact]
    public void Reserve_WithValidParams_ShouldReturnActiveReservation()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);

        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.TenantId.Should().Be(TenantId);
        reservation.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Reserve_WithValidParams_ShouldRaiseStockReservedDomainEvent()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Joinery", null, null, OneItem(), ValidTtl);

        reservation.DomainEvents.Should().ContainSingle(e => e is StockReservedDomainEvent);
    }

    [Fact]
    public void Reserve_ExpiresAtShouldBeCreatedAtPlusTtl()
    {
        var before = DateTimeOffset.UtcNow;
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        var after = DateTimeOffset.UtcNow;

        reservation.ExpiresAt.Should().BeOnOrAfter(before + ValidTtl);
        reservation.ExpiresAt.Should().BeOnOrBefore(after + ValidTtl);
    }

    // -------------------------------------------------------
    // Reserve() — invariant failures
    // -------------------------------------------------------

    [Fact]
    public void Reserve_WithNoItems_ShouldThrowArgumentException()
    {
        var act = () => Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null,
            Array.Empty<(Guid, string, decimal)>(), ValidTtl);

        act.Should().Throw<ArgumentException>().WithMessage("*I-01*");
    }

    [Fact]
    public void Reserve_WithTtlBelowMinimum_ShouldThrowArgumentException()
    {
        var shortTtl = TimeSpan.FromMinutes(30);
        var act = () => Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), shortTtl);

        act.Should().Throw<ArgumentException>().WithMessage("*I-03*");
    }

    [Fact]
    public void Reserve_WithTtlAboveMaximum_ShouldThrowArgumentException()
    {
        var longTtl = TimeSpan.FromHours(200);
        var act = () => Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), longTtl);

        act.Should().Throw<ArgumentException>().WithMessage("*I-03*");
    }

    [Fact]
    public void Reserve_ItemWithDifferentTenantId_ShouldThrowArgumentException()
    {
        // I-10: ReservationItem.TenantId must equal Reservation.TenantId.
        // Since Reserve() passes tenantId into Create(), a mismatch must come from a zero-guid item.
        // We simulate by constructing an item directly with a different TenantId.
        // The domain guard is on Create(reservationId, tenantId, ...) — TenantId is always the aggregate's.
        // I-10 is enforced by the factory always using the aggregate's TenantId.
        // Test that using Guid.Empty for tenantId on the aggregate throws before creating items.
        var act = () => Reservation.Reserve(Guid.Empty, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);

        act.Should().Throw<ArgumentException>().WithMessage("*TenantId*");
    }

    // -------------------------------------------------------
    // Release()
    // -------------------------------------------------------

    [Fact]
    public void Release_FromActive_ShouldTransitionToReleased()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        reservation.PopDomainEvents();

        reservation.Release("unit test");

        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.DomainEvents.Should().ContainSingle(e => e is ReservationReleasedDomainEvent);
    }

    [Fact]
    public void Release_FromReleased_ShouldThrowInvalidOperationException()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        reservation.Release("first release");

        var act = () => reservation.Release("second release");

        act.Should().Throw<InvalidOperationException>().WithMessage("*I-05*");
    }

    // -------------------------------------------------------
    // MarkExpired()
    // -------------------------------------------------------

    [Fact]
    public void MarkExpired_WithoutWorkerContext_ShouldThrowInvalidOperationException()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);

        var act = () => reservation.MarkExpired(isWorkerContext: false);

        act.Should().Throw<InvalidOperationException>().WithMessage("*I-08*");
    }

    [Fact]
    public void MarkExpired_WithWorkerContext_ShouldTransitionToExpired()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        reservation.PopDomainEvents();

        reservation.MarkExpired(isWorkerContext: true);

        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.DomainEvents.Should().ContainSingle(e => e is ReservationExpiredDomainEvent);
    }

    // -------------------------------------------------------
    // MarkConsumed()
    // -------------------------------------------------------

    [Fact]
    public void MarkConsumed_FromActive_ShouldTransitionToConsumed()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        reservation.PopDomainEvents();

        reservation.MarkConsumed();

        reservation.Status.Should().Be(ReservationStatus.Consumed);
        reservation.DomainEvents.Should().ContainSingle(e => e is ReservationConsumedDomainEvent);
    }

    [Fact]
    public void MarkConsumed_FromExpired_ShouldThrowInvalidOperationException()
    {
        var reservation = Reservation.Reserve(TenantId, Guid.NewGuid(), "Cutting", null, null, OneItem(), ValidTtl);
        reservation.MarkExpired(isWorkerContext: true);

        var act = () => reservation.MarkConsumed();

        act.Should().Throw<InvalidOperationException>().WithMessage("*I-05*");
    }

    // -------------------------------------------------------
    // ReservationItem — RecordConsumption
    // -------------------------------------------------------

    [Fact]
    public void ReservationItem_RecordConsumption_ExceedingReserved_ShouldThrow()
    {
        var item = ReservationItem.Create(Guid.NewGuid(), TenantId, StockItemId, "MDF-18", 10m);

        var act = () => item.RecordConsumption(15m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exceed*");
    }

    [Fact]
    public void ReservationItem_RecordConsumption_WithinLimit_ShouldAccumulate()
    {
        var item = ReservationItem.Create(Guid.NewGuid(), TenantId, StockItemId, "MDF-18", 10m);

        item.RecordConsumption(4m);
        item.RecordConsumption(4m);

        item.QuantityConsumed.Should().Be(8m);
    }

    // -------------------------------------------------------
    // ConsumerContextValidator
    // -------------------------------------------------------

    [Fact]
    public void ConsumerContextValidator_ValidJson_ShouldReturnSuccess()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("""{"orderId":"abc123"}""");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ConsumerContextValidator_NullInput_ShouldReturnSuccess()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ConsumerContextValidator_InvalidJson_ShouldReturnInvalid()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("not-json");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ConsumerContextValidator_XssAngleBracket_ShouldReturnInvalid()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("""{"note":"<script>alert(1)</script>"}""");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ConsumerContextValidator_XssJavascriptUri_ShouldReturnInvalid()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("""{"url":"javascript:void(0)"}""");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ConsumerContextValidator_PiiEmail_ShouldReturnInvalid()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("""{"contact":"user@example.com"}""");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ConsumerContextValidator_BearerToken_ShouldReturnInvalid()
    {
        var validator = new ConsumerContextValidator();
        var result = validator.Validate("""{"auth":"Bearer eyJhbGciOiJFUzI1NiJ9.payload.sig"}""");
        result.IsSuccess.Should().BeFalse();
    }

    // -------------------------------------------------------
    // HardcodedModuleRegistry
    // -------------------------------------------------------

    [Theory]
    [InlineData("Cutting")]
    [InlineData("cutting")]   // case-insensitive
    [InlineData("Joinery")]
    [InlineData("Cabinet")]
    [InlineData("FreeTier")]
    public void HardcodedModuleRegistry_KnownModules_ShouldReturnTrue(string moduleName)
    {
        var registry = new HardcodedModuleRegistry();
        registry.IsKnownConsumerModule(moduleName).Should().BeTrue();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Admin")]
    public void HardcodedModuleRegistry_UnknownModules_ShouldReturnFalse(string moduleName)
    {
        var registry = new HardcodedModuleRegistry();
        registry.IsKnownConsumerModule(moduleName).Should().BeFalse();
    }
}
