using System.Text.Json;
using FluentAssertions;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Inventory.Enums;
using SpaceOS.Modules.Contracts.Inventory.Events;
using SpaceOS.Modules.Contracts.Inventory.Requests;
using SpaceOS.Modules.Contracts.Inventory.Validation;
using SpaceOS.Modules.Contracts.Shared;
using Xunit;

namespace SpaceOS.Modules.Contracts.Tests.Inventory;

public class ReservationContractTests
{
    private static readonly Guid TenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // ── ReservationStatus enum ──────────────────────────────────────────────

    [Fact]
    public void ReservationStatus_HasExpectedValues()
    {
        ((int)ReservationStatus.Active).Should().Be(0);
        ((int)ReservationStatus.Released).Should().Be(1);
        ((int)ReservationStatus.Expired).Should().Be(2);
        ((int)ReservationStatus.Consumed).Should().Be(3);
    }

    [Fact]
    public void ReservationStatus_HasExactlyFourValues()
    {
        Enum.GetValues<ReservationStatus>().Should().HaveCount(4);
    }

    // ── ReserveStockRequest / ReserveItemRequest DTOs ──────────────────────

    [Fact]
    public void ReserveStockRequest_CanBeConstructed()
    {
        var correlationId = Guid.NewGuid();
        var item = new ReserveItemRequest(Guid.NewGuid(), "MDF18", 3.5m);
        var req = new ReserveStockRequest(
            correlationId,
            "CuttingPlanning",
            null,
            new[] { item },
            TimeSpan.FromHours(24));

        req.CorrelationId.Should().Be(correlationId);
        req.ConsumerModule.Should().Be("CuttingPlanning");
        req.ConsumerContextJson.Should().BeNull();
        req.Items.Should().HaveCount(1);
        req.Ttl.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void ReserveItemRequest_QuantityReserved_IsDecimal()
    {
        var item = new ReserveItemRequest(Guid.NewGuid(), "OAK22", 1.25m);
        item.QuantityReserved.Should().Be(1.25m);
        item.MaterialCode.Should().Be("OAK22");
    }

    // ── ReservationDto / ReservationItemDto ────────────────────────────────

    [Fact]
    public void ReservationDto_ContainsTenantId()
    {
        var dto = new ReservationDto(
            Id: Guid.NewGuid(),
            TenantId: TenantId,
            CorrelationId: Guid.NewGuid(),
            ConsumerModule: "CuttingPlanning",
            ConsumerContextJson: null,
            CreatedByUserId: null,
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24),
            Status: ReservationStatus.Active,
            Items: Array.Empty<ReservationItemDto>());

        dto.TenantId.Should().Be(TenantId);
        dto.Status.Should().Be(ReservationStatus.Active);
    }

    [Fact]
    public void ReservationItemDto_QuantityConsumed_CanBeZero()
    {
        var item = new ReservationItemDto(Guid.NewGuid(), Guid.NewGuid(), "MDF18", 5m, 0m);
        item.QuantityConsumed.Should().Be(0m);
        item.QuantityReserved.Should().Be(5m);
    }

    // ── ReservationFilter ──────────────────────────────────────────────────

    [Fact]
    public void ReservationFilter_Defaults_AreCorrect()
    {
        var filter = new ReservationFilter(
            ConsumerModule: "CuttingPlanning",
            Status: null,
            CorrelationId: null,
            CreatedAfter: null,
            CreatedBefore: null);

        filter.Skip.Should().Be(0);
        filter.Take.Should().Be(100);
    }

    [Fact]
    public void ReservationFilter_CanSetAllFields()
    {
        var corrId = Guid.NewGuid();
        var filter = new ReservationFilter(
            ConsumerModule: "Joinery",
            Status: ReservationStatus.Expired,
            CorrelationId: corrId,
            CreatedAfter: DateTimeOffset.UtcNow.AddDays(-7),
            CreatedBefore: DateTimeOffset.UtcNow,
            Skip: 10,
            Take: 50);

        filter.CorrelationId.Should().Be(corrId);
        filter.Status.Should().Be(ReservationStatus.Expired);
        filter.Take.Should().Be(50);
    }

    // ── Events — SEC-03 pattern ────────────────────────────────────────────

    [Fact]
    public void StockReserved_InheritsModuleEvent_WithAutoEventId()
    {
        var ev = new StockReserved
        {
            TenantId = TenantId,
            OccurredAt = DateTimeOffset.UtcNow,
            ActorUserId = null,
            CorrelationId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            ConsumerModule = "CuttingPlanning",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Items = Array.Empty<ReservationItemDto>(),
        };

        ev.Should().BeAssignableTo<ModuleEvent>();
        ev.EventId.Should().NotBe(Guid.Empty);
        ev.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void ReservationReleased_HasRequiredProperties()
    {
        var reservationId = Guid.NewGuid();
        var ev = new ReservationReleased
        {
            TenantId = TenantId,
            OccurredAt = DateTimeOffset.UtcNow,
            ReservationId = reservationId,
            CorrelationId = Guid.NewGuid(),
            Reason = "Planning cancelled",
        };

        ev.ReservationId.Should().Be(reservationId);
        ev.Reason.Should().Be("Planning cancelled");
    }

    [Fact]
    public void ReservationExpired_HasExpiredAtTimestamp()
    {
        var expiredAt = DateTimeOffset.UtcNow.AddHours(-1);
        var ev = new ReservationExpired
        {
            TenantId = TenantId,
            OccurredAt = DateTimeOffset.UtcNow,
            ReservationId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExpiredAt = expiredAt,
        };

        ev.ExpiredAt.Should().Be(expiredAt);
    }

    [Fact]
    public void ReservationConsumed_HasConsumerModule()
    {
        var ev = new ReservationConsumed
        {
            TenantId = TenantId,
            OccurredAt = DateTimeOffset.UtcNow,
            ReservationId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ConsumerModule = "CuttingPlanning",
        };

        ev.ConsumerModule.Should().Be("CuttingPlanning");
        ev.Should().BeAssignableTo<ModuleEvent>();
    }

    [Fact]
    public void AllFourReservationEvents_AutoGenerateUniqueEventIds()
    {
        var ev1 = new StockReserved { TenantId = TenantId, OccurredAt = DateTimeOffset.UtcNow, ActorUserId = null, CorrelationId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), ConsumerModule = "X", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), Items = Array.Empty<ReservationItemDto>() };
        var ev2 = new ReservationReleased { TenantId = TenantId, OccurredAt = DateTimeOffset.UtcNow, ReservationId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Reason = null };
        var ev3 = new ReservationExpired { TenantId = TenantId, OccurredAt = DateTimeOffset.UtcNow, ReservationId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), ExpiredAt = DateTimeOffset.UtcNow };
        var ev4 = new ReservationConsumed { TenantId = TenantId, OccurredAt = DateTimeOffset.UtcNow, ReservationId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), ConsumerModule = "X" };

        new[] { ev1.EventId, ev2.EventId, ev3.EventId, ev4.EventId }
            .Distinct().Should().HaveCount(4, "each event generates its own unique EventId");
    }

    // ── ConsumerContextJsonSchema (SEC-07, SEC-09) ─────────────────────────

    [Fact]
    public void ConsumerContextJsonSchema_AcceptsValidJson()
    {
        var json = """{"jobId":"abc123","priority":1}""";
        ConsumerContextJsonSchema.IsValid(json, out var violation).Should().BeTrue();
        violation.Should().BeNull();
    }

    [Fact]
    public void ConsumerContextJsonSchema_RejectsXssPayload()
    {
        var json = """{"note":"<script>alert(1)</script>"}""";
        ConsumerContextJsonSchema.IsValid(json, out var violation).Should().BeFalse();
        violation.Should().Be("XssPattern");
    }

    [Fact]
    public void ConsumerContextJsonSchema_RejectsEmailAsPii()
    {
        var json = """{"createdBy":"user@example.com"}""";
        ConsumerContextJsonSchema.IsValid(json, out var violation).Should().BeFalse();
        violation.Should().Be("PiiPattern");
    }

    [Fact]
    public void ConsumerContextJsonSchema_RejectsBearerTokenAsPii()
    {
        var json = """{"auth":"Bearer eyJhbGciOiJSUzI1NiJ9.abc"}""";
        ConsumerContextJsonSchema.IsValid(json, out var violation).Should().BeFalse();
        violation.Should().Be("PiiPattern");
    }

    [Fact]
    public void ConsumerContextJsonSchema_AcceptsNullOrEmpty()
    {
        ConsumerContextJsonSchema.IsValid(null, out _).Should().BeTrue();
        ConsumerContextJsonSchema.IsValid(string.Empty, out _).Should().BeTrue();
    }

    [Fact]
    public void ConsumerContextJsonSchema_RejectsOversizePayload()
    {
        var bigJson = "{\"data\":\"" + new string('x', 4001) + "\"}";
        ConsumerContextJsonSchema.IsValid(bigJson, out var violation).Should().BeFalse();
        violation.Should().Be("MaxSizeBytes");
    }

    // ── ProviderCapability — InventoryReservation flag ─────────────────────

    [Fact]
    public void ProviderCapability_InventoryReservation_IsBit11()
    {
        ((int)ProviderCapability.InventoryReservation).Should().Be(1 << 11);
    }

    [Fact]
    public void ProviderCapability_InventoryReservation_CanBeCombined()
    {
        var caps = ProviderCapability.InventoryStock | ProviderCapability.InventoryReservation;
        caps.HasFlag(ProviderCapability.InventoryReservation).Should().BeTrue();
        caps.HasFlag(ProviderCapability.InventoryStock).Should().BeTrue();
        caps.HasFlag(ProviderCapability.ProcurementOrder).Should().BeFalse();
    }
}
