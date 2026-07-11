using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SpaceOS.Modules.Sales.Api;
using SpaceOS.Modules.Sales.Api.Endpoints;
using SpaceOS.Modules.Sales.Application.Behaviors;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (JWT Bearer) ──────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Auth:Authority"];
        opts.MapInboundClaims = false;
        opts.TokenValidationParameters.ValidIssuer = builder.Configuration["Auth:Issuer"];
        opts.TokenValidationParameters.ValidateAudience = false;
    });

// ── Authorization policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("TenantUser",  p => p.RequireAuthenticatedUser());
    opts.AddPolicy("SalesUser",   p => p.RequireAuthenticatedUser()
        .RequireClaim("role", "sales_user", "sales_admin", "tenant_admin"));
    opts.AddPolicy("TenantAdmin", p => p.RequireAuthenticatedUser()
        .RequireClaim("role", "tenant_admin"));
});

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("per-tenant", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.FindFirst("tenant_id")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anon",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    opts.AddPolicy("convert", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.FindFirst("tenant_id")?.Value ?? "anon",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    opts.RejectionStatusCode = 429;
});

// ── MediatR + FluentValidation ────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(CreateCustomerCommandHandler).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(CreateCustomerCommandValidator).Assembly);

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddSalesInfrastructure(builder.Configuration);

// ── HTTP tenant context (registered after Infrastructure so it overrides) ─────
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────────────
app.MapCustomerEndpoints();
app.MapQuoteEndpoints();
app.MapPipelineEndpoints();

// Health probe
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "sales" }));

app.Run();

// Allow test project to reference the entry point
public partial class Program { }
