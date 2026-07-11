// Identity.Api/Program.cs

using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Users.Commands;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Cache;
using Identity.Infrastructure.CurrentUser;
using Identity.Infrastructure.Keycloak;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.RateLimiting;
using Identity.Infrastructure.Workers;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ── Configuration bindings ───────────────────────────────────────────────────
var kcOptions = builder.Configuration
    .GetSection(KeycloakOptions.SectionName)
    .Get<KeycloakOptions>() ?? new KeycloakOptions();

builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection(KeycloakOptions.SectionName));

// ── JWT Authentication (Keycloak RS256, JWKS endpoint) ───────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience  = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = false; // VPS terminates TLS at nginx

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            // MapInboundClaims=false: keeps "tid", "sub", "realm_access" as-is
            // (avoids the WS-Fed claim remapping that breaks tid/sub reads)
            NameClaimType  = "preferred_username",
            RoleClaimType  = "realm_access.roles"
        };

        // SEC-09: tid claim must come from validated JWT — never from headers
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization policies ───────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantMember", p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("tid"));

    options.AddPolicy("TenantAdmin", p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("tid")
         .RequireRole("TenantAdmin"));

    options.AddPolicy("SuperAdmin", p =>
        p.RequireAuthenticatedUser()
         .RequireRole("SuperAdmin"));
});

// ── MediatR + FluentValidation ───────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CreateUserCommand).Assembly);

// ── EF Core / PostgreSQL ─────────────────────────────────────────────────────
builder.Services.AddDbContext<IdentityDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("IdentityDb"),
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity")));

// ── Redis ────────────────────────────────────────────────────────────────────
var redisCs = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisCs))
{
    try
    {
        var redisCfg = ConfigurationOptions.Parse(redisCs);
        redisCfg.AbortOnConnectFail = false;
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisCfg));
    }
    catch
    {
        // Redis unavailable at startup — services handle null gracefully
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null!);
    }
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null!);
}

// ── Infrastructure services ──────────────────────────────────────────────────
builder.Services.AddScoped<ISpaceOSUserRepository, SpaceOSUserRepository>();
builder.Services.AddScoped<IKcSyncOutboxRepository, KcSyncOutboxRepository>();
builder.Services.AddScoped<IKcSyncOutboxProcessor, KcSyncOutboxRepository>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddSingleton<IRateLimitService, RedisRateLimitService>();
builder.Services.AddSingleton<UserCacheService>();

builder.Services.AddHttpContextAccessor();

// ── Keycloak Admin Client ────────────────────────────────────────────────────
builder.Services.AddHttpClient<IKeycloakTokenProvider, KeycloakTokenProvider>();
builder.Services.AddHttpClient<IIdentityProviderClient, KeycloakAdminClient>();
builder.Services.AddScoped<KcSyncProcessor>();

// ── Background worker ────────────────────────────────────────────────────────
builder.Services.AddHostedService<KcSyncWorkerService>();

// ── Health check ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Port / binding ───────────────────────────────────────────────────────────
// Production: set ASPNETCORE_URLS=http://127.0.0.1:5008 in systemd unit
// or add Kestrel:Endpoints section in appsettings.Production.json.
// Not hard-coded here so WebApplicationFactory (tests) can use its own transport.

// ════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ════════════════════════════════════════════════════════════════════════════

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Expose Program class for WebApplicationFactory in tests
public partial class Program { }
