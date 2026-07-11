using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using SpaceOS.Modules.Inventory.Domain.Interfaces;
using SpaceOS.Modules.Inventory.Domain.Services;
using SpaceOS.Modules.Inventory.Infrastructure.Adapters;
using SpaceOS.Modules.Inventory.Infrastructure.HealthChecks;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Repositories;
using SpaceOS.Modules.Inventory.Infrastructure.Services;

namespace SpaceOS.Modules.Inventory.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<TenantSessionInterceptor>();

        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_inventory"));
            options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
        });

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IHttpContextTenantAccessor, HttpContextTenantAccessor>();
        services.AddScoped<IInventoryProvider, InventoryProviderAdapter>();

        // Domain services used by reservation handlers
        services.AddSingleton<IModuleRegistry, HardcodedModuleRegistry>();
        services.AddSingleton<ConsumerContextValidator>();

        // Worker infrastructure
        services.AddSingleton<IWorkerHeartbeatStore, InMemoryWorkerHeartbeatStore>();
        services.AddHostedService<ReservationCleanupWorker>();
        services.AddHostedService<ReorderAlertWorker>();

        // HttpClient for ReorderAlertWorker outbound calls to Procurement
        services.AddHttpClient("inventory-reorder-alert-worker")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        services.AddHealthChecks()
            .AddCheck<InventoryHealthCheck>("inventory");

        return services;
    }
}
