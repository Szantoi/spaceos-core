using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class WorkStationIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyGuid()
    {
        var id = WorkStationId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => WorkStationId.From(Guid.Empty));
    }

    [Fact]
    public void Equals_WithSameGuid_ShouldBeTrue()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(WorkStationId.From(guid), WorkStationId.From(guid));
    }
}
