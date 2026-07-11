using FluentAssertions;
using Xunit;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Tests.Shared;

public class ProviderCapabilityTests
{
    [Fact]
    public void ProviderCapability_None_IsZero()
    {
        ((int)ProviderCapability.None).Should().Be(0);
    }

    [Fact]
    public void ProviderCapability_Flags_CanCompose()
    {
        var combined = ProviderCapability.CuttingSubmit | ProviderCapability.InventoryStock;

        ((int)combined).Should().Be(
            (int)ProviderCapability.CuttingSubmit + (int)ProviderCapability.InventoryStock);
    }

    [Fact]
    public void ProviderCapability_HasFlag_Works()
    {
        var combined = ProviderCapability.CuttingSubmit | ProviderCapability.CuttingNesting | ProviderCapability.InventoryTrend;

        combined.HasFlag(ProviderCapability.CuttingSubmit).Should().BeTrue();
        combined.HasFlag(ProviderCapability.InventoryTrend).Should().BeTrue();
        combined.HasFlag(ProviderCapability.ProcurementOrder).Should().BeFalse();
    }

    [Fact]
    public void ProviderCapability_Decompose_Works()
    {
        var combined = ProviderCapability.CuttingSubmit | ProviderCapability.CuttingNesting | ProviderCapability.ProcurementRating;

        var flags = Enum.GetValues<ProviderCapability>()
            .Where(f => f != ProviderCapability.None && combined.HasFlag(f))
            .ToList();

        flags.Should().Contain(ProviderCapability.CuttingSubmit);
        flags.Should().Contain(ProviderCapability.CuttingNesting);
        flags.Should().Contain(ProviderCapability.ProcurementRating);
        flags.Should().NotContain(ProviderCapability.InventoryStock);
    }

    [Fact]
    public void ProviderCapability_CuttingAnonymous_Is4096()
    {
        ((int)ProviderCapability.CuttingAnonymous).Should().Be(4096); // 1 << 12
    }

    [Fact]
    public void ProviderCapability_AllFlags_AreUnique()
    {
        var values = Enum.GetValues<ProviderCapability>()
            .Where(f => f != ProviderCapability.None)
            .Select(f => (int)f)
            .ToList();

        values.Should().OnlyHaveUniqueItems("each capability flag must occupy a distinct bit");
    }

    [Fact]
    public void ProviderCapability_CuttingAnonymous_DoesNotOverlapOtherFlags()
    {
        var others = Enum.GetValues<ProviderCapability>()
            .Where(f => f != ProviderCapability.None && f != ProviderCapability.CuttingAnonymous)
            .Aggregate(ProviderCapability.None, (acc, f) => acc | f);

        (others & ProviderCapability.CuttingAnonymous).Should().Be(ProviderCapability.None,
            "CuttingAnonymous bit must not overlap any existing capability");
    }
}
