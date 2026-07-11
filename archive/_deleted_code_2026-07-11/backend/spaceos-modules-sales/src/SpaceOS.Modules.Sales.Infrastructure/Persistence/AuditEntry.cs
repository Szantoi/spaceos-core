using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Events;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// Immutable audit log entry, one per domain event. SEC-S-08.
/// Written in the same transaction as the aggregate mutation by <see cref="AuditAndDispatchInterceptor"/>.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>Auto-generated identity column.</summary>
    public long Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>JWT sub of the actor who triggered the event.</summary>
    public string ActorSub { get; private set; } = default!;

    /// <summary>Aggregate type name (e.g. "Customer", "Quote").</summary>
    public string AggregateType { get; private set; } = default!;

    /// <summary>Aggregate identifier.</summary>
    public Guid AggregateId { get; private set; }

    /// <summary>Event type name (e.g. "QuoteCreated").</summary>
    public string Operation { get; private set; } = default!;

    /// <summary>SHA-256 hex hash of the serialised event JSON.</summary>
    public string PayloadHash { get; private set; } = default!;

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    private AuditEntry() { } // EF Core

    /// <summary>Creates an audit entry from a domain event.</summary>
    public static AuditEntry From(IDomainEvent evt, Guid tenantId, string actorSub, IClock clock)
    {
        var json = JsonSerializer.Serialize(evt, evt.GetType());
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(json)));
        var (aggType, aggId) = GetAggregateInfo(evt);
        return new AuditEntry
        {
            TenantId = tenantId,
            ActorSub = actorSub,
            AggregateType = aggType,
            AggregateId = aggId,
            Operation = evt.GetType().Name,
            PayloadHash = hash,
            OccurredAt = clock.UtcNow
        };
    }

    private static (string AggType, Guid AggId) GetAggregateInfo(IDomainEvent evt) => evt switch
    {
        CustomerRegistered e        => ("Customer", e.CustomerId),
        CustomerUpdated e           => ("Customer", e.CustomerId),
        CustomerLinkRequested e     => ("Customer", e.CustomerId),
        CustomerLinkVerified e      => ("Customer", e.CustomerId),
        CustomerUnlinkedFromActor e => ("Customer", e.CustomerId),
        CustomerArchived e          => ("Customer", e.CustomerId),
        QuoteCreated e              => ("Quote", e.QuoteId),
        QuoteSent e                 => ("Quote", e.QuoteId),
        QuoteAccepted e             => ("Quote", e.QuoteId),
        QuoteRejected e             => ("Quote", e.QuoteId),
        QuoteConversionRequested e  => ("Quote", e.QuoteId),
        QuoteConverted e            => ("Quote", e.QuoteId),
        QuoteConversionFailed e     => ("Quote", e.QuoteId),
        QuoteArchived e             => ("Quote", e.QuoteId),
        _                           => ("Unknown", Guid.Empty)
    };
}
