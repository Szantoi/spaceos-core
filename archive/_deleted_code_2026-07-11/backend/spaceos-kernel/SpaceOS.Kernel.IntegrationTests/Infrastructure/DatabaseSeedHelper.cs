// SpaceOS.Kernel.IntegrationTests/Infrastructure/DatabaseSeedHelper.cs
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Provides typed seed methods for populating the integration test database
/// with domain entities before each test.
/// </summary>
public static class DatabaseSeedHelper
{
    /// <summary>Seeds a <see cref="Tenant"/> with the given name and persists it.</summary>
    /// <param name="services">The test host's service provider.</param>
    /// <param name="name">Display name for the tenant. Defaults to "Test Tenant".</param>
    /// <returns>The persisted <see cref="Tenant"/>.</returns>
    public static async Task<Tenant> SeedTenantAsync(
        IServiceProvider services, string name = "Test Tenant")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = Tenant.Create(name);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return tenant;
    }

    /// <summary>Seeds a <see cref="Facility"/> under the given tenant and persists it.</summary>
    /// <param name="services">The test host's service provider.</param>
    /// <param name="tenantId">The owning <see cref="TenantId"/>.</param>
    /// <param name="name">Display name for the facility. Defaults to "Test Facility".</param>
    /// <returns>The persisted <see cref="Facility"/>.</returns>
    public static async Task<Facility> SeedFacilityAsync(
        IServiceProvider services, TenantId tenantId, string name = "Test Facility")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facility = Facility.Create(name, tenantId);
        db.Facilities.Add(facility);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return facility;
    }

    /// <summary>Seeds a <see cref="WorkStation"/> and persists it.</summary>
    /// <param name="services">The test host's service provider.</param>
    /// <param name="facilityId">The owning <see cref="FacilityId"/>.</param>
    /// <param name="tenantId">The owning <see cref="TenantId"/>.</param>
    /// <param name="name">Display name for the work station. Defaults to "Test WorkStation".</param>
    /// <param name="type">Work station type string. Defaults to "Desk".</param>
    /// <returns>The persisted <see cref="WorkStation"/>.</returns>
    public static async Task<WorkStation> SeedWorkStationAsync(
        IServiceProvider services, FacilityId facilityId, TenantId tenantId,
        string name = "Test WorkStation", string type = "Desk")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workStation = WorkStation.Create(name, type, facilityId, tenantId);
        db.WorkStations.Add(workStation);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return workStation;
    }

    /// <summary>Seeds a local <see cref="SpaceLayer"/> and persists it.</summary>
    /// <param name="services">The test host's service provider.</param>
    /// <param name="facilityId">The owning <see cref="FacilityId"/>.</param>
    /// <param name="tenantId">The owning <see cref="TenantId"/>.</param>
    /// <param name="intentDataJson">Intent data JSON. Defaults to minimal valid JSON.</param>
    /// <param name="tradeType">Trade type. Defaults to <see cref="TradeType.Architecture"/>.</param>
    /// <returns>The persisted <see cref="SpaceLayer"/>.</returns>
    public static async Task<SpaceLayer> SeedSpaceLayerAsync(
        IServiceProvider services, FacilityId facilityId, TenantId tenantId,
        string intentDataJson = "{}", TradeType tradeType = TradeType.Architecture)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var layer = SpaceLayer.CreateLocalLayer(intentDataJson, facilityId, tradeType, tenantId);
        db.SpaceLayers.Add(layer);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return layer;
    }

    /// <summary>Seeds a <see cref="FlowEpic"/> and persists it.</summary>
    /// <param name="services">The test host's service provider.</param>
    /// <param name="facilityId">The owning <see cref="FacilityId"/>.</param>
    /// <param name="tenantId">The owning <see cref="TenantId"/>.</param>
    /// <param name="title">Title for the flow epic. Defaults to "Test FlowEpic".</param>
    /// <returns>The persisted <see cref="FlowEpic"/>.</returns>
    public static async Task<FlowEpic> SeedFlowEpicAsync(
        IServiceProvider services, FacilityId facilityId, TenantId tenantId,
        string title = "Test FlowEpic")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var epic = FlowEpic.Create(title, facilityId, tenantId);
        db.FlowEpics.Add(epic);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return epic;
    }
}
