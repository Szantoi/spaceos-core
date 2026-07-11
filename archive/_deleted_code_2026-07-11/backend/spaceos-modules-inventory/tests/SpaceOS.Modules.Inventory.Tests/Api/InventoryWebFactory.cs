using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Tests.Api;

/// <summary>
/// WebApplicationFactory for Inventory integration tests.
/// Replaces PostgreSQL + Npgsql with in-memory EF Core.
/// TenantSessionInterceptor is internal — not referenced directly;
/// the in-memory DbContext is registered fresh without it.
/// </summary>
public sealed class InventoryWebFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own isolated DB so parallel tests don't collide.
    private readonly string _dbName = $"InventoryTest-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Supply the worker connection string so Program.cs startup does not throw.
        // UseSetting is the IWebHostBuilder way to inject config before the builder reads it.
        builder.UseSetting("INVENTORY_WORKER_CONNECTION_STRING", "placeholder-for-tests");

        builder.ConfigureServices(services =>
        {
            // ── Remove real InventoryDbContext (Npgsql + TenantSessionInterceptor) ──
            var dbOpts = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>))
                .ToList();
            foreach (var d in dbOpts) services.Remove(d);

            // ── Add in-memory InventoryDbContext ──────────────────────────────────
            services.AddDbContext<InventoryDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // ── Remove real InventoryWorkerDbContext (registered with Npgsql) ─────
            var workerOpts = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<InventoryWorkerDbContext>))
                .ToList();
            foreach (var d in workerOpts) services.Remove(d);

            // ── Add in-memory InventoryWorkerDbContext ────────────────────────────
            services.AddDbContext<InventoryWorkerDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName + "-worker"));
        });
    }
}
