using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.DTOs;

/// <summary>Full customer detail response.</summary>
public sealed record CustomerResponse(
    Guid Id,
    Guid TenantId,
    CustomerType Type,
    string DisplayName,
    string? CompanyTaxNumber,
    string ContactName,
    string? ContactEmail,
    string? ContactPhone,
    AddressDto? BillingAddress,
    AddressDto? ShippingAddress,
    CustomerStatus Status,
    Guid? LinkedTenantId,
    DateTimeOffset? LinkedAt,
    LinkVerificationStatus LinkStatus,
    DateTimeOffset? LinkVerifiedAt,
    string? Notes,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt);

/// <summary>Summary version for list responses.</summary>
public sealed record CustomerSummary(
    Guid Id,
    CustomerType Type,
    string DisplayName,
    string ContactName,
    string? ContactEmail,
    CustomerStatus Status,
    bool IsArchived,
    DateTimeOffset CreatedAt);

/// <summary>Address DTO for responses.</summary>
public sealed record AddressDto(
    string? Street,
    string? City,
    string? ZipCode,
    string? Country);
