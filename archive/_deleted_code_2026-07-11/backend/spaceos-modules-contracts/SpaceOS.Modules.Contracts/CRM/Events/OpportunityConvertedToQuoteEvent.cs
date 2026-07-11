namespace SpaceOS.Modules.Contracts.CRM.Events;

using SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Published by CRM when an Opportunity is being converted to a Sales Quote.
/// Signals the Sales module to create a Quote from the Opportunity data.
/// Part of ADR-063 (CRM → Sales Integration Pattern).
/// </summary>
public record OpportunityConvertedToQuoteEvent : ModuleEvent
{
    /// <summary>Gets the Opportunity ID being converted.</summary>
    public required Guid OpportunityId { get; init; }

    /// <summary>Gets the idempotency key for this conversion (prevents duplicate quotes).</summary>
    public required Guid ConversionId { get; init; }

    /// <summary>Gets the Customer ID for the quote.</summary>
    public required Guid CustomerId { get; init; }

    /// <summary>Gets the Customer name.</summary>
    public required string CustomerName { get; init; }

    /// <summary>Gets the Customer email.</summary>
    public required string CustomerEmail { get; init; }

    /// <summary>Gets the line items to be included in the quote.</summary>
    public required List<QuoteLineItemDto> LineItems { get; init; }

    /// <summary>Gets optional special terms for the quote.</summary>
    public string? SpecialTerms { get; init; }

    /// <summary>Gets the Sales Rep ID responsible for this quote.</summary>
    public required Guid SalesRepId { get; init; }
}

/// <summary>
/// DTO for line items in a quote conversion.
/// </summary>
public record QuoteLineItemDto
{
    /// <summary>Gets the Product ID.</summary>
    public required Guid ProductId { get; init; }

    /// <summary>Gets the Product name.</summary>
    public required string ProductName { get; init; }

    /// <summary>Gets the quantity.</summary>
    public required int Quantity { get; init; }

    /// <summary>Gets the unit price.</summary>
    public required decimal UnitPrice { get; init; }
}
