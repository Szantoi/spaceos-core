using Xunit;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Tests.ValueObjects
{
    public class FacilityIdTests
    {
        [Fact]
        public void New_ShouldCreateNonEmptyGuid()
        {
            var id = FacilityId.New();
            Assert.NotEqual(Guid.Empty, id.Value);
        }

        [Fact]
        public void From_WithEmptyGuid_ShouldThrowDomainException()
        {
            Assert.Throws<DomainException>(() => FacilityId.From(Guid.Empty));
        }

        [Fact]
        public void Equals_WithSameGuid_ShouldBeTrue()
        {
            var guid = Guid.NewGuid();
            var id1 = FacilityId.From(guid);
            var id2 = FacilityId.From(guid);
            Assert.Equal(id1, id2);
        }
    }
}
