using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.DTOs;
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

public class QuoteCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly FakeClock _clock = new();
    private readonly FakeQuoteNumberGenerator _numGen = new();
    private readonly Mock<IQuoteRepository> _quotes = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IQuotaGuard> _quota = new();
    private readonly Mock<ITenantContext> _tenant = new();

    public QuoteCommandHandlerTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantId);
        _tenant.Setup(t => t.ActorSub).Returns("sub:user1");
        _quota.Setup(q => q.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success());
        _quotes.Setup(r => r.AddAsync(It.IsAny<Quote>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        _quotes.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private CreateQuoteCommandHandler BuildCreateHandler() =>
        new(_quotes.Object, _numGen, _quota.Object, _tenant.Object, _clock);

    private SendQuoteCommandHandler BuildSendHandler() =>
        new(_quotes.Object, _tenant.Object, _clock);

    private AcceptQuoteCommandHandler BuildAcceptHandler() =>
        new(_quotes.Object, _tenant.Object, _clock);

    private ArchiveQuoteCommandHandler BuildArchiveHandler() =>
        new(_quotes.Object, _tenant.Object, _clock);

    private ArchiveCustomerCommandHandler BuildArchiveCustomerHandler() =>
        new(_customers.Object, _tenant.Object, _clock);

    private UpdateCustomerContactCommandHandler BuildUpdateContactHandler() =>
        new(_customers.Object, _tenant.Object, _clock);

    private async Task<Quote> BuildDraftAsync()
    {
        var r = await Quote.CreateAsync(TenantId, CustomerId, "HUF", "sub:user1", _numGen, _clock, default)
            .ConfigureAwait(false);
        return r.Value;
    }

    private static QuoteLine BuildLine() =>
        QuoteLine.Create(TenantId, QuoteLineType.Product, null, "Termék", 1m,
            new Money(1000m, "HUF"), 0.27m, null, 1).Value;

    private async Task<Quote> BuildSentAsync()
    {
        var q = await BuildDraftAsync().ConfigureAwait(false);
        q.AddLine(BuildLine());
        q.Send(null, _clock);
        return q;
    }

    private async Task<Quote> BuildAcceptedAsync()
    {
        var q = await BuildSentAsync().ConfigureAwait(false);
        q.Accept(_clock);
        return q;
    }

    // ─── CreateQuoteCommandHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateQuote_ValidArgs_ReturnsQuoteResponse()
    {
        var cmd = new CreateQuoteCommand(CustomerId, "HUF", null, null);

        var result = await BuildCreateHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.Status.Should().Be(QuoteStatus.Draft);
        result.Value.Currency.Should().Be("HUF");
    }

    [Fact]
    public async Task CreateQuote_QuotaExceeded_ReturnsForbidden()
    {
        _quota.Setup(q => q.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Forbidden());

        var cmd = new CreateQuoteCommand(CustomerId, "HUF", null, null);

        var result = await BuildCreateHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task CreateQuote_EmptyCustomerId_ReturnsInvalid()
    {
        var cmd = new CreateQuoteCommand(Guid.Empty, "HUF", null, null);

        var result = await BuildCreateHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task CreateQuote_InvalidCurrency_ReturnsInvalid()
    {
        var cmd = new CreateQuoteCommand(CustomerId, "XX", null, null);

        var result = await BuildCreateHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    // ─── SendQuoteCommandHandler ──────────────────────────────────────────────

    [Fact]
    public async Task SendQuote_DraftWithLines_ReturnsSentResponse()
    {
        var quote = await BuildDraftAsync();
        quote.AddLine(BuildLine());
        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildSendHandler().Handle(new SendQuoteCommand(quote.Id, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(QuoteStatus.Sent);
        result.Value.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendQuote_QuoteNotFound_ReturnsNotFound()
    {
        _quotes.Setup(q => q.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Quote?)null);

        var result = await BuildSendHandler().Handle(new SendQuoteCommand(Guid.NewGuid(), null), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task SendQuote_CrossTenant_ReturnsForbidden()
    {
        var otherTenant = Guid.NewGuid();
        var quoteResult = await Quote.CreateAsync(otherTenant, CustomerId, "HUF", "sub", _numGen, _clock, default);
        var quote = quoteResult.Value;
        quote.AddLine(BuildLine());

        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildSendHandler().Handle(new SendQuoteCommand(quote.Id, null), default);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    // ─── AcceptQuoteCommandHandler ────────────────────────────────────────────

    [Fact]
    public async Task AcceptQuote_SentQuote_ReturnsAccepted()
    {
        var quote = await BuildSentAsync();
        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildAcceptHandler().Handle(new AcceptQuoteCommand(quote.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(QuoteStatus.Accepted);
        result.Value.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptQuote_NotFound_ReturnsNotFound()
    {
        _quotes.Setup(q => q.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Quote?)null);

        var result = await BuildAcceptHandler().Handle(new AcceptQuoteCommand(Guid.NewGuid()), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task AcceptQuote_DraftQuote_ReturnsInvalid()
    {
        var quote = await BuildDraftAsync();
        _quotes.Setup(q => q.GetByIdWithLinesAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildAcceptHandler().Handle(new AcceptQuoteCommand(quote.Id), default);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    // ─── ArchiveQuoteCommandHandler ───────────────────────────────────────────

    [Fact]
    public async Task ArchiveQuote_DraftQuote_ReturnsSuccess()
    {
        var quote = await BuildDraftAsync();
        _quotes.Setup(q => q.GetByIdAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildArchiveHandler().Handle(new ArchiveQuoteCommand(quote.Id), default);

        result.IsSuccess.Should().BeTrue();
        quote.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveQuote_PendingConversion_ReturnsInvalid()
    {
        var quote = await BuildAcceptedAsync();
        quote.RequestConversion(_clock);

        _quotes.Setup(q => q.GetByIdAsync(quote.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(quote);

        var result = await BuildArchiveHandler().Handle(new ArchiveQuoteCommand(quote.Id), default);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ArchiveQuote_NotFound_ReturnsNotFound()
    {
        _quotes.Setup(q => q.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Quote?)null);

        var result = await BuildArchiveHandler().Handle(new ArchiveQuoteCommand(Guid.NewGuid()), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ─── ArchiveCustomerCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task ArchiveCustomer_Success()
    {
        var customer = Customer.Create(TenantId, CustomerType.Individual, "Test", "Contact",
            null, null, "sub", _clock).Value;
        _customers.Setup(c => c.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(customer);
        _customers.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildArchiveCustomerHandler().Handle(new ArchiveCustomerCommand(customer.Id), default);

        result.IsSuccess.Should().BeTrue();
        customer.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveCustomer_NotFound_ReturnsNotFound()
    {
        _customers.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Customer?)null);

        var result = await BuildArchiveCustomerHandler()
            .Handle(new ArchiveCustomerCommand(Guid.NewGuid()), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ArchiveCustomer_CrossTenant_ReturnsForbidden()
    {
        var otherTenant = Guid.NewGuid();
        var customer = Customer.Create(otherTenant, CustomerType.Individual, "Test", "Contact",
            null, null, "sub", _clock).Value;
        _customers.Setup(c => c.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(customer);

        var result = await BuildArchiveCustomerHandler()
            .Handle(new ArchiveCustomerCommand(customer.Id), default);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    // ─── UpdateCustomerContactCommandHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateContact_Success()
    {
        var customer = Customer.Create(TenantId, CustomerType.Company, "Kft.", "Old Contact",
            null, null, "sub", _clock).Value;
        _customers.Setup(c => c.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(customer);
        _customers.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cmd = new UpdateCustomerContactCommand(customer.Id, "New Contact", null, null);
        var result = await BuildUpdateContactHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be("New Contact");
    }

    [Fact]
    public async Task UpdateContact_CrossTenant_ReturnsForbidden()
    {
        var otherTenant = Guid.NewGuid();
        var customer = Customer.Create(otherTenant, CustomerType.Company, "Kft.", "Contact",
            null, null, "sub", _clock).Value;
        _customers.Setup(c => c.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(customer);

        var cmd = new UpdateCustomerContactCommand(customer.Id, "New Contact", null, null);
        var result = await BuildUpdateContactHandler().Handle(cmd, default);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task UpdateContact_NotFound_ReturnsNotFound()
    {
        _customers.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Customer?)null);

        var cmd = new UpdateCustomerContactCommand(Guid.NewGuid(), "Contact", null, null);
        var result = await BuildUpdateContactHandler().Handle(cmd, default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
