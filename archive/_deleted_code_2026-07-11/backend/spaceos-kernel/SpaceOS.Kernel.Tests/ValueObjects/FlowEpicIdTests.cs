using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class FlowEpicIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyGuid()
    {
        var id = FlowEpicId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => FlowEpicId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnUnderlyingGuid()
    {
        var guid = Guid.NewGuid();
        Guid result = FlowEpicId.From(guid);
        Assert.Equal(guid, result);
    }
}
