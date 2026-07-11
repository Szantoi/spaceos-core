using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Api.Endpoints;
using SpaceOS.Modules.Joinery.Application;
using SpaceOS.Modules.Joinery.Application.Seeding;
using SpaceOS.Modules.Joinery.Infrastructure;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("JoineryDb")
    ?? Environment.GetEnvironmentVariable("JOINERY_CONNECTION_STRING")
    ?? throw new InvalidOperationException("JoineryDb connection string not configured");

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "kernel-api";

var jwtAuthority = builder.Configuration["Jwt:Authority"]
    ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");

// M3: fail-fast in Production if authority is not configured
if (builder.Environment.IsProduction())
    ArgumentNullException.ThrowIfNullOrEmpty(jwtAuthority,
        "Jwt:Authority / JWT_AUTHORITY must be configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = jwtAuthority;
        opts.RequireHttpsMetadata = false;
        opts.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            // M2: enforce audience — only tokens issued for this service are accepted
            ValidateAudience = true,
            ValidAudience    = jwtAudience,
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("ManufacturerOnly", policy =>
        policy.RequireClaim("tenant_type", "Manufacturer"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapDoorOrderEndpoints();
app.MapProductEndpoints();
app.MapWorkOrderEndpoints();
app.MapGyartasilapEndpoints();
app.MapAnyaglistaEndpoints();
app.MapInternalEndpoints();

// Internal conversion middleware (loopback-only, secret header, tenant header)
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/joinery/internal"),
    branch => branch.UseMiddleware<SpaceOS.Modules.Joinery.Infrastructure.Security.InternalOrderConversionMiddleware>());

app.MapInternalOrdersEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "spaceos-joinery" }))
   .AllowAnonymous();

// T1: Migrations / schema creation (skipped for in-memory test providers)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync().ConfigureAwait(false);
    else
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();

    // In-memory provider: no raw SQL seeder, no fatal guard (used in integration tests)
    if (!db.Database.IsRelational()) return;

    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None).ConfigureAwait(false);

    var count = await db.DoorTypeRules.CountAsync().ConfigureAwait(false);
    if (count == 0)
        throw new InvalidOperationException("FATAL: DoorTypeRules seed empty — startup aborted");
});

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests
public partial class Program { }
