using FluentAssertions;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Domain;

public class IdempotencyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly FakeQuoteNumberGenerator _numGen = new();

    private async Task<Quote> BuildAcceptedQuoteAsync()
    {
        var r = await Quote.CreateAsync(TenantId, CustomerId, "HUF", "sub", _numGen, _clock, default)
            .ConfigureAwait(false);
        var q = r.Value;
        var line = QuoteLine.Create(TenantId, QuoteLineType.Product, null, "Termék", 1m,
            new Money(1000m, "HUF"), 0.27m, null, 1).Value;
        q.AddLine(line);
        q.Send(null, _clock);
        q.Accept(_clock);
        return q;
    }

    [Fact]
    public async Task RequestConversion_CalledTwice_SecondCallIdempotent_NoDoubleEvent()
    {
        var quote = await BuildAcceptedQuoteAsync();

        // Clear events accumulated during state transitions in the helper
        quote.ClearDomainEvents();

        // First call — sets ConversionRequestedAt and adds exactly 1 domain event
        var first = quote.RequestConversion(_clock);
        var eventsAfterFirst = quote.PopDomainEvents();

        // Second call — idempotent, returns Success but no new event raised
        var second = quote.RequestConversion(_clock);
        var eventsAfterSecond = quote.PopDomainEvents();

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        eventsAfterFirst.Should().HaveCount(1);
        eventsAfterSecond.Should().BeEmpty();
        quote.ConversionRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void OutboxMessage_Create_HasPendingStatus()
    {
        var msg = SpaceOS.Modules.Sales.Infrastructure.Outbox.OutboxMessage.Create(
            TenantId, Guid.NewGuid(), "QuoteConversionRequested", "{}", "key-1", _clock);

        msg.Status.Should().Be("Pending");
        msg.AttemptCount.Should().Be(0);
        msg.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void OutboxMessage_RecordFailure_MaxAttempts_SetsFailed()
    {
        var msg = SpaceOS.Modules.Sales.Infrastructure.Outbox.OutboxMessage.Create(
            TenantId, Guid.NewGuid(), "QuoteConversionRequested", "{}", "key-2", _clock);

        for (var i = 0; i < 3; i++) msg.MarkInFlight(_clock);
        msg.RecordFailure("TestException", _clock, maxAttempts: 3);

        msg.Status.Should().Be("Failed");
        msg.LastError.Should().Be("TestException");
    }

    [Fact]
    public void QuoteNumber_ValidFormat_ParsesSuccessfully()
    {
        var result = QuoteNumber.From("Q-2026-00001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Q-2026-00001");
    }

    [Fact]
    public void QuoteNumber_InvalidFormat_ReturnsInvalid()
    {
        var result = QuoteNumber.From("invalid-number");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("Q-YYYY-NNNNN"));
    }

    [Fact]
    public void QuoteNumber_ShortFormat_ReturnsInvalid()
    {
        var result = QuoteNumber.From("Q-2026-0001");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Archive_AlreadyArchived_ReturnsSuccessIdempotently()
    {
        var quote = await BuildAcceptedQuoteAsync();

        var first = quote.Archive(_clock);
        var second = quote.Archive(_clock);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        quote.IsArchived.Should().BeTrue();
    }
}
