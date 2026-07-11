using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Sales.Application.Outbox;
using SpaceOS.Modules.Sales.Application.Quotes.Commands;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Application;

public class RequestConversionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly FakeQuoteNumberGenerator _numGen = new();
    private readonly Mock<IQuoteRepository> _quotes = new();
    private readonly Mock<IOutboxRepository> _outbox = new();

    private RequestConversionCommandHandler BuildHandler() =>
        new(_quotes.Object, _outbox.Object, _clock,
            NullLogger<RequestConversionCommandHandler>.Instance);

    private async Task<Quote> BuildAcceptedQuoteAsync()
    {
        var result = await Quote.CreateAsync(
            TenantId, Guid.NewGuid(), "HUF", "sub:user1", _numGen, _clock, default);
        var quote = result.Value;
        var line = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null, "Termék", 1m,
            new Money(1000m, "HUF"), 0.27m, null, 1).Value;
        quote.AddLine(line);
        quote.Send(null, _clock);
        quote.Accept(_clock);
        return quote;
    }

    [Fact]
    public async Task Handle_QuoteNotFound_ReturnsNotFound()
    {
        _quotes.Setup(q => q.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Quote?)null);

        var result = await BuildHandler().Handle(new RequestConversionCommand(Guid.NewGuid()), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_AcceptedQuote_CallsOutboxAddMessage()
    {
        var quote = await BuildAcceptedQuoteAsync();
        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);
        _quotes.Setup(q => q.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _outbox.Setup(o => o.AddMessageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IClock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outbox.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildHandler().Handle(new RequestConversionCommand(quote.Id), default);

        result.IsSuccess.Should().BeTrue();
        _outbox.Verify(o => o.AddMessageAsync(
            It.IsAny<Guid>(), quote.Id, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IClock>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_QuoteNotAccepted_ReturnsInvalid()
    {
        var result = await Quote.CreateAsync(
            TenantId, Guid.NewGuid(), "HUF", "sub", _numGen, _clock, default);
        var quote = result.Value; // Draft status

        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var handlerResult = await BuildHandler().Handle(
            new RequestConversionCommand(quote.Id), default);

        handlerResult.IsSuccess.Should().BeFalse();
        handlerResult.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_WhenConversionAlreadyRequested_ReturnsSuccessIdempotently()
    {
        // The domain returns Success idempotently when ConversionRequestedAt is already set.
        // The handler also returns Success — the DB idempotency key (QuoteId) prevents duplicate
        // outbox rows at the DB level.
        var quote = await BuildAcceptedQuoteAsync();
        quote.RequestConversion(_clock); // first time — sets ConversionRequestedAt

        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);
        _quotes.Setup(q => q.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _outbox.Setup(o => o.AddMessageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IClock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outbox.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Second handler call — the domain is idempotent (returns Success without a new event).
        var result = await BuildHandler().Handle(new RequestConversionCommand(quote.Id), default);

        // The handler returns Success; DB-level idempotency key prevents duplicate outbox rows.
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AcceptedQuote_SetsConversionRequestedAt()
    {
        var quote = await BuildAcceptedQuoteAsync();
        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);
        _quotes.Setup(q => q.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _outbox.Setup(o => o.AddMessageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IClock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outbox.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await BuildHandler().Handle(new RequestConversionCommand(quote.Id), default);

        quote.ConversionRequestedAt.Should().NotBeNull();
    }
}
