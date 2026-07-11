// SpaceOS.Kernel.Api.Tests/Endpoints/HealthEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the /healthz endpoint.</summary>
public sealed class HealthEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises factory and HTTP client for this test class.</summary>
    public HealthEndpointTests()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateAuthorizedClient();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _factory.SeedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Verifies GET /healthz returns 200 OK with a body containing "healthy".</summary>
    [Fact]
    public async Task HealthEndpoint_Returns200WithHealthyBody()
    {
        var response = await _client.GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("healthy", body.Status);
    }

    /// <summary>Verifies the WebApplicationFactory boots without throwing an exception.</summary>
    [Fact]
    public async Task Application_StartsWithoutException()
    {
        var response = await _client.GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode,
            $"Expected successful status code but got {response.StatusCode}.");
    }

    /// <summary>Verifies GET /healthz response contains a "db" field (T4 — enhanced health check).</summary>
    [Fact]
    public async Task HealthEndpoint_ReturnsDbStatus()
    {
        var response = await _client.GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponseWithDb>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("healthy", body.Status);
        // SQLite in-memory connects successfully → "connected"
        Assert.Equal("connected", body.Db);
    }

    /// <summary>
    /// Verifies GET /healthz still returns 200 when the DB is unavailable (T4 — liveness not readiness).
    /// Uses a disposed SqliteConnection so CanConnectAsync throws, exercising the catch path.
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_WhenDbUnavailable_StillReturns200()
    {
        // Arrange — build a factory whose DbContext will fail CanConnectAsync.
        // Use a SQLite path whose directory does not exist → CanConnectAsync returns false.
        await using var brokenFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbDesc is not null) services.Remove(dbDesc);
                    services.AddDbContext<AppDbContext>(opt =>
                        opt.UseSqlite("Data Source=/spaceos_nonexistent_dir_99999/test.db"));

                    var trDesc = services.SingleOrDefault(d => d.ServiceType == typeof(ITenantResolver));
                    if (trDesc is not null) services.Remove(trDesc);
                    services.AddSingleton<ITenantResolver>(new NullTenantResolver());

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

                    // Stub IAuditWriteLock — Testing env is not Development.
                    services.AddScoped<IAuditWriteLock, HealthNoOpAuditWriteLock>();
                });
            });

        using var client = brokenFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz", TestContext.Current.CancellationToken);

        // Assert — 200 even when DB is unreachable (liveness probe)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponseWithDb>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("healthy", body.Status);
        Assert.Equal("unavailable", body.Db);
    }

    /// <summary>Deserialization contract for the basic /healthz response.</summary>
    private sealed record HealthResponse(string Status);

    /// <summary>Deserialization contract for the enhanced /healthz response (T4).</summary>
    private sealed record HealthResponseWithDb(string Status, string Db);
}

/// <summary>No-op stub for <see cref="IAuditWriteLock"/> used in the health endpoint broken-DB test.</summary>
file sealed class HealthNoOpAuditWriteLock : IAuditWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisposable.Instance);

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
