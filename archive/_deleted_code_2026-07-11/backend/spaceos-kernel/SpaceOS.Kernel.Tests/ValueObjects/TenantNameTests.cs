using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class TenantNameTests
{
    [Theory]
    [InlineData("ACME Corp")]
    [InlineData("  trimmed  ")]
    public void From_WithValidName_ShouldCreateTenantName(string value)
    {
        var name = TenantName.From(value);
        Assert.Equal(value.Trim(), name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhiteSpace_ShouldThrowDomainException(string value)
    {
        Assert.Throws<DomainException>(() => TenantName.From(value));
    }

    [Fact]
    public void From_WithNameTooLong_ShouldThrowDomainException()
    {
        var longName = new string('x', 101);
        Assert.Throws<DomainException>(() => TenantName.From(longName));
    }

    [Fact]
    public void From_WithNameExactly100CharsAfterTrim_ShouldSucceed()
    {
        // 100 érdemi karakter + szóközök → trim után pontosan 100 kar.
        var name = "  " + new string('A', 100) + "  ";
        var result = TenantName.From(name);
        Assert.Equal(100, result.Value.Length);
    }
}
