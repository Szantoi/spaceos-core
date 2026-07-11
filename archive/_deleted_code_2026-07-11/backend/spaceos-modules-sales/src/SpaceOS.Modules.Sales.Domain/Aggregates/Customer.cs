using Ardalis.Result;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Events;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Domain.Aggregates;

/// <summary>
/// CRM Customer aggregate. Represents a tenant-private contact record (D-07).
/// Optionally linked to a platform actor via a Kernel-handshake-derived trust (SEC-S-02).
/// </summary>
public sealed class Customer : TenantScopedEntity
{
    /// <summary>Customer classification (Individual or Company).</summary>
    public CustomerType Type { get; private set; }

    /// <summary>Display name (max 200 chars).</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>Company tax number (max 50 chars, Company only).</summary>
    public string? CompanyTaxNumber { get; private set; }

    /// <summary>Primary contact person name (max 200 chars).</summary>
    public string ContactName { get; private set; } = default!;

    /// <summary>Contact email address.</summary>
    public Email? ContactEmail { get; private set; }

    /// <summary>Contact phone number.</summary>
    public PhoneNumber? ContactPhone { get; private set; }

    /// <summary>Optional billing address.</summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>Optional shipping address (defaults to billing if omitted).</summary>
    public Address? ShippingAddress { get; private set; }

    /// <summary>Current lifecycle status.</summary>
    public CustomerStatus Status { get; private set; }

    /// <summary>Linked platform tenant ID (D-12 / SEC-S-02).</summary>
    public Guid? LinkedTenantId { get; private set; }

    /// <summary>When the link was established.</summary>
    public DateTimeOffset? LinkedAt { get; private set; }

    /// <summary>B2B handshake verification state (SEC-S-02).</summary>
    public LinkVerificationStatus LinkStatus { get; private set; } = LinkVerificationStatus.None;

    /// <summary>When the link was verified via Kernel handshake.</summary>
    public DateTimeOffset? LinkVerifiedAt { get; private set; }

    /// <summary>Optional free-text notes (max 2000 chars).</summary>
    public string? Notes { get; private set; }

    /// <summary>Whether the customer has been soft-deleted.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>JWT sub of the actor who created this customer.</summary>
    public string CreatedBy { get; private set; } = default!;

    /// <summary>Last-modification timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Customer() { } // EF Core

    /// <summary>
    /// Creates a new Customer aggregate in Lead status.
    /// </summary>
    public static Result<Customer> Create(
        Guid tenantId,
        CustomerType type,
        string displayName,
        string contactName,
        Email? email,
        PhoneNumber? phone,
        string createdBy,
        IClock clock)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200)
            return Result.Invalid(new ValidationError("DisplayName: 1..200 char required."));
        if (string.IsNullOrWhiteSpace(contactName) || contactName.Length > 200)
            return Result.Invalid(new ValidationError("ContactName: 1..200 char required."));
        if (string.IsNullOrWhiteSpace(createdBy))
            return Result.Invalid(new ValidationError("CreatedBy cannot be empty."));

        return Result.Success(new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            DisplayName = displayName,
            ContactName = contactName,
            ContactEmail = email,
            ContactPhone = phone,
            Status = CustomerStatus.Lead,
            LinkStatus = LinkVerificationStatus.None,
            IsArchived = false,
            CreatedAt = clock.UtcNow,
            CreatedBy = createdBy
        }.WithEventTyped(c => new CustomerRegistered(c.Id, tenantId, type)));
    }

    /// <summary>Updates the contact information of the customer.</summary>
    public Result UpdateContact(string contactName, Email? email, PhoneNumber? phone, IClock clock)
    {
        if (IsArchived) return Result.Invalid(new ValidationError("Customer is archived."));
        if (string.IsNullOrWhiteSpace(contactName) || contactName.Length > 200)
            return Result.Invalid(new ValidationError("ContactName: 1..200 char required."));
        ContactName = contactName;
        ContactEmail = email;
        ContactPhone = phone;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new CustomerUpdated(Id, TenantId));
        return Result.Success();
    }

    /// <summary>Updates display name, tax number and notes.</summary>
    public Result UpdateDetails(string displayName, string? companyTaxNumber, string? notes, IClock clock)
    {
        if (IsArchived) return Result.Invalid(new ValidationError("Customer is archived."));
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200)
            return Result.Invalid(new ValidationError("DisplayName: 1..200 char required."));
        if (companyTaxNumber?.Length > 50)
            return Result.Invalid(new ValidationError("CompanyTaxNumber: max 50 chars."));
        if (notes?.Length > 2000)
            return Result.Invalid(new ValidationError("Notes: max 2000 chars."));
        DisplayName = displayName;
        CompanyTaxNumber = companyTaxNumber;
        Notes = notes;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new CustomerUpdated(Id, TenantId));
        return Result.Success();
    }

    /// <summary>Updates billing and/or shipping addresses.</summary>
    public Result UpdateAddresses(Address? billing, Address? shipping, IClock clock)
    {
        if (IsArchived) return Result.Invalid(new ValidationError("Customer is archived."));
        BillingAddress = billing;
        ShippingAddress = shipping;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new CustomerUpdated(Id, TenantId));
        return Result.Success();
    }

    /// <summary>
    /// Links this customer to a platform actor tenant.
    /// SEC-S-02: LinkStatus = Verified only if Kernel already has a confirmed B2B handshake.
    /// </summary>
    public Result LinkToPlatformActor(Guid platformTenantId, bool handshakeVerified, IClock clock)
    {
        if (platformTenantId == Guid.Empty)
            return Result.Invalid(new ValidationError("PlatformTenantId required."));
        if (platformTenantId == TenantId)
            return Result.Invalid(new ValidationError("Cannot link customer to its own tenant."));
        if (LinkedTenantId.HasValue)
            return Result.Invalid(new ValidationError("Customer already linked."));

        LinkedTenantId = platformTenantId;
        LinkedAt = clock.UtcNow;
        LinkStatus = handshakeVerified
            ? LinkVerificationStatus.Verified
            : LinkVerificationStatus.Pending;
        LinkVerifiedAt = handshakeVerified ? clock.UtcNow : null;
        AddDomainEvent(new CustomerLinkRequested(Id, TenantId, platformTenantId, LinkStatus));
        return Result.Success();
    }

    /// <summary>
    /// Promotes a Pending link to Verified once a Kernel handshake is confirmed (SEC-S-02).
    /// </summary>
    public Result MarkLinkVerified(IClock clock)
    {
        if (LinkStatus != LinkVerificationStatus.Pending)
            return Result.Invalid(new ValidationError("No pending link to verify."));
        LinkStatus = LinkVerificationStatus.Verified;
        LinkVerifiedAt = clock.UtcNow;
        AddDomainEvent(new CustomerLinkVerified(Id, TenantId, LinkedTenantId!.Value));
        return Result.Success();
    }

    /// <summary>Removes the platform actor link.</summary>
    public Result UnlinkFromPlatformActor()
    {
        if (!LinkedTenantId.HasValue)
            return Result.Invalid(new ValidationError("Customer is not linked."));
        LinkedTenantId = null;
        LinkedAt = null;
        LinkStatus = LinkVerificationStatus.None;
        LinkVerifiedAt = null;
        AddDomainEvent(new CustomerUnlinkedFromActor(Id, TenantId));
        return Result.Success();
    }

    /// <summary>Promotes a Lead to Active status.</summary>
    public Result Promote(IClock clock)
    {
        if (Status == CustomerStatus.Active) return Result.Success();
        if (Status == CustomerStatus.Inactive)
            return Result.Invalid(new ValidationError("Cannot promote inactive customer; reactivate first."));
        Status = CustomerStatus.Active;
        UpdatedAt = clock.UtcNow;
        return Result.Success();
    }

    /// <summary>Deactivates an Active customer (moves to Inactive).</summary>
    public Result Deactivate(IClock clock)
    {
        if (Status == CustomerStatus.Inactive) return Result.Success();
        Status = CustomerStatus.Inactive;
        UpdatedAt = clock.UtcNow;
        return Result.Success();
    }

    /// <summary>Soft-deletes (archives) the customer (BE-S-12).</summary>
    public Result Archive(IClock clock)
    {
        if (IsArchived) return Result.Success();
        IsArchived = true;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new CustomerArchived(Id, TenantId));
        return Result.Success();
    }

    // Helper that returns Customer (typed) rather than TenantScopedEntity for factory use.
    private Customer WithEventTyped(Func<Customer, IDomainEvent> evt)
    {
        AddDomainEvent(evt(this));
        return this;
    }
}
