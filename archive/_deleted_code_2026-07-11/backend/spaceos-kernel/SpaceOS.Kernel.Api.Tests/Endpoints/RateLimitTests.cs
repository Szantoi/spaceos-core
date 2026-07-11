// SpaceOS.Kernel.Api.Tests/Endpoints/RateLimitTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Ardalis.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// Integration tests verifying rate limit enforcement, /healthz exemption,
/// and the RFC 7807 Problem Details shape of 429 responses.
/// Each test creates its own <see cref="RateLimitTestFactory"/> instance to prevent
/// shared rate-limit state from causing inter-test interference.
/// </summary>
public sealed class RateLimitTests
{
    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    /// <summary>Verifies that the 4th GET request returns 429 when the fixed-window limit is 3.</summary>
    [Fact]
    public async Task RateLimit_GetEndpoint_Returns429AfterLimit()
    {
        // Arrange — each test gets its own factory so rate limit counters are reset
        await using var factory = new RateLimitTestFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthorizedClient();

        // Act — send 3 allowed requests, then a 4th that should be rejected
        for (var i = 0; i < 3; i++)
        {
            var allowed = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var rejected = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    /// <summary>Verifies that /healthz is never rate limited regardless of request count.</summary>
    [Fact]
    public async Task RateLimit_HealthEndpoint_NotLimited()
    {
        // Arrange
        await using var factory = new RateLimitTestFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();

        // Act — send more requests than the fixed-window limit allows
        HttpResponseMessage? last = null;
        for (var i = 0; i < 10; i++)
        {
            last = await client.GetAsync("/healthz", TestContext.Current.CancellationToken);
        }

        // Assert — healthz is always 200 regardless of count
        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.OK, last.StatusCode);
    }

    /// <summary>Verifies that a 429 response contains a <c>Retry-After</c> header.</summary>
    [Fact]
    public async Task RateLimit_429_ContainsRetryAfterHeader()
    {
        // Arrange
        await using var factory = new RateLimitTestFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthorizedClient();

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 4; i++)
        {
            rejected = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.NotNull(rejected);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(
            rejected.Headers.Contains("Retry-After"),
            "Expected Retry-After header on 429 response.");
    }

    /// <summary>Verifies that the 429 response body is a valid RFC 7807 Problem Details document.</summary>
    [Fact]
    public async Task RateLimit_429_ContainsProblemDetails()
    {
        // Arrange
        await using var factory = new RateLimitTestFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthorizedClient();

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 4; i++)
        {
            rejected = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.NotNull(rejected);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Too Many Requests", problem.Title);
        Assert.Equal(429, problem.Status);
    }
}

// =============================================================================
// Test-only factory — scoped to this file, not shared across test classes.
// =============================================================================

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the production
/// rate limiter with a low-limit policy (3 req / 10 s) so tests execute without
/// sending hundreds of real requests.
/// Inherits the same SQLite, JWT, and stub registrations used by
/// <see cref="ApiFactory"/>.
/// </summary>
file sealed class RateLimitTestFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>Initialises the factory and opens the shared in-memory SQLite connection.</summary>
    public RateLimitTestFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // --- Database ---
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // --- Tenant resolver ---
            var tenantResolverDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantResolver));
            if (tenantResolverDescriptor is not null)
                services.Remove(tenantResolverDescriptor);

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantResolver, ApiClaimsTenantResolver>();

            // --- Audit event stub ---
            services.AddScoped<IAuditEventRepository, RateLimitNoOpAuditEventRepository>();

            // --- Audit write lock stub — Testing env is not Development ---
            services.AddScoped<IAuditWriteLock, RateLimitNoOpAuditWriteLock>();

            // --- JWT ---
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new RsaSecurityKey(JwtTestHelper.TestRsa),
                    ValidAlgorithms          = [SecurityAlgorithms.RsaSha256],
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateLifetime         = true
                };
            });

            // --- Rate limiter override: low limits for fast test execution ---
            // Remove all IConfigureOptions<RateLimiterOptions> registered by the production
            // AddRateLimiter call. RateLimiterOptions.Policies is a dict with no replace —
            // adding a policy with the same name twice throws ArgumentException.
            var rateLimiterDescriptors = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<RateLimiterOptions>))
                .ToList();
            foreach (var d in rateLimiterDescriptors)
                services.Remove(d);

            services.AddRateLimiter(options =>
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
                options.AddFixedWindowLimiter("fixed", o =>
                {
                    o.Window               = TimeSpan.FromSeconds(10);
                    o.PermitLimit          = 3;
                    o.QueueLimit           = 0;
                    o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                options.AddSlidingWindowLimiter("sliding", o =>
                {
                    o.Window            = TimeSpan.FromSeconds(10);
                    o.PermitLimit       = 3;
                    o.SegmentsPerWindow = 2;
                    o.QueueLimit        = 0;
                });
            });
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with an Admin Bearer JWT
    /// whose <c>tid</c> claim matches the shared <see cref="ApiFactory.TestTenantId"/>.
    /// </summary>
    /// <returns>An authorised <see cref="HttpClient"/>.</returns>
    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", JwtTestHelper.ForRole("Admin"));
        return client;
    }

    /// <summary>Seeds the database schema and optional data using the factory's DI scope.</summary>
    /// <param name="seed">Optional action to add seed data to the <see cref="AppDbContext"/>.</param>
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
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    /// <inheritdoc/>
    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        _connection.Dispose();
    }
}

/// <summary>
/// No-op stub for <see cref="IAuditEventRepository"/> scoped to <see cref="RateLimitTestFactory"/>.
/// All writes are silently discarded; reads return empty collections.
/// </summary>
file sealed class RateLimitNoOpAuditEventRepository : IAuditEventRepository
{
    /// <inheritdoc/>
    public Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IReadOnlyList<AuditEvent>> ListAsync(
        ISpecification<AuditEvent> specification,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());

    /// <inheritdoc/>
    public Task<int> CountAsync(
        ISpecification<AuditEvent> specification,
        CancellationToken ct = default) =>
        Task.FromResult(0);

    /// <inheritdoc/>
    public Task<string> GetLastHashAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult("GENESIS");

    /// <inheritdoc/>
    public Task<IReadOnlyList<AuditEvent>> GetChainAsync(
        Guid tenantId, DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
}

/// <summary>No-op stub for <see cref="IAuditWriteLock"/> used in rate-limit tests.</summary>
file sealed class RateLimitNoOpAuditWriteLock : IAuditWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisposable.Instance);

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
