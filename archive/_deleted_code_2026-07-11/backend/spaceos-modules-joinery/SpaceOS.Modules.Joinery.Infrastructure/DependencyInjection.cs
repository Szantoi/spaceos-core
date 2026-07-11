using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Application.Products.Services;
using SpaceOS.Modules.Joinery.Application.Seeding;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Cutting;
using SpaceOS.Modules.Joinery.Infrastructure.Documents;
using SpaceOS.Modules.Joinery.Infrastructure.Http;
using SpaceOS.Modules.Joinery.Infrastructure.Outbox;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Joinery.Infrastructure.Seeding;
using SpaceOS.Modules.Joinery.Infrastructure.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Storage;

namespace SpaceOS.Modules.Joinery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration? configuration = null)
    {
        services.AddSingleton<TenantSessionInterceptor>();

        services.AddDbContext<JoineryDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(JoineryDbContext).Assembly.FullName));
            opts.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
        });

        services.AddSingleton<IClock, SystemClock>();

        // PDF options and generator
        if (configuration is not null)
            services.Configure<PdfOptions>(configuration.GetSection("Pdf"));
        else
            services.Configure<PdfOptions>(_ => { });

        services.AddSingleton<IProductionSheetGenerator, ProductionSheetGenerator>();

        services.AddScoped<IGyartasilapPdfBuilder, GyartasilapPdfBuilder>();

        // Gyártásilap MinIO storage
        if (configuration is not null)
            services.Configure<GyartasilapStorageOptions>(
                configuration.GetSection("GyartasilapStorage"));
        else
            services.Configure<GyartasilapStorageOptions>(_ => { });

        var storageCfg = configuration
            ?.GetSection("GyartasilapStorage")
            .Get<GyartasilapStorageOptions>() ?? new GyartasilapStorageOptions();

        if (storageCfg.Enabled
            && !string.IsNullOrWhiteSpace(storageCfg.AccessKey)
            && !string.IsNullOrWhiteSpace(storageCfg.SecretKey))
        {
            services.AddSingleton<IMinioClient>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<GyartasilapStorageOptions>>().Value;
                return new MinioClient()
                    .WithEndpoint(opts.Endpoint)
                    .WithCredentials(opts.AccessKey, opts.SecretKey)
                    .WithSSL(opts.UseSSL)
                    .Build();
            });
            services.AddScoped<IGyartasilapStorage, GyartasilapMinioStorage>();
        }
        else
        {
            services.AddScoped<IGyartasilapStorage, NullGyartasilapStorage>();
        }

        services.AddScoped<IGyartasilapRepository, GyartasilapRepository>();
        services.AddScoped<IGyartasilapBatchRepository, GyartasilapBatchRepository>();

        services.AddScoped<IAnyaglistaPdfBuilder, AnyaglistaPdfBuilder>();
        services.AddScoped<IAnyaglistaRepository, AnyaglistaRepository>();

        services.AddScoped<IDoorCalculationService, DoorCalculationService>();
        services.AddScoped<IHardwareResolutionService, HardwareResolutionService>();
        services.AddScoped<IProcessFlowService, ProcessFlowService>();
        services.AddScoped<IMaterialRequirementService, MaterialRequirementService>();

        services.AddScoped<IDoorOrderRepository, DoorOrderRepository>();
        services.AddScoped<IDoorRulesRepository, DoorRulesRepository>();
        services.AddScoped<IDataSeeder, DoorRulesDataSeeder>();
        services.AddScoped<ICuttingProvider, CuttingProviderStub>();

        // Product configurator services
        services.AddScoped<IProductTemplateRepository, ProductTemplateRepository>();
        services.AddScoped<IProductConfigurationRepository, ProductConfigurationRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IProductConfiguratorService, ProductConfiguratorService>();
        services.AddScoped<IWorkOrderPdfService, WorkOrderPdfService>();

        // Register MediatR handlers that live in the Infrastructure assembly
        // (e.g. SaveCalculationResultCommandHandler which needs JoineryDbContext directly)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Outbox writer: scoped so it shares the same DbContext as the calling handler
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        // Orchestrator HTTP client
        var orchestratorBaseUrl = configuration?["Orchestrator:BaseUrl"] ?? "http://localhost:3000";
        services.AddHttpClient<IOrchestratorClient, OrchestratorClient>(client =>
        {
            client.BaseAddress = new Uri(orchestratorBaseUrl);
        });

        // Background workers
        services.AddHostedService<JoineryOutboxWorker>();
        services.AddHostedService<JoineryOutboxCleanupJob>();

        return services;
    }
}
