using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities
{
    public class FacilityTests
    {
        [Fact]
        public void Create_WithValidName_ShouldInitializeProperties()
        {
            // Arrange
            var name = "Main Workspace";
            var tenantId = TenantId.New();

            // Act
            var facility = Facility.Create(name, tenantId);

            // Assert
            Assert.Equal(name, facility.Name.Value);
            Assert.NotEqual(Guid.Empty, facility.Id.Value);
            Assert.Equal(tenantId, facility.TenantId);
        }

        [Fact]
        public void Create_ShouldRaiseFacilityCreatedEvent()
        {
            // Arrange & Act
            var tenantId = TenantId.New();
            var facility = Facility.Create("HQ", tenantId);

            // Assert
            var events = facility.PopDomainEvents();
            Assert.Single(events);
            Assert.IsType<FacilityCreatedEvent>(events[0]);
            var evt = (FacilityCreatedEvent)events[0];
            Assert.Equal(facility.Id, evt.FacilityId);
            Assert.Equal(tenantId, evt.TenantId);
        }

        [Fact]
        public void Rename_WithValidName_ShouldUpdateName()
        {
            // Arrange
            var tenantId = TenantId.New();
            var facility = Facility.Create("Original Name", tenantId);
            facility.PopDomainEvents(); // clear creation event
            var newName = "New Name";

            // Act
            var renamedFacility = facility.Rename(newName);

            // Assert
            Assert.Equal(newName, renamedFacility.Name.Value);
            Assert.Equal(facility.Id, renamedFacility.Id);
            Assert.Equal(tenantId, renamedFacility.TenantId);
        }

        [Fact]
        public void Rename_ShouldRaiseFacilityRenamedEvent()
        {
            // Arrange
            var facility = Facility.Create("OldName", TenantId.New());
            facility.PopDomainEvents(); // clear creation event

            // Act
            facility.Rename("NewName");

            // Assert
            var events = facility.PopDomainEvents();
            Assert.Single(events);
            Assert.IsType<FacilityRenamedEvent>(events[0]);
            var evt = (FacilityRenamedEvent)events[0];
            Assert.Equal("OldName", evt.OldName);
            Assert.Equal("NewName", evt.NewName);
        }
    }
}
