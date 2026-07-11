// SpaceOS.Kernel.Api/Program.cs
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpaceOS.Kernel.Application;
using SpaceOS.Infrastructure;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Api.Endpoints;
using SpaceOS.Infrastructure.Security;
using SpaceOS.Kernel.Api.Middleware;
using SpaceOS.Kernel.Api.OpenApi;
using SpaceOS.Modules.FlowManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "SpaceOS Kernel API",
        Version     = "v1",
        Description = "SpaceOS Kernel — Clean Architecture REST API. Authenticate with JWT Bearer."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix). Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.SchemaFilter<PagedListSchemaFilter>();
    c.SchemaFilter<EnumStringSchemaFilter>();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// KC-T1: Keycloak OIDC authority-based JWKS validation.
// JWKS keys are fetched automatically from Authority/.well-known/openid-configuration.
// realm_access.roles are mapped to ClaimTypes.Role in OnTokenValidated.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience  = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // Preserve JWT claim names as-is (e.g. "tid", "spaceos_tenants").
        // Default MapInboundClaims=true would remap "tid" → Microsoft tenantid URI,
        // breaking ClaimsTenantResolver and TenantSessionInterceptor which look for "tid".
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer    = true,
            ValidateAudience  = true,
            ValidateLifetime  = true,
            ClockSkew         = TimeSpan.FromSeconds(30),
            NameClaimType     = "preferred_username",
            RoleClaimType     = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/problem+json";
                var problem = new
                {
                    type   = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    title  = "Unauthorized",
                    status = 401,
                    detail = "A valid JWT Bearer token is required."
                };
                return ctx.Response.WriteAsJsonAsync(problem);
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode  = 403;
                ctx.Response.ContentType = "application/problem+json";
                var problem = new
                {
                    type   = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title  = "Forbidden",
                    status = 403,
                    detail = "Insufficient permissions for this operation."
                };
                return ctx.Response.WriteAsJsonAsync(problem);
            },
            OnTokenValidated = context =>
            {
                // Map Keycloak realm_access.roles → ClaimTypes.Role
                var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;
                if (realmAccess is not null)
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonDocument.Parse(realmAccess);
                        if (parsed.RootElement.TryGetProperty("roles", out var roles))
                        {
                            var identity = context.Principal!.Identity as ClaimsIdentity;
                            foreach (var role in roles.EnumerateArray())
                            {
                                var roleStr = role.GetString();
                                if (!string.IsNullOrEmpty(roleStr))
                                    identity!.AddClaim(new Claim(ClaimTypes.Role, roleStr));
                            }
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Ignore malformed realm_access — token validation will fail on missing roles
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// KC-T3: JWKS health check — registered via Infrastructure extension to keep JwksHealthCheck internal (I2).
builder.Services.AddHealthChecks().AddKernelHealthChecks();
builder.Services.AddHttpClient("JwksHealthCheck");

// OutputCache — replaces AddMemoryCache for the JWKS endpoint (BE-P15-06).
builder.Services.AddOutputCache(opt =>
    opt.AddPolicy("jwks-cache", policy =>
        policy.Expire(TimeSpan.FromHours(1))
              .SetVaryByHeader("Accept")
              .Tag("jwks")));

// Rate limiting — identity-partitioned by JWT sub + TenantId claim.
// Partitions are in-process (no shared state). Redis-backed distributed limiting deferred to Sprint 3.
// Limits are configurable via appsettings / environment variables (e.g. RateLimit__WritePerMinute=1000)
// so that E2E / staging environments can relax limits without switching away from Production mode.
var isDevelopment = builder.Environment.IsDevelopment();
var rl = builder.Configuration.GetSection("RateLimit");
var rlFixed       = rl.GetValue<int>("FixedPerMinute",    isDevelopment ? 500 : 100);
var rlWrite       = rl.GetValue<int>("WritePerMinute",    isDevelopment ? 500 : 20);
var rlSyncSignal  = rl.GetValue<int>("SyncSignalPerMin",  isDevelopment ? 500 : 50);
var rlNodeReg     = rl.GetValue<int>("NodeRegPerMin",     isDevelopment ? 500 : 10);
var rlNodeHb      = rl.GetValue<int>("NodeHbPerMin",      isDevelopment ? 500 : 120);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type   = "https://httpstatuses.io/429",
            Title  = "Too Many Requests",
            Status = 429,
            Detail = "Rate limit exceeded. Please retry after 60 seconds."
        }, ct).ConfigureAwait(false);
    };

    // Each authenticated identity (sub:tid) gets its own window.
    // Unauthenticated callers are partitioned by remote IP address.
    options.AddPolicy("fixed", context =>
    {
        var key = GetRateLimitKey(context);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window       = TimeSpan.FromSeconds(60),
            PermitLimit  = rlFixed,
            QueueLimit   = 0,
        });
    });

    options.AddPolicy("sliding", context =>
    {
        var key = GetRateLimitKey(context);
        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            Window            = TimeSpan.FromSeconds(60),
            PermitLimit       = rlWrite,
            SegmentsPerWindow = 6,
            QueueLimit        = 0,
        });
    });

    // S-11: SyncSignal rate limit — 50 signals per minute, sliding window.
    options.AddPolicy("sync-signal", context =>
    {
        var key = GetRateLimitKey(context);
        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            Window            = TimeSpan.FromMinutes(1),
            PermitLimit       = rlSyncSignal,
            SegmentsPerWindow = 6,
            QueueLimit        = 0,
        });
    });

    // BE-06: Node registration — 10 per minute fixed window (admin-only, low volume expected).
    options.AddPolicy("node-register", context =>
    {
        var key = GetRateLimitKey(context);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window      = TimeSpan.FromMinutes(1),
            PermitLimit = rlNodeReg,
            QueueLimit  = 0,
        });
    });

    // BE-06: Node heartbeat — 120 per minute fixed window (high-frequency keepalive).
    options.AddPolicy("node-heartbeat", context =>
    {
        var key = GetRateLimitKey(context);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window      = TimeSpan.FromMinutes(1),
            PermitLimit = rlNodeHb,
            QueueLimit  = 0,
        });
    });
});

// Authorization policies (RBAC)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadPolicy",  policy => policy.RequireRole("Joiner", "Designer", "Admin"));
    options.AddPolicy("WritePolicy", policy => policy.RequireRole("Designer", "Admin"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));

    // Stage Registry RBAC (SEC-02)
    options.AddPolicy("SystemAdminPolicy",   policy => policy.RequireRole("SystemAdmin"));
    options.AddPolicy("TenantAdminPolicy",   policy => policy.RequireRole("TenantAdmin", "SystemAdmin"));
    options.AddPolicy("StageOperatorPolicy", policy => policy.RequireRole("StageOperator", "TenantAdmin", "SystemAdmin"));
});

// BE-P2-08: UseForwardedHeaders — required when running behind an Nginx reverse proxy on VPS.
// KnownProxies is set to loopback only (Nginx on the same host). KnownNetworks is cleared so that
// only the explicitly listed proxy IP is trusted — prevents X-Forwarded-For spoofing from arbitrary sources.
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownProxies.Add(IPAddress.Loopback);      // 127.0.0.1 — Nginx loopback proxy
    opts.KnownNetworks.Clear();                      // only explicit KnownProxies are trusted
    opts.RequireHeaderSymmetry = false;
});

// T-06: Limit request body size to 64 KB at the Kestrel level.
// This prevents large IntentDataJson payload injection before any middleware or handler runs.
// The IntentDataSchemaValidator enforces the same limit at the application layer as a second gate.
builder.WebHost.ConfigureKestrel(opts =>
    opts.Limits.MaxRequestBodySize = 64 * 1024);

// ModulesDbContext — same connection string as AppDbContext, isolated schema on PostgreSQL.
var modulesConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<ModulesDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(modulesConnectionString);
    }
    else
    {
        // EnableRetryOnFailure is intentionally omitted for ModulesDbContext.
        // FlowManagement and other modules may use explicit user-initiated transactions.
        // NpgsqlRetryingExecutionStrategy calls OnFirstExecution() on every EF Core operation,
        // which throws when a user transaction is active — regardless of whether the operation
        // itself would fail. Removing the retry strategy from contexts that own explicit transactions
        // is the correct approach; retry at the application/HTTP client level is the fallback.
        options.UseNpgsql(modulesConnectionString);
    }
});

var app = builder.Build();

// Dev/Testing: ensure SQLite schema exists on startup using EnsureCreated (no migrations).
// SQLite does not support all migration operations (e.g. RenameColumn via ALTER TABLE).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

    var modulesDb = scope.ServiceProvider.GetRequiredService<ModulesDbContext>();
    await modulesDb.Database.EnsureCreatedAsync().ConfigureAwait(false);
}
else if (app.Environment.IsEnvironment("Testing"))
{
    // Testing: ApiFactory replaces AppDbContext with SQLite in-memory.
    // Use EnsureCreated so the schema is built from the live model without running migrations.
    // ModulesDbContext is not used directly in API tests — skip its initialization here.
    // Swallow exceptions for tests that deliberately inject a broken DB connection
    // (e.g. HealthEndpoint_WhenDbUnavailable_StillReturns200).
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
    catch
    {
        // Intentionally ignored — the health endpoint must still respond even when the DB is unavailable.
    }
}
else
{
    // Non-development: apply pending EF Core migrations, then PostgreSQL-specific schema objects.
    // NOTE: MigrateAsync is intentionally called here as a one-time operational fix to apply
    // migrations 0015–0024 that were not applied via dotnet ef database update.
    // Remove MigrateAsync after migrations are confirmed applied; revert to schema-init only.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
    await PostgresSchemaInitializer.ApplyAsync(db).ConfigureAwait(false);
}

// Global exception handler — must be first in the pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// BE-P2-08: Must be before UseAuthentication so that the real client IP (from X-Forwarded-For)
// is present in HttpContext.Connection.RemoteIpAddress when the rate limiter and auth run.
app.UseForwardedHeaders();

app.UseRateLimiter();
app.UseOutputCache(); // BE-P15-06: unified caching — JWKS endpoint uses CacheOutput("jwks-cache")

// ADR-039: Internal endpoint guard — header-based auth, JWT is not involved
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/internal"),
    inner => inner.UseMiddleware<InternalHeaderMiddleware>());

app.UseAuthentication();
app.UseAuthorization();

// BE-06: Enforces SpaceOS-SIP-Version header on /api/sync/* and /api/nodes/* paths.
app.UseMiddleware<SipVersionMiddleware>();

// S-04 / BE-P15-03: app.current_tenant_id is now set at SESSION level (is_local=false)
// by TenantSessionInterceptor : DbConnectionInterceptor on every opened PG connection.
// TenantContextMiddleware has been removed — the interceptor replaces it entirely and
// avoids the cross-context RLS bypass that is_local=true caused.

app.MapGet("/healthz", async (AppDbContext db, CancellationToken ct) =>
{
    bool connected;
    try
    {
        connected = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
    }
    catch
    {
        connected = false;
    }
    return Results.Ok(new { status = "healthy", db = connected ? "connected" : "unavailable" });
})
.WithTags("Health")
.WithName("HealthCheck")
.ExcludeFromDescription()
.DisableRateLimiting()
.AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
})
.WithTags("Health")
.ExcludeFromDescription()
.DisableRateLimiting()
.AllowAnonymous();

app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapFacilityEndpoints();
app.MapWorkStationEndpoints();
app.MapSpaceLayerEndpoints();
app.MapFlowEpicEndpoints();
app.MapAuditEventEndpoints();
app.MapSnapshotEndpoints();
app.MapDashboardEndpoints();
app.MapGdprEndpoints();
app.MapNodeEndpoints();
app.MapSyncEndpoints();
app.MapToolEndpoints();
app.MapLlmToolEndpoints();
app.MapSpaceEndpoints();
app.MapHandshakeEndpoints();
app.MapStageEndpoints();
app.MapModuleRegistryEndpoints();
app.MapInternalEndpoints();

// Swagger JSON spec — available in all environments (ADR-07 / KERNEL-087).
// The Swashbuckle middleware dynamically generates the spec from the registered endpoints.
// In Production the same /openapi/v1.json route is served; SwaggerUI is restricted to Development.
app.UseSwagger(c => c.RouteTemplate = "openapi/{documentName}.json");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "SpaceOS v1"));
}

app.Run();

// Builds the rate-limit partition key for the given HTTP context.
// BE-P2-08: clientIp is the real IP after ForwardedHeaders middleware has rewritten
//           HttpContext.Connection.RemoteIpAddress from X-Forwarded-For.
// Key is SHA-256(rawKey) to bound length and avoid timing side-channels on string comparison.
static string GetRateLimitKey(HttpContext context)
{
    var sub = context.User.FindFirst("sub")?.Value
           ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var tid = context.User.FindFirst("tid")?.Value;

    string rawKey;
    if (string.IsNullOrEmpty(sub))
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anon";
        rawKey = $"anon:{ip}";
    }
    else
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        rawKey = $"{sub}:{tid}:{ip}";
    }

    // SHA-256 bounds the key to 64 hex chars and prevents timing side-channels.
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
    return Convert.ToHexString(hash);
}

/// <summary>Exposes <see cref="Program"/> for <see cref="WebApplicationFactory{TEntryPoint}"/> in integration tests.</summary>
public partial class Program { }
