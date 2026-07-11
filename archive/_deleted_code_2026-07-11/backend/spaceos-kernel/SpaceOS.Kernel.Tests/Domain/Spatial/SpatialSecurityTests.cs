// SpaceOS.Kernel.Tests/Domain/Spatial/SpatialSecurityTests.cs

using SpaceOS.Kernel.Application.Spaces.Queries;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain.Spatial;

/// <summary>
/// Compile-time / reflection security gates that ensure dangerous properties
/// and enum values are absent from the domain model (SEC-P3A findings).
/// </summary>
public sealed class SpatialSecurityTests
{
    [Fact]
    public void SpatialContractDto_HasNoElementTypeProperty()
    {
        // ADR-008: ElementType is driver-specific and must never cross the server boundary
        var prop = typeof(SpatialContractDto).GetProperty("ElementType");

        Assert.Null(prop);
    }

    [Fact]
    public void TradeType_HasNoOtherValue()
    {
        // SEC-P3A: 'Other' enum value was removed to prevent unconstrained trade types
        var names = Enum.GetNames<TradeType>();

        Assert.DoesNotContain(names, n => n.Equals("Other", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorkPhase_HasNoOtherValue()
    {
        // SEC-P3A: 'Other' enum value was removed to prevent unconstrained work phases
        var names = Enum.GetNames<WorkPhase>();

        Assert.DoesNotContain(names, n => n.Equals("Other", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BvhNode_HasNoChildrenNavigationProperty()
    {
        // BE-P3A-03: Children are loaded via IBvhRepository.GetChildrenAsync(), not navigation
        var prop = typeof(BvhNode).GetProperty("Children");

        Assert.Null(prop);
    }

    [Fact]
    public void PhysicalSpace_HasNoNodesNavigationProperty()
    {
        // BE-P3A-01: _nodes eager load removed — tree traversal is via IBvhRepository
        var properties = typeof(PhysicalSpace).GetProperties();

        Assert.DoesNotContain(properties, p => p.Name == "Nodes" || p.Name == "BvhNodes");
    }
}
