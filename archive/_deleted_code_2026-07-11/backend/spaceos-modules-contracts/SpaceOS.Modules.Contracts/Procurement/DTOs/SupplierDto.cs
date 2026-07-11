namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>Supplier master data record.</summary>
public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    int LeadTimeDays,
    string? Notes);
