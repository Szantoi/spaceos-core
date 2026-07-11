using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Abstractions.Api.Endpoints;
using SpaceOS.Modules.Abstractions.Application.Common;
using SpaceOS.Modules.Abstractions.Application.Seeding;
using SpaceOS.Modules.Abstractions.Infrastructure;
using SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AbstractionsDb")
    ?? Environment.GetEnvironmentVariable("ABSTRACTIONS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("AbstractionsDb connection string not configured");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Jwt:Authority"]
            ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");
        opts.RequireHttpsMetadata = false;
        opts.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "kernel-api",
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

app.MapProductTemplateEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "spaceos-abstractions" }))
   .AllowAnonymous();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AbstractionsDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync().ConfigureAwait(false);
    else
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AbstractionsDbContext>();
    if (!db.Database.IsRelational()) return;
    var seeder = scope.ServiceProvider.GetRequiredService<ITemplateSeeder>();
    await seeder.SeedAsync(CancellationToken.None).ConfigureAwait(false);
});

app.Run("http://127.0.0.1:5003");

public partial class Program { }
