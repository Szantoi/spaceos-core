// SpaceOS.Kernel.Api.Tests/Endpoints/InternalDeleteByTenantEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using Ardalis.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Anomaly;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Sync;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.UserProfiles;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Actors;
using SpaceOS.Modules.Abstractions.Crypto;
using SpaceOS.Modules.Abstractions.Sync;
using Xunit;
using IAuditUnitOfWork = SpaceOS.Kernel.Domain.Repositories.IAuditUnitOfWork;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>DELETE /internal/flow-epics/by-tenant/{tenantId}</c> (BE-TEST-02).
/// Validates the three defense-in-depth gates: X-SpaceOS-Internal header, confirm=true
/// query parameter, and TEST_TENANT_ALLOWLIST environment variable check.
/// </summary>
public sealed class InternalDeleteByTenantEndpointTests : IAsyncLifetime
{
    private static readonly string TestTenantGuid = ApiFactory.TestTenantId.Value.ToString();

    private readonly InternalTestFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory with TEST_TENANT_ALLOWLIST configured to include the test tenant.</summary>
    public InternalDeleteByTenantEndpointTests()
    {
        _factory = new InternalTestFactory(TestTenantGuid);
        _client = _factory.CreateClient();
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

    // -------------------------------------------------------------------------
    // Success path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a well-formed DELETE request with all three security gates satisfied
    /// returns 200 with the correct deletedCounts response shape.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_ValidRequest_Returns200WithCounts()
    {
        // Arrange — seed FlowEpics + Snapshot for the test tenant
        var tenant = Tenant.Create("Reset Tenant");
        var facility = Facility.Create("Reset Facility", ApiFactory.TestTenantId);
        var epic1 = FlowEpic.Create("Epic One", facility.Id, ApiFactory.TestTenantId);
        var epic2 = FlowEpic.Create("Epic Two", facility.Id, ApiFactory.TestTenantId);
        var snapshot = AggregateSnapshot.Create(
            epic1.Id.Value, "FlowEpic", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), "{}", "abc123", ApiFactory.TestTenantId.Value);

        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.FlowEpics.AddRange(epic1, epic2);
            db.AggregateSnapshots.Add(snapshot);
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/flow-epics/by-tenant/{TestTenantGuid}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeleteResponse>(
            TestJsonOptions.Default, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(body);
        Assert.Equal(TestTenantGuid, body.TenantId);
        Assert.NotNull(body.DeletedCounts);
        Assert.Equal(2, body.DeletedCounts.FlowEpics);
        Assert.Equal(1, body.DeletedCounts.Snapshots);
    }

    // -------------------------------------------------------------------------
    // Gate 1: Missing X-SpaceOS-Internal header
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a request without the X-SpaceOS-Internal header returns 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_MissingInternalHeader_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/flow-epics/by-tenant/{TestTenantGuid}?confirm=true");
        // No X-SpaceOS-Internal header

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Gate 2: Missing confirm=true
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a request without confirm=true returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_MissingConfirmParam_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/flow-epics/by-tenant/{TestTenantGuid}");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Gate 3: Tenant not in allowlist
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a request for a tenant not in TEST_TENANT_ALLOWLIST returns 403.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_TenantNotInAllowlist_Returns403()
    {
        var unknownTenantId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/flow-epics/by-tenant/{unknownTenantId}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Gate 4: Invalid GUID format
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a request with a malformed tenantId returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_InvalidGuid_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "/internal/flow-epics/by-tenant/not-a-guid?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Edge: Empty tenant — no data to delete still returns 200 with zero counts
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that deleting data for a tenant with no FlowEpics returns 200
    /// with all counts equal to zero.
    /// </summary>
    [Fact]
    public async Task DeleteByTenant_NoData_Returns200WithZeroCounts()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/flow-epics/by-tenant/{TestTenantGuid}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeleteResponse>(
            TestJsonOptions.Default, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(body);
        Assert.Equal(0, body.DeletedCounts.FlowEpics);
        Assert.Equal(0, body.DeletedCounts.Snapshots);
    }

    // -------------------------------------------------------------------------
    // Response DTOs for deserialization
    // -------------------------------------------------------------------------

    private sealed record DeleteResponse(string TenantId, DeletedCountsDto DeletedCounts);
    private sealed record DeletedCountsDto(int FlowEpics, int Snapshots, int AuditEvents);
}

// -------------------------------------------------------------------------
// Test factory with TEST_TENANT_ALLOWLIST configuration
// -------------------------------------------------------------------------

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that sets up an in-memory SQLite database
/// and injects the <c>TEST_TENANT_ALLOWLIST</c> configuration key required by the internal
/// delete endpoint (BE-TEST-02).
/// </summary>
internal sealed class InternalTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteConnection _auditConnection;
    private readonly string _allowedTenantId;

    public InternalTestFactory(string allowedTenantId)
    {
        _allowedTenantId = allowedTenantId;
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _auditConnection = new SqliteConnection("Data Source=:memory:");
        _auditConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TEST_TENANT_ALLOWLIST"] = _allowedTenantId,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace AppDbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Replace AuditDbContext
            var auditDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuditDbContext>));
            if (auditDescriptor is not null)
                services.Remove(auditDescriptor);
            services.AddDbContext<AuditDbContext>(options => options.UseSqlite(_auditConnection));

            // Stub IAuditUnitOfWork
            ReplaceService<IAuditUnitOfWork, StubAuditUnitOfWork>(services);

            // Replace ITenantResolver with null (admin bypass — sees all data)
            ReplaceService<ITenantResolver, StubNullTenantResolver>(services);
            services.AddHttpContextAccessor();

            // Stub remaining services (same set as ApiFactory)
            services.AddScoped<IAuditEventRepository, StubAuditEventRepository>();
            services.AddScoped<IAuditWriteLock, StubAuditWriteLock>();
            services.AddSingleton<IExternalAuditSink, StubExternalAuditSink>();
            services.AddScoped<ISecretProvider, StubSecretProvider>();
            services.AddScoped<IAggregateSnapshotRepository, StubAggregateSnapshotRepository>();
            services.AddScoped<IOutboxRepository, StubOutboxRepository>();
            services.AddSingleton<IImmutableStorage, StubImmutableStorage>();
            services.AddSingleton<IGenesisHashProvider, StubGenesisHashProvider>();
            services.AddSingleton<IAlertService, StubAlertService>();
            services.AddScoped<IUserProfileRepository, StubUserProfileRepository>();
            services.AddScoped<IPseudonymizer, StubPseudonymizer>();
            services.AddSingleton<IHashProvider, StubSha256HashProvider>();
            services.AddScoped<INodeManifestRepository, StubNodeManifestRepository>();
            services.AddScoped<ISyncSignalRepository, StubSyncSignalRepository>();
            services.AddScoped<ISyncSignalWriteLock, StubSyncSignalWriteLock>();
            services.AddScoped<ITransactionManager, StubTransactionManager>();
            services.AddSingleton<INodeUrlValidator, StubNodeUrlValidator>();
            services.AddSingleton<IKeyVaultService, StubKeyVaultService>();
            services.AddSingleton<IColumnEncryptionService, StubColumnEncryptionService>();
            services.AddSingleton<INodeAuthService, StubNodeAuthService>();
            services.AddSingleton<ISyncSignalHasher, StubSyncSignalHasher>();
            services.AddScoped<AuditAnomalyDetector>();
            services.AddScoped<IRefreshTokenRepository, StubRefreshTokenRepository>();

            // Override JWT Bearer validation
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(JwtTestHelper.TestRsa),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task SeedAsync(Func<AppDbContext, Task>? seed = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await auditDb.Database.EnsureCreatedAsync().ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AuditEvents" (
                "Id" TEXT NOT NULL PRIMARY KEY
            )
            """).ConfigureAwait(false);

        if (seed is not null)
        {
            await seed(db).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private static void ReplaceService<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var desc = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
        if (desc is not null)
            services.Remove(desc);
        services.AddScoped<TService, TImplementation>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            _auditConnection.Dispose();
        }
    }
}

// -------------------------------------------------------------------------
// Minimal stubs (file-scoped)
// -------------------------------------------------------------------------

file sealed class StubNullTenantResolver : ITenantResolver
{
    public TenantId? TryResolve() => null;
}

file sealed class StubAuditUnitOfWork : IAuditUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubAuditEventRepository : IAuditEventRepository
{
    public Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<AuditEvent>> ListAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    public Task<int> CountAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default) => Task.FromResult(0);
    public Task<string> GetLastHashAsync(Guid tenantId, CancellationToken ct = default) => Task.FromResult("GENESIS");
    public Task<IReadOnlyList<AuditEvent>> GetChainAsync(Guid tenantId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>([]);
}

file sealed class StubAuditWriteLock : IAuditWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisp.Instance);
}

file sealed class StubExternalAuditSink : IExternalAuditSink
{
    public Task WriteAsync(Guid tenantId, string eventType, string stateHash, string previousHash, DateTimeOffset occurredAt, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(Guid tenantId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ExternalAuditHashRecord>>([]);
}

file sealed class StubSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default) => Task.FromResult<string?>(secretRef);
}

file sealed class StubAggregateSnapshotRepository : IAggregateSnapshotRepository
{
    public Task AddAsync(AggregateSnapshot snapshot, CancellationToken ct = default) => Task.CompletedTask;
    public Task<AggregateSnapshot?> GetLatestAsync(Guid aggregateId, CancellationToken ct = default) => Task.FromResult<AggregateSnapshot?>(null);
    public Task<AggregateSnapshot?> GetAtTimestampAsync(Guid aggregateId, DateTimeOffset at, CancellationToken ct = default) => Task.FromResult<AggregateSnapshot?>(null);
    public Task<IReadOnlyList<AggregateSnapshot>> ListByAggregateAsync(Guid aggregateId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AggregateSnapshot>>([]);
}

file sealed class StubOutboxRepository : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
    public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
    public Task UpdateAsync(OutboxMessage message, CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubImmutableStorage : IImmutableStorage
{
    public Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default) => Task.FromResult("0".PadLeft(64, '0'));
    public Task<Stream> RetrieveAndVerifyAsync(string fileName, string expectedHash, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
}

file sealed class StubGenesisHashProvider : IGenesisHashProvider
{
    public Task<string> GetGenesisHashAsync(CancellationToken ct = default) => Task.FromResult("0".PadLeft(64, '0'));
}

file sealed class StubAlertService : IAlertService
{
    public Task SendAlertAsync(string alertType, string message, CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubUserProfileRepository : IUserProfileRepository
{
    public Task<UserProfile?> GetByExternalUserIdAsync(string externalUserId, Guid tenantId, CancellationToken ct = default) => Task.FromResult<UserProfile?>(null);
    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserProfile?>(null);
    public Task AddAsync(UserProfile profile, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(UserProfile profile, CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubPseudonymizer : IPseudonymizer
{
    public Task<Guid> GetOrCreatePseudonymAsync(string externalUserId, Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
}

file sealed class StubSha256HashProvider : IHashProvider
{
    public HashAlgorithmType AlgorithmType => HashAlgorithmType.SHA256;
    public string ComputeHash(string input) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}

file sealed class StubNodeManifestRepository : INodeManifestRepository
{
    public Task<NodeManifest?> GetByTenantIdAsync(TenantId tenantId, CancellationToken ct = default) => Task.FromResult<NodeManifest?>(null);
    public Task AddAsync(NodeManifest manifest, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(NodeManifest manifest, CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubSyncSignalRepository : ISyncSignalRepository
{
    public Task<SyncSignal?> GetByClientSignalIdAsync(TenantId tenantId, Guid clientSignalId, CancellationToken ct = default) => Task.FromResult<SyncSignal?>(null);
    public Task AddAsync(SyncSignal signal, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> GetLastHashAsync(TenantId tenantId, CancellationToken ct = default) => Task.FromResult(SpaceOS.Kernel.Domain.Sync.SyncConstants.GenesisHash);
}

file sealed class StubSyncSignalWriteLock : ISyncSignalWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisp.Instance);
}

file sealed class StubTransactionManager : ITransactionManager
{
    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisp.Instance);
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubNodeUrlValidator : INodeUrlValidator
{
    public string? Validate(string serverUrl) => null;
}

file sealed class StubKeyVaultService : IKeyVaultService
{
    private static readonly byte[] SigningKey = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("spaceos-test-signing-key"));
    private static readonly byte[] EncryptionKey = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("spaceos-test-encryption-key"));
    public Task<byte[]> GetSigningKeyAsync(CancellationToken ct = default) => Task.FromResult(SigningKey);
    public Task<byte[]> GetEncryptionKeyAsync(CancellationToken ct = default) => Task.FromResult(EncryptionKey);
}

file sealed class StubColumnEncryptionService : IColumnEncryptionService
{
    public string Encrypt(string plaintext) => plaintext;
    public string Decrypt(string ciphertext) => ciphertext;
}

file sealed class StubNodeAuthService : INodeAuthService
{
    public Task<string> IssueNodeJwtAsync(Guid tenantId, string nodeUrl, CancellationToken ct = default) => Task.FromResult("test-node-jwt");
    public Task<bool> ValidateNodeJwtAsync(string token, CancellationToken ct = default) => Task.FromResult(true);
}

file sealed class StubSyncSignalHasher : ISyncSignalHasher
{
    public string ComputeHash(string previousHash, string payloadJson, DateTimeOffset occurredAt) => "0".PadLeft(64, '0');
}

file sealed class StubRefreshTokenRepository : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct) => Task.FromResult<RefreshToken?>(null);
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) => Task.FromResult<RefreshToken?>(null);
    public Task AddAsync(RefreshToken token, CancellationToken ct) => Task.CompletedTask;
    public Task UpdateAsync(RefreshToken token, CancellationToken ct) => Task.CompletedTask;
}

file sealed class NullDisp : IAsyncDisposable
{
    internal static readonly NullDisp Instance = new();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
