// SpaceOS.Kernel.Api.Tests/Infrastructure/ApiFactory.cs
using Ardalis.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Anomaly;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Sync;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using IAuditUnitOfWork = SpaceOS.Kernel.Domain.Repositories.IAuditUnitOfWork;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.UserProfiles;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Actors;
using SpaceOS.Modules.Abstractions.Crypto;
using SpaceOS.Modules.Abstractions.Sync;
using System.Security.Cryptography;

namespace SpaceOS.Kernel.Api.Tests.Infrastructure;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the production database
/// with an in-memory SQLite connection isolated per test class instance.
/// The connection is kept open for the lifetime of the factory to prevent EF Core
/// from discarding the in-memory schema between operations.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteConnection _auditConnection;

    /// <summary>The fixed <see cref="TenantId"/> used for all Api.Tests endpoint tests.</summary>
    public static readonly TenantId TestTenantId =
        TenantId.From(new Guid("20000000-0000-0000-0000-000000000002"));

    /// <summary>Initialises the factory and opens the shared in-memory SQLite connections.</summary>
    public ApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _auditConnection = new SqliteConnection("Data Source=:memory:");
        _auditConnection.Open();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Register DbContext on the shared open SQLite connection
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Remove the production AuditDbContext registration and replace with SQLite
            var auditDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuditDbContext>));
            if (auditDescriptor is not null)
                services.Remove(auditDescriptor);

            services.AddDbContext<AuditDbContext>(options =>
                options.UseSqlite(_auditConnection));

            // Stub IAuditUnitOfWork — wraps the test AuditDbContext.
            var auditUowDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAuditUnitOfWork));
            if (auditUowDescriptor is not null)
                services.Remove(auditUowDescriptor);

            services.AddScoped<IAuditUnitOfWork, NoOpAuditUnitOfWork>();

            // Replace the production ITenantResolver with a claims-reading stub
            // that extracts the tid claim from the JWT, falling back to TestTenantId.
            // This ensures EF Core tenant filters resolve correctly in write-path tests.
            var tenantResolverDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantResolver));
            if (tenantResolverDescriptor is not null)
                services.Remove(tenantResolverDescriptor);

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantResolver, ApiClaimsTenantResolver>();

            // Stub IAuditEventRepository until T3 registers the real implementation.
            services.AddScoped<IAuditEventRepository, NoOpAuditEventRepository>();

            // Stub IAuditWriteLock — no-op for API tests (audit chain not exercised here).
            services.AddScoped<IAuditWriteLock, NoOpAuditWriteLock>();

            // Stub IExternalAuditSink — no-op for API tests.
            services.AddSingleton<IExternalAuditSink, NoOpExternalAuditSink>();

            // Stub ISecretProvider — pass-through for API tests.
            services.AddScoped<ISecretProvider, NoOpSecretProvider>();

            // Stub IAggregateSnapshotRepository — no-op for API tests.
            services.AddScoped<IAggregateSnapshotRepository, NoOpAggregateSnapshotRepository>();

            // Stub IOutboxRepository — no-op for API tests.
            services.AddScoped<IOutboxRepository, NoOpOutboxRepository>();

            // Stub IImmutableStorage — no-op for API tests.
            services.AddSingleton<IImmutableStorage, NoOpImmutableStorage>();

            // Stub IGenesisHashProvider — returns a fixed test genesis hash.
            services.AddSingleton<IGenesisHashProvider, NoOpGenesisHashProvider>();

            // Stub IAlertService — discards alerts silently.
            services.AddSingleton<IAlertService, NoOpAlertService>();

            // Stub IUserProfileRepository — no-op for API tests (GDPR path not exercised here).
            services.AddScoped<IUserProfileRepository, NoOpUserProfileRepository>();

            // Stub IPseudonymizer — returns a fixed deterministic GUID for API tests.
            services.AddScoped<IPseudonymizer, NoOpPseudonymizer>();

            // Stub IHashProvider — delegates to SHA-256 so hash format assertions still pass.
            services.AddSingleton<IHashProvider, TestSha256HashProvider>();

            // Stub INodeManifestRepository — no-op for API tests (federation not exercised here).
            services.AddScoped<INodeManifestRepository, NoOpNodeManifestRepository>();

            // Stub ISyncSignalRepository — no-op for API tests (sync path not exercised here).
            services.AddScoped<ISyncSignalRepository, NoOpSyncSignalRepository>();

            // Stub ISyncSignalWriteLock — no-op for API tests.
            services.AddScoped<ISyncSignalWriteLock, NoOpSyncSignalWriteLock>();

            // Stub ITransactionManager — no-op for API tests (single-connection SQLite does not need explicit transactions).
            services.AddScoped<ITransactionManager, NoOpTransactionManager>();

            // Stub INodeUrlValidator — accepts all URLs in tests (SSRF guard not exercised here).
            services.AddSingleton<INodeUrlValidator, NoOpNodeUrlValidator>();

            // Stub IKeyVaultService — returns deterministic dev keys derived from constants.
            services.AddSingleton<IKeyVaultService, TestKeyVaultService>();

            // Stub IColumnEncryptionService — identity (pass-through) for API tests.
            services.AddSingleton<IColumnEncryptionService, NoOpColumnEncryptionService>();

            // Stub INodeAuthService — returns a fixed token and always reports valid.
            services.AddSingleton<INodeAuthService, NoOpNodeAuthService>();

            // Stub ISyncSignalHasher — returns a deterministic constant hash for API tests.
            services.AddSingleton<ISyncSignalHasher, NoOpSyncSignalHasher>();

            // Register AuditAnomalyDetector (scoped) — needed by DI but background worker is suppressed.
            services.AddScoped<AuditAnomalyDetector>();

            // Stub IRefreshTokenRepository — no-op for API tests (refresh token path not exercised here).
            services.AddScoped<IRefreshTokenRepository, NoOpRefreshTokenRepository>();

            // Stub IAuditEscrowWriter — no-op for API tests (WORM escrow path not exercised here).
            services.AddScoped<IAuditEscrowWriter, NoOpAuditEscrowWriter>();

            // Override JWT Bearer validation to accept RS256 test-signed tokens.
            // KC-T1: Authority is not set in tests — PostConfigure overrides the entire TVP
            // so JWKS discovery is bypassed and test-issued RS256 tokens are accepted directly.
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
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with an Admin Bearer JWT
    /// whose <c>tid</c> claim is set to <see cref="TestTenantId"/>.
    /// The <see cref="ApiClaimsTenantResolver"/> will resolve this claim so EF Core
    /// tenant filters apply correctly in write-path tests.
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

        // AuditDbContext has its own SQLite connection — create its schema.
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await auditDb.Database.EnsureCreatedAsync().ConfigureAwait(false);

        // DashboardStatsQuery uses raw SQL against AppDbContext to count AuditEvents.
        // In production both contexts share the same PG database, but in tests they're
        // separate SQLite instances.  Create a minimal AuditEvents stub table in AppDbContext
        // so the raw SQL doesn't fail.
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

    /// <inheritdoc/>
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

/// <summary>
/// No-op stub for <see cref="IAuditEventRepository"/> used in API tests until the
/// Infrastructure implementation is wired up in T3.
/// All writes are silently discarded; reads return empty collections.
/// </summary>
file sealed class NoOpAuditEventRepository : IAuditEventRepository
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
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
}

/// <summary>No-op stub for <see cref="IAuditWriteLock"/> used in API tests.</summary>
file sealed class NoOpAuditWriteLock : IAuditWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisposable.Instance);

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>No-op stub for <see cref="IExternalAuditSink"/> used in API tests.</summary>
file sealed class NoOpExternalAuditSink : IExternalAuditSink
{
    public Task WriteAsync(
        Guid tenantId, string eventType, string stateHash,
        string previousHash, DateTimeOffset occurredAt,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
        Guid tenantId, DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ExternalAuditHashRecord>>(Array.Empty<ExternalAuditHashRecord>());
}

/// <summary>Pass-through stub for <see cref="ISecretProvider"/> used in API tests.</summary>
file sealed class NoOpSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default) =>
        Task.FromResult<string?>(secretRef);
}

/// <summary>No-op stub for <see cref="IAggregateSnapshotRepository"/> used in API tests.</summary>
file sealed class NoOpAggregateSnapshotRepository : IAggregateSnapshotRepository
{
    public Task AddAsync(AggregateSnapshot snapshot, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<AggregateSnapshot?> GetLatestAsync(Guid aggregateId, CancellationToken ct = default) =>
        Task.FromResult<AggregateSnapshot?>(null);

    public Task<AggregateSnapshot?> GetAtTimestampAsync(Guid aggregateId, DateTimeOffset at, CancellationToken ct = default) =>
        Task.FromResult<AggregateSnapshot?>(null);

    public Task<IReadOnlyList<AggregateSnapshot>> ListByAggregateAsync(Guid aggregateId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AggregateSnapshot>>(Array.Empty<AggregateSnapshot>());
}

/// <summary>No-op stub for <see cref="IOutboxRepository"/> used in API tests.</summary>
file sealed class NoOpOutboxRepository : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>());

    public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>());

    public Task UpdateAsync(OutboxMessage message, CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>No-op stub for <see cref="IImmutableStorage"/> used in API tests.</summary>
file sealed class NoOpImmutableStorage : IImmutableStorage
{
    public Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default) =>
        Task.FromResult("0000000000000000000000000000000000000000000000000000000000000000");

    public Task<Stream> RetrieveAndVerifyAsync(string fileName, string expectedHash, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new MemoryStream());
}

/// <summary>Fixed genesis hash stub for <see cref="IGenesisHashProvider"/> used in API tests.</summary>
file sealed class NoOpGenesisHashProvider : IGenesisHashProvider
{
    public Task<string> GetGenesisHashAsync(CancellationToken ct = default) =>
        Task.FromResult("0000000000000000000000000000000000000000000000000000000000000000");
}

/// <summary>No-op stub for <see cref="IAlertService"/> used in API tests.</summary>
file sealed class NoOpAlertService : IAlertService
{
    public Task SendAlertAsync(string alertType, string message, CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>No-op stub for <see cref="IUserProfileRepository"/> used in API tests.</summary>
file sealed class NoOpUserProfileRepository : IUserProfileRepository
{
    public Task<UserProfile?> GetByExternalUserIdAsync(string externalUserId, Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<UserProfile?>(null);

    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<UserProfile?>(null);

    public Task AddAsync(UserProfile profile, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(UserProfile profile, CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>
/// Deterministic pseudonymizer stub for API tests.
/// Returns a fixed, reproducible GUID derived from the external user ID so tests
/// can assert on audit actor values without hitting the database.
/// </summary>
file sealed class NoOpPseudonymizer : IPseudonymizer
{
    public Task<Guid> GetOrCreatePseudonymAsync(string externalUserId, Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
}

/// <summary>SHA-256 hash provider stub for API tests — matches the production implementation.</summary>
file sealed class TestSha256HashProvider : IHashProvider
{
    public HashAlgorithmType AlgorithmType => HashAlgorithmType.SHA256;

    public string ComputeHash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }
}

/// <summary>No-op stub for <see cref="INodeManifestRepository"/> used in API tests.</summary>
file sealed class NoOpNodeManifestRepository : INodeManifestRepository
{
    public Task<NodeManifest?> GetByTenantIdAsync(TenantId tenantId, CancellationToken ct = default) =>
        Task.FromResult<NodeManifest?>(null);

    public Task AddAsync(NodeManifest manifest, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(NodeManifest manifest, CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>No-op stub for <see cref="ISyncSignalRepository"/> used in API tests.</summary>
file sealed class NoOpSyncSignalRepository : ISyncSignalRepository
{
    public Task<SyncSignal?> GetByClientSignalIdAsync(TenantId tenantId, Guid clientSignalId, CancellationToken ct = default) =>
        Task.FromResult<SyncSignal?>(null);

    public Task AddAsync(SyncSignal signal, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<string> GetLastHashAsync(TenantId tenantId, CancellationToken ct = default) =>
        Task.FromResult(SpaceOS.Kernel.Domain.Sync.SyncConstants.GenesisHash);
}

/// <summary>No-op stub for <see cref="ISyncSignalWriteLock"/> used in API tests.</summary>
file sealed class NoOpSyncSignalWriteLock : ISyncSignalWriteLock
{
    public Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisposable.Instance);

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>No-op stub for <see cref="ITransactionManager"/> used in API tests.</summary>
file sealed class NoOpTransactionManager : ITransactionManager
{
    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default) =>
        Task.FromResult<IAsyncDisposable>(NullDisposable.Instance);

    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>Pass-through stub for <see cref="INodeUrlValidator"/> — accepts all URLs in API tests.</summary>
file sealed class NoOpNodeUrlValidator : INodeUrlValidator
{
    public string? Validate(string serverUrl) => null;
}

/// <summary>
/// Deterministic key stub for <see cref="IKeyVaultService"/> used in API tests.
/// Returns 32-byte keys derived from well-known constants so that crypto services
/// can be constructed without a Key Vault connection.
/// </summary>
file sealed class TestKeyVaultService : IKeyVaultService
{
    private static readonly byte[] SigningKey =
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("spaceos-test-signing-key"));

    private static readonly byte[] EncryptionKey =
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("spaceos-test-encryption-key"));

    public Task<byte[]> GetSigningKeyAsync(CancellationToken ct = default) =>
        Task.FromResult(SigningKey);

    public Task<byte[]> GetEncryptionKeyAsync(CancellationToken ct = default) =>
        Task.FromResult(EncryptionKey);
}

/// <summary>Identity (pass-through) stub for <see cref="IColumnEncryptionService"/> used in API tests.</summary>
file sealed class NoOpColumnEncryptionService : IColumnEncryptionService
{
    public string Encrypt(string plaintext) => plaintext;
    public string Decrypt(string ciphertext) => ciphertext;
}

/// <summary>Fixed-response stub for <see cref="INodeAuthService"/> used in API tests.</summary>
file sealed class NoOpNodeAuthService : INodeAuthService
{
    public Task<string> IssueNodeJwtAsync(Guid tenantId, string nodeUrl, CancellationToken ct = default) =>
        Task.FromResult("test-node-jwt");

    public Task<bool> ValidateNodeJwtAsync(string token, CancellationToken ct = default) =>
        Task.FromResult(true);
}

/// <summary>
/// Deterministic no-op stub for <see cref="ISyncSignalHasher"/> used in API tests.
/// Returns a constant hex string so hash-chain assertions pass without a real HMAC key.
/// </summary>
file sealed class NoOpSyncSignalHasher : ISyncSignalHasher
{
    public string ComputeHash(string previousHash, string payloadJson, DateTimeOffset occurredAt) =>
        "0000000000000000000000000000000000000000000000000000000000000000";
}

/// <summary>No-op stub for <see cref="IAuditUnitOfWork"/> used in API tests.</summary>
file sealed class NoOpAuditUnitOfWork : IAuditUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>No-op stub for <see cref="IAuditEscrowWriter"/> used in API tests.
/// WORM escrow writes are silently discarded — MinIO is not available in the test host.</summary>
file sealed class NoOpAuditEscrowWriter : IAuditEscrowWriter
{
    public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>No-op stub for <see cref="IRefreshTokenRepository"/> used in API tests.</summary>
file sealed class NoOpRefreshTokenRepository : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult<RefreshToken?>(null);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult<RefreshToken?>(null);

    public Task AddAsync(RefreshToken token, CancellationToken ct) => Task.CompletedTask;

    public Task UpdateAsync(RefreshToken token, CancellationToken ct) => Task.CompletedTask;
}

