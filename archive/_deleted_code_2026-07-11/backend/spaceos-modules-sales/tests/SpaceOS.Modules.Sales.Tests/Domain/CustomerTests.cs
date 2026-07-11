using FluentAssertions;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Events;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Domain;

public class CustomerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FakeClock Clock = new();

    private static Customer BuildCustomer(
        CustomerType type = CustomerType.Individual,
        string displayName = "Teszt Bt.",
        string contactName = "Kovács Péter")
    {
        var result = Customer.Create(
            TenantId, type, displayName, contactName,
            email: null, phone: null, createdBy: "sub:user1", Clock);
        return result.Value;
    }

    [Fact]
    public void Create_ValidArgs_ReturnsCustomerWithLeadStatus()
    {
        var result = Customer.Create(
            TenantId, CustomerType.Company, "Ajtógyártó Kft.", "Főnök Úr",
            email: null, phone: null, createdBy: "sub:actor", Clock);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CustomerStatus.Lead);
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_EmptyDisplayName_ReturnsInvalid()
    {
        var result = Customer.Create(
            TenantId, CustomerType.Individual, "", "Kovács Péter",
            null, null, "sub:actor", Clock);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_TooLongDisplayName_ReturnsInvalid()
    {
        var result = Customer.Create(
            TenantId, CustomerType.Individual, new string('A', 201), "Kovács",
            null, null, "sub:actor", Clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateContact_WhenArchived_ReturnsInvalid()
    {
        var customer = BuildCustomer();
        customer.Archive(Clock);

        var result = customer.UpdateContact("Új Név", null, null, Clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void LinkToPlatformActor_SelfLink_ReturnsInvalid()
    {
        var customer = BuildCustomer();

        // TenantId is the same as the customer's TenantId
        var result = customer.LinkToPlatformActor(TenantId, handshakeVerified: false, Clock);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("own tenant"));
    }

    [Fact]
    public void LinkToPlatformActor_AlreadyLinked_ReturnsInvalid()
    {
        var customer = BuildCustomer();
        var otherTenant = Guid.NewGuid();

        customer.LinkToPlatformActor(otherTenant, handshakeVerified: false, Clock);
        var second = customer.LinkToPlatformActor(Guid.NewGuid(), handshakeVerified: false, Clock);

        second.IsSuccess.Should().BeFalse();
        second.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("already linked"));
    }

    [Fact]
    public void LinkToPlatformActor_HandshakeVerified_SetsVerified()
    {
        var customer = BuildCustomer();
        var otherTenant = Guid.NewGuid();

        customer.LinkToPlatformActor(otherTenant, handshakeVerified: true, Clock);

        customer.LinkStatus.Should().Be(LinkVerificationStatus.Verified);
        customer.LinkVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void LinkToPlatformActor_HandshakeNotVerified_SetsPending()
    {
        var customer = BuildCustomer();

        customer.LinkToPlatformActor(Guid.NewGuid(), handshakeVerified: false, Clock);

        customer.LinkStatus.Should().Be(LinkVerificationStatus.Pending);
        customer.LinkVerifiedAt.Should().BeNull();
    }

    [Fact]
    public void MarkLinkVerified_NoPendingLink_ReturnsInvalid()
    {
        var customer = BuildCustomer(); // LinkStatus = None

        var result = customer.MarkLinkVerified(Clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void MarkLinkVerified_PendingLink_SetsVerified()
    {
        var customer = BuildCustomer();
        customer.LinkToPlatformActor(Guid.NewGuid(), handshakeVerified: false, Clock);

        var result = customer.MarkLinkVerified(Clock);

        result.IsSuccess.Should().BeTrue();
        customer.LinkStatus.Should().Be(LinkVerificationStatus.Verified);
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var customer = BuildCustomer();

        customer.Archive(Clock);

        customer.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Promote_LeadCustomer_SetsActive()
    {
        var customer = BuildCustomer();

        var result = customer.Promote(Clock);

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Active);
    }

    [Fact]
    public void Promote_InactiveCustomer_ReturnsInvalid()
    {
        var customer = BuildCustomer();
        customer.Deactivate(Clock);

        var result = customer.Promote(Clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_RaisesCustomerRegisteredEvent()
    {
        var result = Customer.Create(
            TenantId, CustomerType.Individual, "Test", "Contact",
            null, null, "sub:actor", Clock);

        result.Value.DomainEvents.Should().ContainSingle(e => e is CustomerRegistered);
    }

    [Fact]
    public void LinkToPlatformActor_RaisesCustomerLinkRequestedEvent()
    {
        var customer = BuildCustomer();

        customer.LinkToPlatformActor(Guid.NewGuid(), handshakeVerified: false, Clock);

        customer.DomainEvents.Should().Contain(e => e is CustomerLinkRequested);
    }
}
