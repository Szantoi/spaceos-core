// SpaceOS.Kernel.Tests/Domain/ModuleRegistryServiceTests.cs
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>Unit tests for <see cref="ModuleRegistryService"/>.</summary>
public sealed class ModuleRegistryServiceTests
{
    private readonly ModuleRegistryService _registry = new();

    // ---- Manufacturer ----

    [Fact]
    public void ValidateModules_Manufacturer_WithDoor_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Manufacturer, new[] { "door" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateModules_Manufacturer_WithOrders_ReturnsFailure()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Manufacturer, new[] { "orders" });
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ValidateModules_Manufacturer_EmptyModules_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Manufacturer, Array.Empty<string>());
        Assert.True(result.IsValid);
    }

    // ---- PanelCutter ----

    [Fact]
    public void ValidateModules_PanelCutter_WithCutting_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.PanelCutter, new[] { "cutting" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateModules_PanelCutter_WithDoor_ReturnsFailure()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.PanelCutter, new[] { "door" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateModules_PanelCutter_MissingRequiredCutting_ReturnsFailure()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.PanelCutter, Array.Empty<string>());
        Assert.False(result.IsValid);
    }

    // ---- Trader ----

    [Fact]
    public void ValidateModules_Trader_WithTrading_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Trader, new[] { "trading" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateModules_Trader_WithCutting_ReturnsFailure()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Trader, new[] { "cutting" });
        Assert.False(result.IsValid);
    }

    // ---- Logistics ----

    [Fact]
    public void ValidateModules_Logistics_WithDelivery_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Logistics, new[] { "delivery" });
        Assert.True(result.IsValid);
    }

    // ---- Installer ----

    [Fact]
    public void ValidateModules_Installer_WithInstallation_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.Installer, new[] { "installation" });
        Assert.True(result.IsValid);
    }

    // ---- EndCustomer ----

    [Fact]
    public void ValidateModules_EndCustomer_WithOrders_ReturnsSuccess()
    {
        var result = _registry.ValidateModulesForTenantType(TenantType.EndCustomer, new[] { "orders" });
        Assert.True(result.IsValid);
    }

    // ---- GetAllowedModules ----

    [Fact]
    public void GetAllowedModules_Manufacturer_ReturnsFiveModules()
    {
        var allowed = _registry.GetAllowedModules(TenantType.Manufacturer);
        Assert.Equal(5, allowed.Count);
    }

    // ---- GetRequiredModules ----

    [Fact]
    public void GetRequiredModules_PanelCutter_ReturnsCutting()
    {
        var required = _registry.GetRequiredModules(TenantType.PanelCutter);
        Assert.Single(required);
        Assert.Equal("cutting", required[0]);
    }
}
