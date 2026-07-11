// SpaceOS.Kernel.IntegrationTests/Infrastructure/SpaceOsApiFactory.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;
using System.Security.Cryptography;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> configured with an in-memory SQLite database
/// for integration tests. Keeps the connection open to preserve the in-memory schema between
/// operations and registers <see cref="EventCaptureService"/> so tests can assert on domain events.
/// A fixed <see cref="TestTenantId"/> is used to isolate all test data to a single tenant.
/// </summary>
public sealed class SpaceOsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    /// <summary>The fixed <see cref="TenantId"/> used for all integration tests.</summary>
    public static readonly TenantId TestTenantId = TenantId.From(new Guid("10000000-0000-0000-0000-000000000001"));

    /// <summary>Initialises the factory and opens the shared in-memory SQLite connection.</summary>
    public SpaceOsApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Provides access to captured domain events dispatched during the current test run.
    /// Populated by <see cref="EventCaptureService"/> which replaces the production
    /// <see cref="IDomainEventDispatcher"/> registration inside the test host.
    /// </summary>
    public IEventCapture Capture => Services.GetRequiredService<IEventCapture>();

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContext registration with in-memory SQLite.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Register stub ITenantResolver for tests — reads tid claim from the JWT so that
            // tenant-filter tests can verify per-tenant data isolation end-to-end.
            var tenantResolverDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantResolver));
            if (tenantResolverDescriptor is not null)
                services.Remove(tenantResolverDescriptor);

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantResolver, ClaimsStubTenantResolver>();

            // Override JWT Bearer validation to accept RS256 test-signed tokens.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new RsaSecurityKey(JwtTokenHelper.TestRsa),
                    ValidAlgorithms          = [SecurityAlgorithms.RsaSha256],
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateLifetime         = true
                };
            });

            // Replace the scoped DomainEventDispatcher with a singleton EventCaptureService
            // so that domain events are captured across scopes and can be asserted by tests.
            var dispatcherDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDomainEventDispatcher));
            if (dispatcherDescriptor is not null)
                services.Remove(dispatcherDescriptor);

            services.AddSingleton<EventCaptureService>();
            services.AddSingleton<IDomainEventDispatcher>(sp =>
                sp.GetRequiredService<EventCaptureService>());
            services.AddSingleton<IEventCapture>(sp =>
                sp.GetRequiredService<EventCaptureService>());
        });
    }

    /// <summary>Creates the database schema for each test run.</summary>
    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>Seeds optional data into the database using an action over <see cref="AppDbContext"/>.</summary>
    /// <param name="seed">Optional seed action to add entities before saving.</param>
    public async Task SeedAsync(Func<AppDbContext, Task>? seed = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        if (seed is not null)
        {
            await seed(db).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        _connection.Dispose();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
