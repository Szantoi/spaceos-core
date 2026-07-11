using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class TenantIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyGuid()
    {
        var id = TenantId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => TenantId.From(Guid.Empty));
    }

    [Fact]
    public void Equals_WithSameGuid_ShouldBeTrue()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(TenantId.From(guid), TenantId.From(guid));
    }
}
