using Xunit;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Tests.ValueObjects
{
    public class FacilityNameTests
    {
        [Theory]
        [InlineData("Valid Facility Name")]
        [InlineData("A")]
        [InlineData(" Facility with padding ")]
        public void From_WithValidName_ShouldCreateFacilityName(string value)
        {
            var name = FacilityName.From(value);
            Assert.Equal(value.Trim(), name.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void From_WithInvalidName_ShouldThrowDomainException(string? value)
        {
            Assert.Throws<DomainException>(() => FacilityName.From(value!));
        }

        [Fact]
        public void From_WithNameTooLong_ShouldThrowDomainException()
        {
            var longName = new string('a', 101);
            Assert.Throws<DomainException>(() => FacilityName.From(longName));
        }
    }
}
