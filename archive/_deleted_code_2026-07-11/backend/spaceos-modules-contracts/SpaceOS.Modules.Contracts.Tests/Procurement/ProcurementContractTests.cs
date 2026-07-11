using System.Text.Json;
using Xunit;
using FluentAssertions;
using SpaceOS.Modules.Contracts.Procurement.DTOs;
using SpaceOS.Modules.Contracts.Procurement.Enums;

namespace SpaceOS.Modules.Contracts.Tests.Procurement;

public class ProcurementContractTests
{
    [Fact]
    public void PurchaseOrderDto_TenantId_IsRequired()
    {
        var tenantId = Guid.NewGuid();
        var dto = new PurchaseOrderDto(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            PurchaseOrderStatus.Draft,
            [],
            null,
            DateTimeOffset.UtcNow,
            null);

        dto.TenantId.Should().Be(tenantId);
        dto.TenantId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void PurchaseOrderStatus_Serialize_Roundtrip()
    {
        var original = PurchaseOrderStatus.Confirmed;

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PurchaseOrderStatus>(json);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void PurchaseOrderLineDto_Currency_CanBeNull()
    {
        var line = new PurchaseOrderLineDto(
            MaterialCode: "MDF18",
            Quantity: 10,
            UnitPrice: null,
            Currency: null);

        line.Currency.Should().BeNull();
        line.UnitPrice.Should().BeNull();
    }
}
