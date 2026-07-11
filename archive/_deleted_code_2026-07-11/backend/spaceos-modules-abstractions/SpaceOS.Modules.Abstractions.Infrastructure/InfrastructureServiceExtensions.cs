using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Abstractions.Application;
using SpaceOS.Modules.Abstractions.Application.Seeding;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Infrastructure.Persistence;
using SpaceOS.Modules.Abstractions.Infrastructure.Seeding;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;

namespace SpaceOS.Modules.Abstractions.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddScoped<TenantCommandInterceptor>();
        services.AddDbContext<AbstractionsDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(
                    typeof(InfrastructureServiceExtensions).Assembly.FullName));
            opts.AddInterceptors(sp.GetRequiredService<TenantCommandInterceptor>());
        });

        services.AddScoped<IAbstractionsRepository, AbstractionsRepository>();
        services.AddScoped<IProductCalculationEngine, GraphCalculationEngine>();
        services.AddScoped<ITemplateValidator, TemplateValidatorService>();
        services.AddScoped<IManufacturingDerivation, ManufacturingDerivationService>();
        services.AddScoped<FafTTemplateSeeder>();
        services.AddScoped<FafUTemplateSeeder>();
        services.AddScoped<BfajTemplateSeeder>();
        services.AddScoped<ITemplateSeeder, CompositeTemplateSeeder>();
        services.AddHttpContextAccessor();
        services.AddScoped<IHttpContextTenantAccessor, HttpContextTenantAccessor>();
        return services;
    }
}
