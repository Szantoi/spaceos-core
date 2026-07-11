using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;
using SpaceOS.Modules.Inventory.Infrastructure.Handlers;

namespace SpaceOS.Modules.Inventory.Api.Extensions;

public static class InventoryApplicationExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            // Application layer: commands, queries, and pipeline behaviors
            cfg.RegisterServicesFromAssembly(typeof(RecordConsumptionCommandHandler).Assembly);
            // Infrastructure layer: handlers that require direct DbContext access
            cfg.RegisterServicesFromAssembly(typeof(ReserveStockCommandHandler).Assembly);
        });
        return services;
    }
}
