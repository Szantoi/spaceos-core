using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Modules.Joinery.Application.Seeding;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// WebApplicationFactory for Joinery integration tests.
/// Replaces PostgreSQL + Npgsql with in-memory EF Core,
/// and JWT Bearer with a symmetric test-key scheme.
/// </summary>
public sealed class JoineryWebFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own isolated DB so parallel tests don't collide.
    private readonly string _dbName = $"JoineryTest-{Guid.NewGuid()}";

    /// <summary>Symmetric key used to sign and verify test JWTs.</summary>
    public const string TestSigningSecret =
        "joinery-integration-test-signing-secret-hs256-256bit-key!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(cfg =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Required by InternalOrderConversionMiddleware — tests can override per-factory
                ["SpaceOS:InternalSecret"] = "joinery-integration-test-internal-secret"
            }));

        builder.ConfigureServices(services =>
        {
            // ── Remove real DbContext (Npgsql + TenantSessionInterceptor) ──────
            var dbOpts = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<JoineryDbContext>))
                .ToList();
            foreach (var d in dbOpts) services.Remove(d);

            // ── Add in-memory DbContext ──────────────────────────────────────
            services.AddDbContext<JoineryDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // ── No-op seeder (in-memory provider can't run raw SQL) ──────────
            var seederDesc = services
                .SingleOrDefault(d => d.ServiceType == typeof(IDataSeeder));
            if (seederDesc is not null) services.Remove(seederDesc);
            services.AddScoped<IDataSeeder, NoOpDataSeeder>();

            // ── Override JWT Bearer to accept symmetric test tokens ───────────
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer            = false,
                        ValidateAudience          = false,
                        ValidateLifetime          = false,
                        ValidateIssuerSigningKey  = true,
                        IssuerSigningKey          = TestKey(),
                    };
                });
        });
    }

    // ── Token helpers ─────────────────────────────────────────────────────────

    /// <summary>Creates an HTTP client with a Manufacturer JWT attached.</summary>
    public HttpClient CreateAuthenticatedClient(
        string tenantId,
        string tenantType = "Manufacturer")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MakeToken(tenantId, tenantType));
        return client;
    }

    /// <summary>Creates a signed HS256 JWT with the given claims.</summary>
    public string MakeToken(string tenantId, string tenantType = "Manufacturer")
    {
        var claims = new[]
        {
            new Claim("tenant_id",   tenantId),
            new Claim("tenant_type", tenantType),
            new Claim("sub",         Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims:            claims,
            expires:           DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                TestKey(), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SymmetricSecurityKey TestKey() =>
        new(Encoding.UTF8.GetBytes(TestSigningSecret));

    // ── Seed minimal rules data so query handlers don't return NotFound ───────
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();

        if (!db.DoorTypeRules.Any())
        {
            db.DoorTypeRules.Add(new DoorTypeRule
            {
                DoorType       = "Falcos",
                AjtólapCount   = 1,
                BkmWidthFixed  = 0m,
                BkmHeightFixed = 0m,
                BkmWidthMoving = 0m,
                BkmHeightMoving = 0m,
            });
        }

        if (!db.GlobalConstants.Any())
        {
            db.GlobalConstants.Add(new GlobalConstant { Key = "CuttingOversize",  Value = 1.0m });
            db.GlobalConstants.Add(new GlobalConstant { Key = "CladdingOverhang", Value = 0.2m });
            db.GlobalConstants.Add(new GlobalConstant { Key = "MatyiWidth",       Value = 4.6m });
        }

        db.SaveChanges();
        return host;
    }

    // ── In-memory seeder no-op ────────────────────────────────────────────────

    private sealed class NoOpDataSeeder : IDataSeeder
    {
        public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
