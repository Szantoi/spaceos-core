using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Events;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Domain.Aggregates;

/// <summary>
/// Quote aggregate — the central FSM of the Sales module.
/// Implements the Draft → Sent → Accepted/Rejected → Converted lifecycle (D-05).
/// Totals are Domain-computed (RULE 1). ContentHash is frozen on Send (D-06).
/// </summary>
public sealed class Quote : TenantScopedEntity
{
    private readonly List<QuoteLine> _lines = [];

    /// <summary>The customer this quote is addressed to.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Human-readable per-tenant quote number (D-09).</summary>
    public QuoteNumber Number { get; private set; } = default!;

    /// <summary>Current FSM status.</summary>
    public QuoteStatus Status { get; private set; }

    /// <summary>ISO 4217 currency for the entire quote (single-currency aggregate — BE-S-04).</summary>
    public string Currency { get; private set; } = "HUF";

    /// <summary>Optional expiry date (must be in the future when set).</summary>
    public DateTimeOffset? ValidUntil { get; private set; }

    /// <summary>Optional notes (max 2000 chars).</summary>
    public string? Notes { get; private set; }

    /// <summary>Domain-computed net total (RULE 1).</summary>
    public Money TotalNet { get; internal set; }

    /// <summary>Domain-computed VAT total (RULE 1).</summary>
    public Money TotalVat { get; internal set; }

    /// <summary>Domain-computed gross total (RULE 1).</summary>
    public Money TotalGross { get; internal set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>JWT sub of the actor who created this quote.</summary>
    public string CreatedBy { get; private set; } = default!;

    /// <summary>When the quote was sent to the customer.</summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>When the quote was accepted.</summary>
    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>When the quote was rejected.</summary>
    public DateTimeOffset? RejectedAt { get; private set; }

    /// <summary>Reason provided with rejection (max 500 chars).</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>When conversion to an order was completed.</summary>
    public DateTimeOffset? ConvertedAt { get; private set; }

    /// <summary>The resulting order ID after successful conversion.</summary>
    public Guid? ConvertedOrderId { get; private set; }

    /// <summary>ADR-039: when conversion was requested (outbox bookkeeping).</summary>
    public DateTimeOffset? ConversionRequestedAt { get; private set; }

    /// <summary>ADR-039: last conversion failure reason.</summary>
    public string? ConversionFailureReason { get; private set; }

    /// <summary>SHA-256 content hash frozen at Send time (D-06).</summary>
    public string? ContentHash { get; private set; }

    /// <summary>Whether the quote has been soft-deleted.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Read-only view of quote lines.</summary>
    public IReadOnlyList<QuoteLine> Lines => _lines.AsReadOnly();

    private Quote() { } // EF Core

    /// <summary>
    /// Creates a new Quote in Draft status, generating a per-tenant QuoteNumber via the advisory-locked generator.
    /// </summary>
    public static async Task<Result<Quote>> CreateAsync(
        Guid tenantId,
        Guid customerId,
        string currency,
        string createdBy,
        IQuoteNumberGenerator numberGen,
        IClock clock,
        CancellationToken ct)
    {
        if (customerId == Guid.Empty)
            return Result.Invalid(new ValidationError("CustomerId required."));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Invalid(new ValidationError("Currency must be ISO 4217 (3 chars)."));
        if (string.IsNullOrWhiteSpace(createdBy))
            return Result.Invalid(new ValidationError("CreatedBy cannot be empty."));

        var number = await numberGen.NextAsync(tenantId, clock.UtcNow.Year, ct)
            .ConfigureAwait(false);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Number = number,
            Status = QuoteStatus.Draft,
            Currency = currency,
            TotalNet = Money.Zero(currency),
            TotalVat = Money.Zero(currency),
            TotalGross = Money.Zero(currency),
            CreatedAt = clock.UtcNow,
            CreatedBy = createdBy,
            IsArchived = false
        };
        quote.AddDomainEvent(new QuoteCreated(quote.Id, tenantId, customerId, number));
        return Result.Success(quote);
    }

    // ─── Line mutations (Draft only) ─────────────────────────────────────────

    /// <summary>Adds a line to the quote. Only allowed in Draft status.</summary>
    public Result AddLine(QuoteLine line)
    {
        if (Status != QuoteStatus.Draft)
            return Result.Invalid(new ValidationError($"Cannot modify lines in {Status}."));
        if (_lines.Count >= 200)
            return Result.Invalid(new ValidationError("Maximum 200 lines per quote."));
        if (line.Currency != Currency)
            return Result.Invalid(new ValidationError("Line currency must match quote."));

        _lines.Add(line);
        RecomputeTotals();
        return Result.Success();
    }

    /// <summary>Removes a line by ID. Only allowed in Draft status.</summary>
    public Result RemoveLine(Guid lineId)
    {
        if (Status != QuoteStatus.Draft)
            return Result.Invalid(new ValidationError($"Cannot modify lines in {Status}."));
        var removed = _lines.RemoveAll(l => l.Id == lineId);
        if (removed == 0)
            return Result.NotFound($"Line {lineId} not on quote.");
        RecomputeTotals();
        return Result.Success();
    }

    // ─── FSM transitions ──────────────────────────────────────────────────────

    /// <summary>
    /// Transitions Draft → Sent, freezing the ContentHash snapshot (D-06).
    /// </summary>
    public Result Send(DateTimeOffset? validUntil, IClock clock)
    {
        if (Status != QuoteStatus.Draft)
            return Result.Invalid(new ValidationError($"Cannot Send in {Status}. Expected: Draft."));
        if (_lines.Count == 0)
            return Result.Invalid(new ValidationError("Quote must have at least one line."));
        if (validUntil.HasValue && validUntil <= clock.UtcNow)
            return Result.Invalid(new ValidationError("ValidUntil must be in the future."));

        Status = QuoteStatus.Sent;
        SentAt = clock.UtcNow;
        ValidUntil = validUntil;
        ContentHash = ComputeContentHash();
        AddDomainEvent(new QuoteSent(Id, TenantId, CustomerId));
        return Result.Success();
    }

    /// <summary>Transitions Sent → Accepted.</summary>
    public Result Accept(IClock clock)
    {
        if (Status != QuoteStatus.Sent)
            return Result.Invalid(new ValidationError($"Cannot Accept in {Status}. Expected: Sent."));
        Status = QuoteStatus.Accepted;
        AcceptedAt = clock.UtcNow;
        AddDomainEvent(new QuoteAccepted(Id, TenantId, CustomerId));
        return Result.Success();
    }

    /// <summary>Transitions Sent → Rejected with a mandatory reason.</summary>
    public Result Reject(string reason, IClock clock)
    {
        if (Status != QuoteStatus.Sent)
            return Result.Invalid(new ValidationError($"Cannot Reject in {Status}. Expected: Sent."));
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            return Result.Invalid(new ValidationError("RejectionReason: 1..500 char required."));
        Status = QuoteStatus.Rejected;
        RejectedAt = clock.UtcNow;
        RejectionReason = reason;
        AddDomainEvent(new QuoteRejected(Id, TenantId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Marks conversion as requested and writes an outbox message via the handler.
    /// Idempotent: if already requested, returns Success without re-adding events.
    /// ADR-039 / D-11.
    /// </summary>
    public Result RequestConversion(IClock clock)
    {
        if (Status != QuoteStatus.Accepted)
            return Result.Invalid(new ValidationError($"Cannot RequestConversion in {Status}. Expected: Accepted."));
        if (ConvertedOrderId.HasValue)
            return Result.Invalid(new ValidationError("Quote already converted."));
        if (ConversionRequestedAt.HasValue)
            return Result.Success(); // idempotent — outbox already has the message

        ConversionRequestedAt = clock.UtcNow;
        ConversionFailureReason = null;
        AddDomainEvent(new QuoteConversionRequested(Id, TenantId, CustomerId));
        return Result.Success();
    }

    /// <summary>
    /// Completes conversion: Accepted → Converted. SEC-S-11: blocked if archived.
    /// Called by the SalesIntegrationWorker after the Joinery order is confirmed.
    /// </summary>
    public Result CompleteConversion(Guid orderId, IClock clock)
    {
        if (IsArchived)
            return Result.Invalid(new ValidationError("Cannot complete conversion on archived quote.")); // SEC-S-11
        if (Status != QuoteStatus.Accepted)
            return Result.Invalid(new ValidationError($"Cannot CompleteConversion in {Status}."));
        if (!ConversionRequestedAt.HasValue)
            return Result.Invalid(new ValidationError("Conversion was not requested."));
        if (orderId == Guid.Empty)
            return Result.Invalid(new ValidationError("OrderId required."));

        Status = QuoteStatus.Converted;
        ConvertedAt = clock.UtcNow;
        ConvertedOrderId = orderId;
        AddDomainEvent(new QuoteConverted(Id, TenantId, CustomerId, orderId));
        return Result.Success();
    }

    /// <summary>
    /// Records a conversion failure. Quote reverts to retry-able state (ConversionRequestedAt = null).
    /// </summary>
    public Result FailConversion(string reason)
    {
        if (!ConversionRequestedAt.HasValue)
            return Result.Invalid(new ValidationError("Conversion was not requested."));
        if (Status == QuoteStatus.Converted)
            return Result.Invalid(new ValidationError("Cannot fail an already-converted quote."));

        ConversionFailureReason = reason;
        ConversionRequestedAt = null; // allow retry via RequestConversion (idempotent)
        AddDomainEvent(new QuoteConversionFailed(Id, TenantId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the quote. BE-S-11: blocked during a pending conversion to prevent orphan Joinery orders.
    /// </summary>
    public Result Archive(IClock clock)
    {
        if (ConversionRequestedAt.HasValue && Status != QuoteStatus.Converted)
            return Result.Invalid(new ValidationError("Cannot archive a quote with a pending conversion."));
        if (IsArchived) return Result.Success();
        IsArchived = true;
        AddDomainEvent(new QuoteArchived(Id, TenantId));
        return Result.Success();
    }

    // ─── Infrastructure hooks (called by EF Core interceptors only) ──────────

    /// <summary>
    /// BE-S-04: Restores Currency into the Money structs after EF Core materialises this entity.
    /// Called by <c>SalesDbContext.ChangeTracker.Tracked</c> — not part of the domain API.
    /// </summary>
    public void FixMoneyCurrency()
    {
        TotalNet   = new Money(TotalNet.Amount,   Currency);
        TotalVat   = new Money(TotalVat.Amount,   Currency);
        TotalGross = new Money(TotalGross.Amount, Currency);
        foreach (var line in _lines)
            line.FixUnitPriceCurrency(Currency);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private void RecomputeTotals()
    {
        TotalNet = Money.Sum(_lines.Select(l => l.LineNet), Currency);
        TotalVat = Money.Sum(_lines.Select(l => l.LineVat), Currency);
        TotalGross = Money.Sum(_lines.Select(l => l.LineGross), Currency);
    }

    /// <summary>
    /// Computes a SHA-256 hash over the quote content to create an immutable snapshot (D-06).
    /// Covers: TenantId, CustomerId, QuoteNumber, Currency, totals and all line details ordered by SortOrder.
    /// </summary>
    private string ComputeContentHash()
    {
        var sb = new StringBuilder();
        sb.Append(TenantId).Append(CustomerId).Append(Number.Value).Append(Currency);
        sb.Append(TotalNet.Amount).Append(TotalVat.Amount).Append(TotalGross.Amount);
        foreach (var line in _lines.OrderBy(l => l.SortOrder))
        {
            sb.Append(line.Description)
              .Append(line.Quantity)
              .Append(line.UnitPrice.Amount)
              .Append(line.VatRate)
              .Append(line.DiscountPercent ?? 0m)
              .Append(line.LineType);
        }
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
