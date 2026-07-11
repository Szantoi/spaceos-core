using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void UpdateName_ShouldRaiseTenantRenamedEvent()
    {
        // Arrange
        var tenant = Tenant.Create("OldName");
        tenant.PopDomainEvents(); // clear creation event

        // Act
        tenant.UpdateName("NewName");

        // Assert
        var events = tenant.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<TenantRenamedEvent>(events[0]);
        var evt = (TenantRenamedEvent)events[0];
        Assert.Equal("OldName", evt.OldName);
        Assert.Equal("NewName", evt.NewName);
    }

    [Fact]
    public void Create_ShouldRaiseTenantCreatedEvent()
    {
        // Arrange & Act
        var tenant = Tenant.Create("Test Tenant");

        // Assert
        var events = tenant.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<TenantCreatedEvent>(events[0]);
        var evt = (TenantCreatedEvent)events[0];
        Assert.Equal(tenant.Id, evt.TenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultTenantTypeManufacturer()
    {
        // Arrange & Act
        var tenant = Tenant.Create("ACME Corp");

        // Assert
        Assert.Equal(TenantType.Manufacturer, tenant.TenantType);
    }

    [Fact]
    public void Register_WithDefaultTenantType_ShouldSetManufacturer()
    {
        // Arrange & Act
        var tenant = Tenant.Register("ACME Corp");

        // Assert
        Assert.Equal(TenantType.Manufacturer, tenant.TenantType);
    }

    [Fact]
    public void Register_WithExplicitTenantType_ShouldSetCorrectType()
    {
        // Arrange & Act
        var tenant = Tenant.Register("Lap-Expert Kft.", TenantType.PanelCutter);

        // Assert
        Assert.Equal(TenantType.PanelCutter, tenant.TenantType);
    }

    [Fact]
    public void Register_WithEnabledModules_ShouldSetModules()
    {
        // Arrange & Act
        var tenant = Tenant.Register("Ajtógyár Kft.", TenantType.Manufacturer, new[] { "door" });

        // Assert
        Assert.Contains("door", tenant.EnabledModules);
    }

    [Fact]
    public void Register_ShouldRaiseTenantCreatedEvent()
    {
        // Arrange & Act
        var tenant = Tenant.Register("New Co.", TenantType.Trader);

        // Assert
        var events = tenant.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<TenantCreatedEvent>(events[0]);
    }

    [Fact]
    public void UpdateEnabledModules_WithValidModules_ReturnsSuccess()
    {
        // Arrange
        var tenant = Tenant.Register("Ajtógyár Kft.", TenantType.Manufacturer);
        tenant.PopDomainEvents();
        var registry = new ModuleRegistryService();

        // Act
        var result = tenant.UpdateEnabledModules(new[] { "door" }, registry);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("door", tenant.EnabledModules);
    }

    [Fact]
    public void UpdateEnabledModules_WithValidModules_RaisesTenantModulesUpdatedEvent()
    {
        // Arrange
        var tenant = Tenant.Register("Ajtógyár Kft.", TenantType.Manufacturer);
        tenant.PopDomainEvents();
        var registry = new ModuleRegistryService();

        // Act
        tenant.UpdateEnabledModules(new[] { "door" }, registry);

        // Assert
        var events = tenant.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<TenantModulesUpdatedEvent>(events[0]);
    }

    [Fact]
    public void UpdateEnabledModules_WithInvalidModules_ReturnsFailure()
    {
        // Arrange — PanelCutter cannot have "orders"
        var tenant = Tenant.Register("Szabász Kft.", TenantType.PanelCutter);
        var registry = new ModuleRegistryService();

        // Act
        var result = tenant.UpdateEnabledModules(new[] { "orders" }, registry);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void UpdateEnabledModules_WithInvalidModules_DoesNotRaiseEvent()
    {
        // Arrange
        var tenant = Tenant.Register("Szabász Kft.", TenantType.PanelCutter);
        tenant.PopDomainEvents();
        var registry = new ModuleRegistryService();

        // Act
        tenant.UpdateEnabledModules(new[] { "orders" }, registry);

        // Assert — no event raised on failure
        var events = tenant.PopDomainEvents();
        Assert.Empty(events);
    }
}
