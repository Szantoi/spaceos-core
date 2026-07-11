using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SpaceOS.Modules.Inventory.Api.Endpoints;
using SpaceOS.Modules.Inventory.Application.Commands.RegisterOffcutBatch;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

public class OffcutBatchEndpointTests
{
    private static readonly Guid SourceId          = Guid.NewGuid();
    private static readonly Guid MaterialCatalogId = Guid.NewGuid();
    private static readonly Guid BatchId           = Guid.NewGuid();
    private static readonly Guid OffcutId          = Guid.NewGuid();

    private static readonly object ValidItem = new
    {
        materialCatalogId = MaterialCatalogId,
        materialCode = "MDF18mm",
        widthMm = 500m,
        heightMm = 400m,
        thicknessMm = 18m
    };

    private HttpClient CreateClient(Mock<IMediator> mediatorMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOffcutEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var client = (app.Services.GetRequiredService<IServer>() as TestServer)!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    private HttpClient CreateUnauthenticatedClient(Mock<IMediator> mediatorMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        // Register a no-op default scheme so auth middleware doesn't throw, but require cookie scheme
        // which will fail for unauthenticated requests
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, NoTidAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOffcutEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        return (app.Services.GetRequiredService<IServer>() as TestServer)!.CreateClient();
    }

    [Fact]
    public async Task RegisterBatch_NewBatch_Returns201Created()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<RegisterOffcutBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterOffcutBatchResponse>.Success(
                new RegisterOffcutBatchResponse(BatchId, new List<Guid> { OffcutId }, IsNew: true)));

        var response = await CreateClient(m).PostAsJsonAsync(
            "/api/inventory/offcuts/batch",
            new { sourceType = "CuttingSheet", sourceId = SourceId, items = new[] { ValidItem } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterBatch_ExistingBatch_Returns200Ok()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<RegisterOffcutBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterOffcutBatchResponse>.Success(
                new RegisterOffcutBatchResponse(BatchId, new List<Guid> { OffcutId }, IsNew: false)));

        var response = await CreateClient(m).PostAsJsonAsync(
            "/api/inventory/offcuts/batch",
            new { sourceType = "CuttingSheet", sourceId = SourceId, items = new[] { ValidItem } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterBatch_NoTenantId_Returns401()
    {
        var m = new Mock<IMediator>();

        var response = await CreateUnauthenticatedClient(m).PostAsJsonAsync(
            "/api/inventory/offcuts/batch",
            new { sourceType = "CuttingSheet", sourceId = SourceId, items = new[] { ValidItem } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterBatch_EmptyItems_Returns400BadRequest()
    {
        var m = new Mock<IMediator>();

        var response = await CreateClient(m).PostAsJsonAsync(
            "/api/inventory/offcuts/batch",
            new { sourceType = "CuttingSheet", sourceId = SourceId, items = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>Authenticates the user without a 'tid' claim, simulating a session with no tenant context.</summary>
internal sealed class NoTidAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoTidAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
