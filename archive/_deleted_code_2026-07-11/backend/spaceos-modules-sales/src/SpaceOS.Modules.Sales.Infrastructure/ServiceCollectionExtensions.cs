using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SpaceOS.Modules.Sales.Abstractions.Ports;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Outbox;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Infrastructure.Adapters;
using SpaceOS.Modules.Sales.Infrastructure.Common;
using SpaceOS.Modules.Sales.Infrastructure.Generators;
using SpaceOS.Modules.Sales.Infrastructure.Outbox;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;
using SpaceOS.Modules.Sales.Infrastructure.Repositories;
using SpaceOS.Modules.Sales.Infrastructure.Security;

namespace SpaceOS.Modules.Sales.Infrastructure;

/// <summary>
/// Registers all Infrastructure-layer services into the DI container.
/// Call from <c>Program.cs</c> in the API project.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds all Infrastructure services required by the Sales module.</summary>
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContext ────────────────────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<TenantSessionInterceptor>();
        services.AddScoped<AuditAndDispatchInterceptor>();

        services.AddDbContext<SalesDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("SalesDb"),
                npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_sales"))
                .AddInterceptors(
                    sp.GetRequiredService<TenantSessionInterceptor>(),
                    sp.GetRequiredService<AuditAndDispatchInterceptor>());
        });

        // ── Core services ────────────────────────────────────────────────────
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IQuoteNumberGenerator, QuoteNumberGenerator>();
        services.AddScoped<IQuotaGuard, QuotaGuard>();

        // ── Worker ───────────────────────────────────────────────────────────
        var workerConnString = configuration.GetConnectionString("SalesDb_Worker")
            ?? throw new InvalidOperationException("SalesDb_Worker connection string is required.");
        services.AddSingleton<ISalesWorkerDbContextFactory>(
            _ => new SalesWorkerDbContextFactory(workerConnString));
        services.AddHostedService<SalesIntegrationWorker>();

        // ── HTTP adapters (with Polly retry) ─────────────────────────────────
        static IAsyncPolicy<HttpResponseMessage> RetryPolicy()
            => HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        services.AddHttpClient<IOrderConversionPort, JoineryOrderConversionClient>()
            .AddPolicyHandler(RetryPolicy());

        services.AddHttpClient<IActorDirectoryPort, KernelActorDirectoryClient>()
            .AddPolicyHandler(RetryPolicy());

        return services;
    }
}
