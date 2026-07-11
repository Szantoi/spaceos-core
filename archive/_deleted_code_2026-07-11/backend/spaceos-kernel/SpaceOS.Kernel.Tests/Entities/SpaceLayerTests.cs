using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public class SpaceLayerTests
{
    private static readonly FacilityId AnyFacilityId = FacilityId.New();
    private const string ValidJson = """{"walls": [], "openings": []}""";
    private const string ValidUrl  = "https://node2.spaceos.io/api/layers/abc";

    // ── Local Layer ────────────────────────────────────────────────────────

    [Fact]
    public void CreateLocalLayer_WithValidJson_IsExternalNode_ShouldBeFalse()
    {
        var layer = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Joinery, TenantId.New());

        Assert.False(layer.IsExternalNode);
        Assert.Equal(ValidJson, layer.IntentDataJson);
        Assert.Null(layer.ExternalSourceUrl);
    }

    [Fact]
    public void CreateLocalLayer_ShouldSetNonEmptyLastStateHash()
    {
        var layer = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Architecture, TenantId.New());

        Assert.False(string.IsNullOrWhiteSpace(layer.LastStateHash));
    }

    [Fact]
    public void CreateLocalLayer_SamJson_ShouldProduceSameHash()
    {
        var layer1 = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Joinery, TenantId.New());
        var layer2 = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Joinery, TenantId.New());

        Assert.Equal(layer1.LastStateHash, layer2.LastStateHash);
    }

    [Fact]
    public void CreateLocalLayer_WithEmptyJson_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => SpaceLayer.CreateLocalLayer("", AnyFacilityId, TradeType.Plumbing, TenantId.New()));

        Assert.Contains("IntentDataJson", ex.Message);
    }

    // ── External Layer ─────────────────────────────────────────────────────

    [Fact]
    public void CreateExternalLayer_WithValidUrl_IsExternalNode_ShouldBeTrue()
    {
        var layer = SpaceLayer.CreateExternalLayer(ValidUrl, AnyFacilityId, TradeType.Electrical, TenantId.New());

        Assert.True(layer.IsExternalNode);
        Assert.Equal(ValidUrl, layer.ExternalSourceUrl);
        Assert.Null(layer.IntentDataJson);
    }

    [Fact]
    public void CreateExternalLayer_WithNullUrl_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => SpaceLayer.CreateExternalLayer(null!, AnyFacilityId, TradeType.Joinery, TenantId.New()));

        Assert.Contains("ExternalSourceUrl", ex.Message);
    }

    [Fact]
    public void CreateExternalLayer_WithEmptyUrl_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => SpaceLayer.CreateExternalLayer("   ", AnyFacilityId, TradeType.Mep, TenantId.New()));

        Assert.Contains("ExternalSourceUrl", ex.Message);
    }

    // ── Identity ───────────────────────────────────────────────────────────

    [Fact]
    public void TwoLocalLayers_ShouldHaveDifferentIds()
    {
        var a = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Joinery, TenantId.New());
        var b = SpaceLayer.CreateLocalLayer(ValidJson, AnyFacilityId, TradeType.Joinery, TenantId.New());

        Assert.NotEqual(a.Id, b.Id);
    }

    // ── Domain Events ──────────────────────────────────────────────────────

    [Fact]
    public void CreateLocalLayer_ShouldRaiseSpaceLayerRegisteredEvent()
    {
        // Arrange & Act
        var facilityId = FacilityId.New();
        var layer = SpaceLayer.CreateLocalLayer(ValidJson, facilityId, TradeType.Electrical, TenantId.New());

        // Assert
        var events = layer.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<SpaceLayerRegisteredEvent>(events[0]);
        var evt = (SpaceLayerRegisteredEvent)events[0];
        Assert.Equal(layer.Id, evt.SpaceLayerId);
        Assert.Equal(facilityId, evt.FacilityId);
        Assert.False(evt.IsExternalNode);
    }

    [Fact]
    public void CreateExternalLayer_ShouldRaiseSpaceLayerRegisteredEvent()
    {
        // Arrange & Act
        var facilityId = FacilityId.New();
        var layer = SpaceLayer.CreateExternalLayer(ValidUrl, facilityId, TradeType.Mep, TenantId.New());

        // Assert
        var events = layer.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<SpaceLayerRegisteredEvent>(events[0]);
        var evt = (SpaceLayerRegisteredEvent)events[0];
        Assert.Equal(layer.Id, evt.SpaceLayerId);
        Assert.True(evt.IsExternalNode);
    }
}
