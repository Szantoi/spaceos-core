// SpaceOS.Kernel.Api.Tests/Endpoints/OpenApiTests.cs
using System.Net;
using System.Net.Http.Headers;
using Ardalis.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the OpenAPI / Swagger endpoints (T4).</summary>
public sealed class OpenApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;

    /// <summary>Initialises a Development-environment factory for Swagger endpoint tests.</summary>
    public OpenApiTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override environment to Development so Swagger is enabled
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(_connection));

                    // KC-T1: Override JWT Bearer validation to accept RS256 test-signed tokens.
                    // Authority is not set in tests — PostConfigure overrides the entire TVP
                    // so JWKS discovery is bypassed and test-issued tokens are accepted directly.
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

                    // Stub IAuditEventRepository until T3 registers the real implementation.
                    services.AddScoped<IAuditEventRepository, NoOpAuditEventRepositoryForOpenApi>();
                });
            });

        // Set Development environment via environment variable before creating client
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.ForRole("Admin"));

        // Ensure schema exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    /// <summary>Swagger UI must be accessible at /swagger in Development environment.</summary>
    [Fact]
    public async Task SwaggerEndpoint_Returns200()
    {
        // Act — Swashbuckle redirects /swagger → /swagger/index.html; follow with AllowAutoRedirect (default)
        var response = await _client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>OpenAPI JSON must be at /openapi/v1.json and document the Tenants path.</summary>
    [Fact]
    public async Task OpenApiJson_Returns200WithPaths()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("/api/tenants", json, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// No-op stub for <see cref="IAuditEventRepository"/> used in OpenAPI tests until the
/// Infrastructure implementation is wired up in T3.
/// </summary>
file sealed class NoOpAuditEventRepositoryForOpenApi : IAuditEventRepository
{
    public Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<AuditEvent>> ListAsync(
        ISpecification<AuditEvent> specification,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());

    public Task<int> CountAsync(
        ISpecification<AuditEvent> specification,
        CancellationToken ct = default) =>
        Task.FromResult(0);

    public Task<string> GetLastHashAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult("GENESIS");

    public Task<IReadOnlyList<AuditEvent>> GetChainAsync(
        Guid tenantId, DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
}
