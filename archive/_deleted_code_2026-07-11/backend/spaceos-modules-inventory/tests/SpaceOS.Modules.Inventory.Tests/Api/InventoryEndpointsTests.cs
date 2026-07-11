using System.Net;
using System.Net.Http.Json;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Inventory.Api.Endpoints;
using SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;
using SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;
using SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;
using SpaceOS.Modules.Inventory.Application.Queries.GetStock;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

public class InventoryEndpointsTests
{
    private HttpClient CreateAuthenticatedClient(Mock<IMediator> mediatorMock)
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
        app.MapInventoryEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        // Add auth header so TestAuthHandler picks it up
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact]
    public async Task GetStock_WithValidMaterial_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetStockQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockLevelResponse>.Success(new StockLevelResponse("MDF 18mm", 5, 2800, 2070, 2)));

        var client = CreateAuthenticatedClient(mediatorMock);
        var response = await client.GetAsync("/api/inventory/stock?materialType=MDF+18mm");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOffcuts_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetOffcutsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<OffcutResponse>>.Success(new List<OffcutResponse>()));

        var client = CreateAuthenticatedClient(mediatorMock);
        var response = await client.GetAsync("/api/inventory/offcuts?materialType=MDF+18mm");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumptionTrend_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetConsumptionTrendQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsumptionTrendResponse>.Success(
                new ConsumptionTrendResponse("MDF 18mm", new List<DailyConsumptionEntry>(), 0m)));

        var client = CreateAuthenticatedClient(mediatorMock);
        var response = await client.GetAsync("/api/inventory/trend?materialType=MDF+18mm");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordConsumption_WithoutAuth_Returns401()
    {
        var mediatorMock = new Mock<IMediator>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        // Register auth but use a scheme that denies everyone
        builder.Services.AddAuthentication("NoAuth")
            .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapInventoryEndpoints();
        await app.StartAsync();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient(); // No auth → 401

        var payload = new { MaterialType = "MDF 18mm", Thickness = 18m, Area = 5.5m, PanelCount = 1, Reason = "test", OccurredAt = DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/inventory/movements/consumption", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordConsumption_WithAuth_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<RecordConsumptionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateAuthenticatedClient(mediatorMock);
        var payload = new { MaterialType = "MDF 18mm", Thickness = 18m, Area = 5.5m, PanelCount = 1, Reason = "Gyártás", OccurredAt = DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/inventory/movements/consumption", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordInbound_WithAuth_Returns201()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<RecordInboundCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateAuthenticatedClient(mediatorMock);
        var payload = new { MaterialType = "MDF 18mm", Thickness = 18m, Area = 50m, PanelCount = 10, Reference = "PO-001", OccurredAt = DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/inventory/movements/inbound", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RecordInbound_UnknownMaterialType_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<RecordInboundCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFound("Material type 'UNKNOWN' not found."));

        var client = CreateAuthenticatedClient(mediatorMock);
        var payload = new { MaterialType = "UNKNOWN", Thickness = 18m, Area = 50m, PanelCount = 10, Reference = "PO-001", OccurredAt = DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/inventory/movements/inbound", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordOffcut_WithAuth_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<RecordOffcutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateAuthenticatedClient(mediatorMock);
        var payload = new { MaterialType = "MDF 18mm", WidthMm = 500m, HeightMm = 300m, OriginCuttingSheetId = (Guid?)null };
        var response = await client.PostAsJsonAsync("/api/inventory/movements/offcut", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStock_ReturnsNotFound_WhenMaterialMissing()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetStockQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockLevelResponse>.NotFound("Not found"));

        var client = CreateAuthenticatedClient(mediatorMock);
        var response = await client.GetAsync("/api/inventory/stock?materialType=NONEXISTENT");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
