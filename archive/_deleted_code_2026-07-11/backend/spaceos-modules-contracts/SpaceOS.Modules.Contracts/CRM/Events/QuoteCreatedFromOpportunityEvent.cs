namespace SpaceOS.Modules.Contracts.CRM.Events;

using SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Published by Sales module when a Quote is successfully created from an Opportunity.
/// CRM consumes this to transition the Opportunity to Won status.
/// Part of ADR-063 (Sales → CRM Integration Pattern).
/// </summary>
public record QuoteCreatedFromOpportunityEvent : ModuleEvent
{
    /// <summary>Gets the conversion ID (matches OpportunityConvertedToQuoteEvent.ConversionId).</summary>
    public required Guid ConversionId { get; init; }

    /// <summary>Gets the newly created Quote ID.</summary>
    public required Guid QuoteId { get; init; }

    /// <summary>Gets the Opportunity ID that was converted.</summary>
    public required Guid OpportunityId { get; init; }
}
