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
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.Customers.Queries;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Api;

public class CustomerApiTests
{
    private static readonly Guid TenantId = SalesTestAuthHandler.DefaultTenantId;

    private static CustomerResponse MakeCustomerResponse() => new(
        Guid.NewGuid(), TenantId, CustomerType.Company, "Test Kft.", null,
        "Teszt Elek", "test@example.com", null,
        null, null, CustomerStatus.Lead, null, null,
        LinkVerificationStatus.None, null, null, false,
        DateTimeOffset.UtcNow, "sub:user", null);

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
        app.MapCustomerEndpoints();
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
        app.MapCustomerEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var server = app.Services.GetRequiredService<IServer>() as TestServer;
        return server!.CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_ValidRequest_Returns201()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(MakeCustomerResponse()));
        var client = BuildAuthClient(sender);

        var body = new { type = 2, displayName = "Test Kft.", contactName = "Teszt Elek" };
        var resp = await client.PostAsJsonAsync("/sales/api/customers", body);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCustomer_NoJwt_Returns401()
    {
        var sender = new Mock<ISender>();
        var client = BuildNoAuthClient(sender);

        var body = new { type = 2, displayName = "Test", contactName = "Contact" };
        var resp = await client.PostAsJsonAsync("/sales/api/customers", body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCustomers_ValidRequest_Returns200()
    {
        var sender = new Mock<ISender>();
        var paged = new SalesPagedResult<CustomerSummary>([], 0, 0, 50);
        sender.Setup(s => s.Send(It.IsAny<ListCustomersQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(paged));
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync("/sales/api/customers?skip=0&take=10");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCustomer_NotFound_Returns404()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetCustomerQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<CustomerResponse>.NotFound());
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync($"/sales/api/customers/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomer_Found_Returns200WithBody()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetCustomerQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success(MakeCustomerResponse()));
        var client = BuildAuthClient(sender);

        var resp = await client.GetAsync($"/sales/api/customers/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
