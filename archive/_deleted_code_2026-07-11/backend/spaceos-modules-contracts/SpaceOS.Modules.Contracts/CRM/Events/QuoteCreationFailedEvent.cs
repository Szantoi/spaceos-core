namespace SpaceOS.Modules.Contracts.CRM.Events;

using SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Published by Sales module when Quote creation fails during Opportunity conversion.
/// CRM consumes this to rollback the Opportunity to Negotiation status.
/// Part of ADR-063 (Sales → CRM Integration Pattern).
/// </summary>
public record QuoteCreationFailedEvent : ModuleEvent
{
    /// <summary>Gets the conversion ID (matches OpportunityConvertedToQuoteEvent.ConversionId).</summary>
    public required Guid ConversionId { get; init; }

    /// <summary>Gets the Opportunity ID that failed conversion.</summary>
    public required Guid OpportunityId { get; init; }

    /// <summary>Gets the reason why the conversion failed.</summary>
    public required string Reason { get; init; }
}
