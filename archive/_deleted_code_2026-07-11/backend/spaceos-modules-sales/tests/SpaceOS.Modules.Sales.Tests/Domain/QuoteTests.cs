using FluentAssertions;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Events;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Domain;

public class QuoteTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly FakeQuoteNumberGenerator _numGen = new();

    private async Task<Quote> BuildDraftQuoteAsync()
    {
        var result = await Quote.CreateAsync(
            TenantId, CustomerId, "HUF", "sub:user1", _numGen, _clock, default);
        return result.Value;
    }

    private static QuoteLine BuildLine(decimal amount = 1000m) =>
        QuoteLine.Create(
            TenantId, QuoteLineType.Product, null, "Test line", 1m,
            new Money(amount, "HUF"), 0.27m, null, 1).Value;

    [Fact]
    public async Task CreateAsync_ValidArgs_ReturnsDraftQuote()
    {
        var result = await Quote.CreateAsync(
            TenantId, CustomerId, "HUF", "sub:user1", _numGen, _clock, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(QuoteStatus.Draft);
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.Currency.Should().Be("HUF");
    }

    [Fact]
    public async Task AddLine_Draft_AddsLine()
    {
        var quote = await BuildDraftQuoteAsync();

        var result = quote.AddLine(BuildLine());

        result.IsSuccess.Should().BeTrue();
        quote.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddLine_NotDraft_ReturnsInvalid()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);

        var result = quote.AddLine(BuildLine(500m));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveLine_ExistingLine_RemovesIt()
    {
        var quote = await BuildDraftQuoteAsync();
        var line = BuildLine();
        quote.AddLine(line);

        var result = quote.RemoveLine(line.Id);

        result.IsSuccess.Should().BeTrue();
        quote.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_Draft_ChangesSentAndSetsContentHash()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());

        var result = quote.Send(null, _clock);

        result.IsSuccess.Should().BeTrue();
        quote.Status.Should().Be(QuoteStatus.Sent);
        quote.ContentHash.Should().NotBeNullOrEmpty();
        quote.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Send_EmptyLines_ReturnsInvalid()
    {
        var quote = await BuildDraftQuoteAsync();

        var result = quote.Send(null, _clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Accept_Sent_ChangesAccepted()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);

        var result = quote.Accept(_clock);

        result.IsSuccess.Should().BeTrue();
        quote.Status.Should().Be(QuoteStatus.Accepted);
    }

    [Fact]
    public async Task Accept_Draft_ReturnsInvalid()
    {
        var quote = await BuildDraftQuoteAsync();

        var result = quote.Accept(_clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Reject_Sent_ChangesRejected()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);

        var result = quote.Reject("Nem felel meg az árnak.", _clock);

        result.IsSuccess.Should().BeTrue();
        quote.Status.Should().Be(QuoteStatus.Rejected);
        quote.RejectionReason.Should().Be("Nem felel meg az árnak.");
    }

    [Fact]
    public async Task RequestConversion_Accepted_SetsConversionRequestedAt()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);

        var result = quote.RequestConversion(_clock);

        result.IsSuccess.Should().BeTrue();
        quote.ConversionRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RequestConversion_Idempotent_SecondCallSuccess()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        var second = quote.RequestConversion(_clock);

        second.IsSuccess.Should().BeTrue(); // idempotent
    }

    [Fact]
    public async Task CompleteConversion_Archived_ReturnsInvalid()
    {
        // SEC-S-11: blocked on archived quote
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        // Force-archive by calling Archive (archive guard requires no pending conversion,
        // so first clear the ConversionRequestedAt by failing the conversion)
        quote.FailConversion("test-error");
        quote.Archive(_clock);

        var result = quote.CompleteConversion(Guid.NewGuid(), _clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteConversion_Accepted_ChangesConverted()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        var orderId = Guid.NewGuid();
        var result = quote.CompleteConversion(orderId, _clock);

        result.IsSuccess.Should().BeTrue();
        quote.Status.Should().Be(QuoteStatus.Converted);
        quote.ConvertedOrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task FailConversion_ResetsConversionRequestedAt()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        quote.FailConversion("http-timeout");

        quote.ConversionRequestedAt.Should().BeNull();
        quote.ConversionFailureReason.Should().Be("http-timeout");
    }

    [Fact]
    public async Task Archive_PendingConversion_ReturnsInvalid()
    {
        // BE-S-11: cannot archive during pending conversion
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine());
        quote.Send(null, _clock);
        quote.Accept(_clock);
        quote.RequestConversion(_clock);

        var result = quote.Archive(_clock);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecomputeTotals_SumsCorrectly()
    {
        var quote = await BuildDraftQuoteAsync();
        quote.AddLine(BuildLine(1000m));
        quote.AddLine(BuildLine(2000m));

        quote.TotalNet.Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task ContentHash_DeterministicOnSameData()
    {
        var clock1 = new FakeClock();
        var clock2 = new FakeClock();
        var gen1 = new FakeQuoteNumberGenerator();
        var gen2 = new FakeQuoteNumberGenerator();

        var q1 = (await Quote.CreateAsync(TenantId, CustomerId, "HUF", "sub", gen1, clock1, default)).Value;
        var q2 = (await Quote.CreateAsync(TenantId, CustomerId, "HUF", "sub", gen2, clock2, default)).Value;

        q1.AddLine(BuildLine(1000m));
        q2.AddLine(BuildLine(1000m));

        q1.Send(null, clock1);
        q2.Send(null, clock2);

        q1.ContentHash.Should().Be(q2.ContentHash);
    }
}
