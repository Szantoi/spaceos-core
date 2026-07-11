using FluentAssertions;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Infrastructure.Security;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Security;

public class SecurityTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly FakeQuoteNumberGenerator _numGen = new();

    private sealed class StubTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public string ActorSub => "sub:test";
    }

    [Fact]
    public void EnsureSameTenant_DifferentTenant_ReturnsForbidden()
    {
        var customer = Customer.Create(
            TenantA, CustomerType.Individual, "Test", "Contact",
            null, null, "sub:actor", _clock).Value;

        var ctx = new StubTenantContext(TenantB);
        var result = ctx.EnsureSameTenant(customer);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public void EnsureSameTenant_SameTenant_ReturnsSuccess()
    {
        var customer = Customer.Create(
            TenantA, CustomerType.Individual, "Test", "Contact",
            null, null, "sub:actor", _clock).Value;

        var ctx = new StubTenantContext(TenantA);
        var result = ctx.EnsureSameTenant(customer);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Quote_CompleteConversion_OnArchivedQuote_ReturnsInvalid()
    {
        // SEC-S-11
        var quote = (await Quote.CreateAsync(
            TenantA, Guid.NewGuid(), "HUF", "sub", _numGen, _clock, default)).Value;

        var line = QuoteLine.Create(
            TenantA, QuoteLineType.Product, null, "T", 1m,
            new Money(100m, "HUF"), 0m, null, 1).Value;
        quote.AddLine(line);
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);
        quote.FailConversion("err"); // resets ConversionRequestedAt
        quote.Archive(_clock);       // now can archive

        var result = quote.CompleteConversion(Guid.NewGuid(), _clock);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("archived"));
    }

    [Fact]
    public async Task Quote_Archive_DuringPendingConversion_ReturnsInvalid()
    {
        // BE-S-11
        var quote = (await Quote.CreateAsync(
            TenantA, Guid.NewGuid(), "HUF", "sub", _numGen, _clock, default)).Value;

        var line = QuoteLine.Create(
            TenantA, QuoteLineType.Product, null, "T", 1m,
            new Money(100m, "HUF"), 0m, null, 1).Value;
        quote.AddLine(line);
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        var result = quote.Archive(_clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerTenantContext_HasCorrectTenantId()
    {
        var expected = Guid.NewGuid();
        var ctx = new WorkerTenantContext(expected);

        ctx.TenantId.Should().Be(expected);
        ctx.ActorSub.Should().Be("worker:integration");
    }

    [Fact]
    public void Customer_LinkToPlatformActor_SelfLink_ReturnsInvalid()
    {
        var customer = Customer.Create(
            TenantA, CustomerType.Individual, "Test", "Contact",
            null, null, "sub", _clock).Value;

        var result = customer.LinkToPlatformActor(TenantA, false, _clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Quote_RequestConversion_NotAccepted_ReturnsInvalid()
    {
        var quote = (await Quote.CreateAsync(
            TenantA, Guid.NewGuid(), "HUF", "sub", _numGen, _clock, default)).Value;

        // Quote is Draft — not Accepted
        var result = quote.RequestConversion(_clock);

        result.IsSuccess.Should().BeFalse();
    }
}
