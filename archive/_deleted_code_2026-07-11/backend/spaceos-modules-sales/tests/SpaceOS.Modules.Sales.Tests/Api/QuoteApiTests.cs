using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Sales.Api.Endpoints;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Quotes.Commands;
using SpaceOS.Modules.Sales.Application.Quotes.Queries;
using SpaceOS.Modules.Sales.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Api;

public class QuoteApiTests
{
    private static readonly Guid TenantId = SalesTestAuthHandler.DefaultTenantId;

    private static QuoteResponse MakeQuoteResponse() => new(
        Guid.NewGuid(), TenantId, Guid.NewGuid(), "Q-2026-00001",
        QuoteStatus.Draft, "HUF", null, null,
        0m, 0m, 0m,
        DateTimeOffset.UtcNow, "sub:user", null, null, null, null,
        null, null, null, null, null, false, []);

    private static void AddTestRateLimiter(IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.AddPolicy<string>("per-tenant", _ =>
                RateLimitPartition.GetNoLimiter("test"));
            opts.AddPolicy<string>("convert", _ =>
                RateLimitPartition.GetNoLimiter("test"));
        });
    }

    private HttpClient BuildAuthClient(Mock<ISender> senderMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(senderMock.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, SalesTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy("TenantUser", p => p.RequireAuthenticatedUser());
            opts.AddPolicy("SalesUser", p => p.RequireAuthenticatedUser());
            opts.AddPolicy("TenantAdmin", p => p.RequireAuthenticatedUser());
        });
        AddTestRateLimiter(builder.Services);
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapQuoteEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var server = app.Services.GetRequiredService<IServer>() as TestServer;
        return server!.CreateClient();
    }

    private HttpClient BuildNoAuthClient(Mock<ISender> senderMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(senderMock.Object);
        builder.Services.AddAuthentication("NoAuth")
            .AddScheme<AuthenticationSchemeOptions, SalesNoAuthHandler>("NoAuth", _ => { });
        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy("TenantUser", p => p.RequireAuthenticatedUser());
            opts.AddPolicy("SalesUser", p => p.RequireAuthenticatedUser());
            opts.AddPolicy("TenantAdmin", p => p.RequireAuthenticatedUser());
        });
        AddTestRateLimiter(builder.Services);
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapQuoteEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var server = app.Services.GetRequiredService<IServer>() as TestServer;
        return server!.CreateClient();
    }

    [Fact]
    public async Task CreateQuote_ValidRequest_Returns201()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<CreateQuoteCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(MakeQuoteResponse()));
        var client = BuildAuthClient(sender);

        var body = new { customerId = Guid.NewGuid(), currency = "HUF" };
        var resp = await client.PostAsJsonAsync("/sales/api/quotes", body);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateQuote_NoJwt_Returns401()
    {
        var sender = new Mock<ISender>();
        var client = BuildNoAuthClient(sender);

        var body = new { customerId = Guid.NewGuid(), currency = "HUF" };
        var resp = await client.PostAsJsonAsync("/sales/api/quotes", body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListQuotes_Returns200()
    {
        var sender = new Mock<ISender>();
        var paged = new SalesPagedResult<QuoteSummary>([], 0, 0, 50);
        sender.Setup(s => s.Send(It.IsAny<ListQuotesQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(paged));
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync("/sales/api/quotes?skip=0&take=10");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuote_NotFound_Returns404()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetQuoteQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<QuoteResponse>.NotFound());
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync($"/sales/api/quotes/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetQuote_Found_Returns200()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetQuoteQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(MakeQuoteResponse()));
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync($"/sales/api/quotes/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ArchiveQuote_ValidRequest_Returns200()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<ArchiveQuoteCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success());
        var client = BuildAuthClient(sender);

        var resp = await client.DeleteAsync($"/sales/api/quotes/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
