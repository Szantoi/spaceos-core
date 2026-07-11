// SpaceOS.Kernel.Application/AuditLog/AuditEventDispatcher.cs

using System.Reflection;
using System.Text.Json;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;


namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Application-layer implementation of <see cref="IAuditEventDispatcher"/>.
/// For each domain event the dispatcher:
/// <list type="number">
///   <item>Serialises it to canonical JSON (camelCase, no indentation).</item>
///   <item>Computes the hash of the UTF-8 encoded JSON via <see cref="IHashProvider"/>.</item>
///   <item>Pseudonymizes the caller's identity via <see cref="IPseudonymizer"/> (GDPR).</item>
///   <item>Extracts <c>AggregateId</c> and <c>TenantId</c> via reflection.</item>
///   <item>Persists an <see cref="AuditEvent"/> via <see cref="IAuditEventRepository"/>.</item>
/// </list>
/// </summary>
public sealed class AuditEventDispatcher : IAuditEventDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented        = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Ordered list of property names used to locate the primary aggregate identifier.</summary>
    private static readonly string[] AggregateIdCandidates =
    [
        "TenantId",
        "FacilityId",
        "WorkStationId",
        "SpaceLayerId",
        "FlowEpicId",
        "AuditEventId",
    ];

    private readonly IAuditEventRepository _repository;
    private readonly IAuditUnitOfWork _unitOfWork;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IAuditWriteLock _writeLock;
    private readonly IExternalAuditSink _sink;
    private readonly IGenesisHashProvider _genesisHashProvider;
    private readonly IHashProvider _hashProvider;
    private readonly IPseudonymizer _pseudonymizer;
    private readonly IAuditEscrowWriter _escrowWriter;

    /// <summary>
    /// Initialises a new <see cref="AuditEventDispatcher"/>.
    /// </summary>
    /// <param name="repository">The write-only repository for <see cref="AuditEvent"/> aggregates.</param>
    /// <param name="unitOfWork">The audit-scoped unit of work for persisting audit entries on the isolated <c>AuditDbContext</c>.</param>
    /// <param name="requestContext">Provides the actor identity and source IP for the current request.</param>
    /// <param name="writeLock">Serializes concurrent appends to the per-tenant hash chain.</param>
    /// <param name="sink">The external audit sink. Failures are swallowed and must not block the primary write.</param>
    /// <param name="genesisHashProvider">Provides the deployment-time genesis hash anchoring the audit chain.</param>
    /// <param name="hashProvider">Computes state hashes for each domain event payload.</param>
    /// <param name="pseudonymizer">Resolves a GDPR-erasable pseudonym for the request actor.</param>
    /// <param name="escrowWriter">
    /// WORM escrow writer (KERNEL-088). Writes each committed event to MinIO Object Lock storage
    /// after the primary DB commit. Failures are fire-and-forget — never block the primary write.
    /// </param>
    public AuditEventDispatcher(
        IAuditEventRepository repository,
        IAuditUnitOfWork unitOfWork,
        ICurrentRequestContext requestContext,
        IAuditWriteLock writeLock,
        IExternalAuditSink sink,
        IGenesisHashProvider genesisHashProvider,
        IHashProvider hashProvider,
        IPseudonymizer pseudonymizer,
        IAuditEscrowWriter escrowWriter)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(writeLock);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(genesisHashProvider);
        ArgumentNullException.ThrowIfNull(hashProvider);
        ArgumentNullException.ThrowIfNull(pseudonymizer);
        ArgumentNullException.ThrowIfNull(escrowWriter);
        _repository          = repository;
        _unitOfWork          = unitOfWork;
        _requestContext      = requestContext;
        _writeLock           = writeLock;
        _sink                = sink;
        _genesisHashProvider = genesisHashProvider;
        _hashProvider        = hashProvider;
        _pseudonymizer       = pseudonymizer;
        _escrowWriter        = escrowWriter;
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0)
            return;

        // Group by tenant so we acquire one lock per tenant per dispatch batch.
        var byTenant = events
            .Select(e => (Event: e, TenantId: ExtractTenantId(e)))
            .GroupBy(x => x.TenantId);

        foreach (var tenantGroup in byTenant)
        {
            var tenantId = tenantGroup.Key;
            await using var _ = await _writeLock.AcquireAsync(tenantId, ct).ConfigureAwait(false);

            var previousHash = await _repository.GetLastHashAsync(tenantId, ct).ConfigureAwait(false);

            // When no events exist yet the repository returns "GENESIS" as a sentinel.
            // Replace that sentinel with the deployment-time genesis hash so the chain
            // is anchored to a value set at provisioning time, not a magic string.
            if (previousHash == "GENESIS")
                previousHash = await _genesisHashProvider.GetGenesisHashAsync(ct).ConfigureAwait(false);

            // Pseudonymize the actor once per tenant group — the same actor identity applies
            // to all events in a single dispatch batch.
            string? pseudonymizedActorId = null;
            if (_requestContext.ActorId is not null)
            {
                var pseudonym = await _pseudonymizer
                    .GetOrCreatePseudonymAsync(_requestContext.ActorId, tenantId, ct)
                    .ConfigureAwait(false);
                pseudonymizedActorId = pseudonym.ToString();
            }

            var sourceBrand = _requestContext.SourceBrand;

            var batchEvents = new List<AuditEvent>();

            foreach (var (domainEvent, _) in tenantGroup)
            {
                var json      = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);

                // Hash chain input: previousHash:payloadJson:occurredAtISO8601:(sourceBrand ?? "")
                // SECURITY: The ":" separator is safe because AllowedBrands values never contain ":".
                // If a future brand value contains ":", the chain input becomes ambiguous — update
                // the allowlist validation or switch to a length-prefixed format first.
                var occurredAt = DateTimeOffset.UtcNow;
                var chainInput = $"{previousHash}:{json}:{occurredAt:O}:{sourceBrand ?? ""}";
                var stateHash  = _hashProvider.ComputeHash(chainInput);

                var aggregateId = ExtractAggregateId(domainEvent);

                var auditEvent = AuditEvent.Create(
                    tenantId:      tenantId,
                    eventType:     domainEvent.GetType().Name,
                    aggregateId:   aggregateId,
                    payload:       json,
                    stateHash:     stateHash,
                    previousHash:  previousHash,
                    actorId:       pseudonymizedActorId,
                    sourceIp:      _requestContext.SourceIp,
                    hashAlgorithm: _hashProvider.AlgorithmType,
                    sourceBrand:   sourceBrand);

                previousHash = stateHash; // chain each event within the same batch

                await _repository.AddAsync(auditEvent, ct).ConfigureAwait(false);
                batchEvents.Add(auditEvent);

                // fire-and-forget — sink failures must not block the primary audit write
                FireAndForgetSink(tenantId, auditEvent.EventType, stateHash, previousHash, auditEvent.OccurredAt, ct);
            }

            // IMPORTANT: SaveChangesAsync is called while the advisory lock is still held.
            // This closes the window between "stage the row" and "commit the row" during which
            // a concurrent writer could read the same tail hash and produce a forked chain.
            // For InProcessAuditWriteLock: the semaphore prevents any other writer from calling
            // GetLastHashAsync until this commit completes.
            // For PostgresAdvisoryAuditWriteLock: the xact-level advisory lock is released
            // at transaction end (commit), so this ordering is also correct for PostgreSQL.
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            // WORM escrow: fire-and-forget after primary DB commit — never blocks the write path.
            foreach (var ae in batchEvents)
                FireAndForgetEscrow(ae, ct);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void FireAndForgetSink(
        Guid tenantId, string eventType, string stateHash,
        string previousHash, DateTimeOffset occurredAt, CancellationToken ct)
    {
        // Intentionally not awaited — the sink must never block the primary write path.
        _ = _sink.WriteAsync(tenantId, eventType, stateHash, previousHash, occurredAt, ct)
                 .ContinueWith(t => { /* swallow */ }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void FireAndForgetEscrow(AuditEvent auditEvent, CancellationToken ct)
    {
        // Intentionally not awaited — escrow failures must never block or roll back the primary DB write.
        _ = _escrowWriter.WriteAsync(auditEvent, ct)
                         .ContinueWith(t => { /* swallow */ }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Walks <see cref="AggregateIdCandidates"/> in order and returns the first matching
    /// property value coerced to <see cref="Guid"/>.  Falls back to <see cref="Guid.Empty"/>.
    /// </summary>
    private static Guid ExtractAggregateId(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();

        foreach (var candidate in AggregateIdCandidates)
        {
            var property = type.GetProperty(candidate, BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
                continue;

            var value = property.GetValue(domainEvent);
            return ExtractGuid(value);
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Extracts the <c>TenantId</c> property from the event, coercing it to <see cref="Guid"/>.
    /// Returns <see cref="Guid.Empty"/> when the property is absent.
    /// </summary>
    private static Guid ExtractTenantId(IDomainEvent domainEvent)
    {
        var property = domainEvent.GetType()
            .GetProperty("TenantId", BindingFlags.Public | BindingFlags.Instance);

        return property is null ? Guid.Empty : ExtractGuid(property.GetValue(domainEvent));
    }

    /// <summary>
    /// Coerces a raw property value to <see cref="Guid"/>.
    /// Handles both plain <see cref="Guid"/> values and strongly-typed value objects that
    /// expose their wrapped value through a <c>Value</c> property (e.g. <c>TenantId</c>,
    /// <c>FacilityId</c>).
    /// </summary>
    private static Guid ExtractGuid(object? value) =>
        value switch
        {
            Guid g => g,
            { } v  => (Guid)(v.GetType().GetProperty("Value")?.GetValue(v) ?? Guid.Empty),
            _      => Guid.Empty,
        };
}
