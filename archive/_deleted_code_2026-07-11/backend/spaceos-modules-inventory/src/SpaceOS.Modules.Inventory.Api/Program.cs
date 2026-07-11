using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Modules.Inventory.Api.Endpoints;
using SpaceOS.Modules.Inventory.Api.Extensions;
using SpaceOS.Modules.Inventory.Infrastructure.Extensions;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInventoryApplication();

var jwtAuthority = builder.Configuration["Jwt:Authority"]
    ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "kernel-api";

if (builder.Environment.IsProduction())
    ArgumentNullException.ThrowIfNullOrEmpty(jwtAuthority,
        "Jwt:Authority / JWT_AUTHORITY must be configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.Authority = jwtAuthority;
        opts.Audience = jwtAudience;
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.FromSeconds(30),
            NameClaimType    = "preferred_username",
            RoleClaimType    = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));

var connectionString = builder.Configuration.GetConnectionString("Inventory")
    ?? "Host=localhost;Database=spaceos;Username=spaceos_app;Password=changeme";

var workerConnStr = builder.Configuration["INVENTORY_WORKER_CONNECTION_STRING"]
    ?? throw new InvalidOperationException("INVENTORY_WORKER_CONNECTION_STRING not set");

builder.Services.AddDbContext<InventoryWorkerDbContext>(opts =>
    opts.UseNpgsql(workerConnStr));

builder.Services.AddInventoryInfrastructure(connectionString);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapInventoryEndpoints();
app.MapOffcutEndpoints();
app.MapInternalInventoryEndpoints();
app.MapInventoryIntegrationEndpoints();
app.MapProcurementReceiverEndpoints();
app.MapHealthChecks("/health");
app.Run();

public partial class Program { }
