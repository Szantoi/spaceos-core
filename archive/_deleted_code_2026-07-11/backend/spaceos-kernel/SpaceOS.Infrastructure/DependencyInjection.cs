// SpaceOS.Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using SpaceOS.Infrastructure.Health;
using SpaceOS.Infrastructure.Alerting;
using SpaceOS.Infrastructure.Internal;
using SpaceOS.Infrastructure.AuditLog;
using SpaceOS.Infrastructure.Auth;
using SpaceOS.Infrastructure.Common;
using SpaceOS.Infrastructure.Crypto;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Infrastructure.Data.Queries;
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Infrastructure.Extensions;
using SpaceOS.Infrastructure.Outbox;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Infrastructure.Storage;
using SpaceOS.Infrastructure.Sync;
using SpaceOS.Infrastructure.Validation;
using SpaceOS.Kernel.Application.AuditLog.Anomaly;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Sync;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Dashboard;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.UserProfiles;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Modules.Abstractions.Actors;
using SpaceOS.Modules.Abstractions.Crypto;
using SpaceOS.Modules.Abstractions.Sync;

namespace SpaceOS.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services with the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core database context, unit of work, and all repository implementations.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration used to resolve the connection string.</param>
    /// <param name="environment">The host environment used to select the database provider.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // BE-P15-10: Startup fail-fast — a missing DefaultConnection is caught at application boot,
        // not at the first DB call. AuditWriter and AuditSink are optional in Development.
        services.AddOptions<ConnectionStringOptions>()
            .BindConfiguration("ConnectionStrings")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        // AuditWriter connection uses the restricted spaceos_audit_writer PostgreSQL role.
        // Falls back to the default connection string in Development (SQLite, single file).
        var auditConnectionString = configuration.GetConnectionString("AuditWriter")
            ?? connectionString;

        // AuditSink connection targets the spaceos_audit_sink database (spaceos_sink_writer role).
        // Only used in production — Development uses FileExternalAuditSink instead.
        var auditSinkConnectionString = configuration.GetConnectionString("AuditSink");

        // BE-P15-03: TenantSessionInterceptor registered as Singleton (production only).
        // SQLite (Development/Testing) does not support set_config — interceptor must not be registered there.
        if (!environment.IsDevelopment())
        {
            services.AddSingleton<TenantSessionInterceptor>();
        }

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            if (environment.IsDevelopment())
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                // EnableRetryOnFailure is intentionally omitted for AppDbContext.
                // Several code paths (EfTransactionManager, CreateStageHandoffCommandHandler)
                // use explicit user-initiated transactions. NpgsqlRetryingExecutionStrategy
                // calls OnFirstExecution() on every EF Core operation, which throws when a
                // user transaction is active — regardless of whether the operation itself would fail.
                // Removing the retry strategy from contexts that own explicit transactions is the
                // correct approach; retry at the application/HTTP client level is the fallback.
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly("SpaceOS.Infrastructure"));
                options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
            }
        });

        // AuditDbContext: isolated context for AuditEvents using the restricted DB role.
        services.AddDbContext<AuditDbContext>((sp, options) =>
        {
            if (environment.IsDevelopment())
            {
                options.UseSqlite(auditConnectionString);
            }
            else
            {
                // EnableRetryOnFailure is intentionally omitted — same reason as AppDbContext.
                // PostgresAdvisoryAuditWriteLock holds a user-initiated transaction open while
                // the audit hash chain is computed and written. Any EF Core operation on
                // AuditDbContext while that transaction is live would hit OnFirstExecution()
                // and throw if a retry strategy is configured.
                options.UseNpgsql(
                    auditConnectionString,
                    npgsql => npgsql.MigrationsAssembly("SpaceOS.Infrastructure"));
                options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
            }
        });

        // BE-P15-09: HashSinkDbContext uses AddDbContextFactory (not AddDbContext) so that
        // PostgresHashSink can create short-lived, immediately disposed contexts for each
        // fire-and-forget sink write — eliminating the Scoped-lifetime disposal risk.
        // Development: no sink DB — PostgresHashSink is not registered there.
        // Production: connects to spaceos_audit_sink with the spaceos_sink_writer role.
        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(auditSinkConnectionString))
        {
            services.AddDbContextFactory<HashSinkDbContext>(options =>
                options.UseNpgsql(
                    auditSinkConnectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly("SpaceOS.Infrastructure");
                        npgsql.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    }));
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditUnitOfWork, AuditUnitOfWork>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantResolver, ClaimsTenantResolver>();
        services.AddScoped<ICurrentRequestContext, HttpContextCurrentRequestContext>();
        services.AddSingleton<ITenantConnectionResolver, SharedTenantConnectionResolver>();

        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IWorkStationRepository, WorkStationRepository>();
        services.AddScoped<IFlowEpicRepository, FlowEpicRepository>();
        services.AddScoped<ISpaceLayerRepository, SpaceLayerRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IDashboardStatsQuery, DashboardStatsQuery>();
        services.AddScoped<IAggregateSnapshotRepository, AggregateSnapshotRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IModuleSubscriptionRepository, ModuleSubscriptionRepository>();
        services.AddScoped<ICrossModuleOutboxDispatcher, CrossModuleOutboxDispatcher>();

        // Named HTTP client for cross-module internal communication.
        // In production this will be configured with mTLS client certificate.
        services.AddHttpClient("cross-module");

        // Phase 4 Track B stubs — replaced by real implementations when SignalR fan-out
        // and hash-chain audit sink are provisioned (Cutting Phase 4 prerequisite).
        services.AddScoped<ISignalROutboxFanOut, NullSignalROutboxFanOut>();
        services.AddScoped<IHashChainOutboxSink, NullHashChainOutboxSink>();
        services.AddHostedService<OutboxBackgroundWorker>();
        services.AddHostedService<OfflineQueuePurgeWorker>();

        services.AddScoped<INodeManifestRepository, NodeManifestRepository>();
        services.AddScoped<ISyncSignalRepository, SyncSignalRepository>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPhysicalSpaceRepository, PhysicalSpaceRepository>();
        services.AddScoped<IBvhRepository, BvhRepository>();
        services.AddScoped<ISpatialElementRepository, SpatialElementRepository>();
        services.AddScoped<ISpatialTaskLinkRepository, SpatialTaskLinkRepository>();
        services.AddScoped<ISpatialQueryRepository, SpatialQueryRepository>();
        services.AddScoped<IFlowTaskLookup, FlowTaskLookup>();
        services.AddScoped<ITenantHandshakeAllowlistRepository, TenantHandshakeAllowlistRepository>();

        // ADR-039: Internal actor directory — SEC-S-09
        services.AddScoped<IB2BHandshakeVerifier, B2BHandshakeVerifier>();
        services.AddScoped<IInternalAccessAuditWriter, InternalAccessAuditWriter>();

        // Stage Registry (ADR-022)
        services.AddScoped<IStageDefinitionRepository, StageDefinitionRepository>();
        services.AddScoped<IStageChainTemplateRepository, StageChainTemplateRepository>();
        services.AddScoped<IStageHandoffRepository, StageHandoffRepository>();
        services.AddScoped<IStageChainValidator, StageChainValidator>();

        // ADR-018/ADR-019: ModuleRegistry — singleton, stateless, no infra deps.
        services.AddSingleton<IModuleRegistryService, ModuleRegistryService>();

        // AuditAnomalyDetector is scoped — it uses IAuditEventRepository which is also scoped.
        services.AddScoped<AuditAnomalyDetector>();
        services.AddHostedService<AnomalyDetectionBackgroundWorker>();

        // Named HTTP client used by WebhookAlertService in production.
        services.AddHttpClient(nameof(WebhookAlertService));

        // S-01: SSRF prevention — singleton, stateless validator.
        services.AddSingleton<INodeUrlValidator, NodeUrlValidator>();

        // S-02 / S-05 / S-08: Crypto and auth services must be environment-gated.
        // Dev: deterministic keys from config (ConfigKeyVaultService, DevRsaKeyManager).
        // Prod: must throw if keys are not explicitly configured — never fall back to dev constants.
        services.AddSingleton<IColumnEncryptionService, AesGcmColumnEncryptionService>();
        services.AddSingleton<ISyncSignalHasher, SyncSignalHasher>();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IKeyVaultService, ConfigKeyVaultService>();
            services.AddSingleton<INodeAuthService, NodeAuthService>();
            services.AddScoped<IAuditWriteLock, InProcessAuditWriteLock>();
            services.AddScoped<ISyncSignalWriteLock, InProcessSyncSignalWriteLock>();
            services.AddSingleton<IRsaPublicKeyProvider, PemFileRsaPublicKeyProvider>();
            services.AddSingleton<IExternalAuditSink, FileExternalAuditSink>();
            services.AddScoped<ISecretProvider, InMemorySecretProvider>();
            services.AddSingleton<IImmutableStorage, FileImmutableStorage>();
            services.AddSingleton<IGenesisHashProvider, ConfigurationGenesisHashProvider>();
            services.AddSingleton<IAlertService, LoggingAlertService>();
            // KERNEL-088: No MinIO in Development — use no-op escrow writer.
            services.AddScoped<IAuditEscrowWriter, NullAuditEscrowWriter>();
        }
        else
        {
            // Production: ConfigKeyVaultService IS registered here but is self-guarding —
            // it throws InvalidOperationException at runtime if called outside a Development
            // environment without explicit Azure Key Vault configuration present.
            // NodeAuthService similarly requires valid RSA key material from Key Vault.
            // Neither service falls back to hardcoded dev constants in non-Development envs.
            services.AddSingleton<IKeyVaultService, ConfigKeyVaultService>();
            services.AddSingleton<INodeAuthService, NodeAuthService>();
            services.AddScoped<IAuditWriteLock, PostgresAdvisoryAuditWriteLock>();
            services.AddScoped<ISyncSignalWriteLock, PostgresAdvisorySyncSignalWriteLock>();
            services.AddSingleton<IRsaPublicKeyProvider, AzureKeyVaultRsaPublicKeyProvider>();
            // PostgresHashSink requires HashSinkDbContext factory which is only registered
            // when AuditSink connection string is present. Fall back to file sink otherwise.
            if (!string.IsNullOrWhiteSpace(auditSinkConnectionString))
            {
                services.AddSingleton<IExternalAuditSink, PostgresHashSink>();
            }
            else
            {
                services.AddSingleton<IExternalAuditSink, FileExternalAuditSink>();
            }
            services.AddScoped<ISecretProvider, KeyVaultSecretProvider>();
            services.AddSingleton<IImmutableStorage, AzureImmutableBlobStorage>();
            services.AddSingleton<IGenesisHashProvider, KeyVaultGenesisHashProvider>();
            services.AddSingleton<IAlertService, WebhookAlertService>();
            // KERNEL-088: WORM audit escrow — MinIO Object Lock COMPLIANCE (365 days).
            // Only register the live MinIO client when Enabled=true AND credentials are configured.
            // An unconfigured/disabled escrow falls back to NullAuditEscrowWriter so the
            // MinioClient.Build() validation (which rejects empty credentials) never runs.
            services.Configure<MinioEscrowOptions>(
                configuration.GetSection(MinioEscrowOptions.SectionName));
            var escrowCfg = configuration
                .GetSection(MinioEscrowOptions.SectionName)
                .Get<MinioEscrowOptions>() ?? new MinioEscrowOptions();
            if (escrowCfg.Enabled
                && !string.IsNullOrWhiteSpace(escrowCfg.AccessKey)
                && !string.IsNullOrWhiteSpace(escrowCfg.SecretKey))
            {
                services.AddSingleton<IMinioClient>(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<MinioEscrowOptions>>().Value;
                    return new MinioClient()
                        .WithEndpoint(opts.Endpoint)
                        .WithCredentials(opts.AccessKey, opts.SecretKey)
                        .Build();
                });
                services.AddSingleton<IMinioStorage, MinioStorageAdapter>();
                services.AddScoped<IAuditEscrowWriter, MinioAuditEscrowWriter>();
            }
            else
            {
                services.AddScoped<IAuditEscrowWriter, NullAuditEscrowWriter>();
            }
        }

        // BE-P2-01: IConnectionMultiplexer singleton + IDistributedCache (Redis or in-memory fallback).
        // startupLogger not available here — warnings will be emitted at connect time if Redis is absent.
        services.AddSpaceOsRedis(configuration);

        // BE-P3B-04: Proof storage service — environment-gated.
        // Dev: local file system storage (no cloud dependency needed for local development).
        // Prod: S3 WORM storage (stub — full SDK integration provisioned separately).
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IProofStorageService, LocalProofStorageService>();
        }
        else
        {
            services.AddSingleton<IProofStorageService, S3WormProofStorageService>();
        }

        // Track C (SEC-03 / SEC-07): PostgreSQL WORM audit hash sink.
        // Only registered when AUDIT_SINK_CONNECTION_STRING env var is set.
        // The connection string must use the spaceos_audit_worm role (INSERT-only on AuditHashes).
        var auditSinkWormConnectionString = configuration["AUDIT_SINK_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(auditSinkWormConnectionString))
        {
            services.AddScoped<IWormStorageService, PostgresWormStorageService>();
        }

        return services;
    }

    /// <summary>
    /// Registers SpaceOS health checks — including the JWKS endpoint probe — with the DI container.
    /// Call this from the API host to keep <see cref="JwksHealthCheck"/> <c>internal</c> to the
    /// Infrastructure assembly while still being accessible to the host builder (I2 compliance).
    /// </summary>
    /// <param name="builder">The health checks builder returned by <c>services.AddHealthChecks()</c>.</param>
    /// <returns>The same <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    public static IHealthChecksBuilder AddKernelHealthChecks(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<JwksHealthCheck>("jwks", tags: ["ready"]);
        return builder;
    }
}
