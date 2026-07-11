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
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Api.Endpoints;
using SpaceOS.Modules.Inventory.Application.Handlers;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

public class InventoryReservationEndpointTests
{
    private static readonly Guid TenantId       = Guid.NewGuid();
    private static readonly Guid CorrelationId  = Guid.NewGuid();
    private static readonly Guid StockItemId    = Guid.NewGuid();

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

        var client = (app.Services.GetRequiredService<IServer>() as TestServer)!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    private static ReservationDto MakeDto() => new(
        Guid.NewGuid(), TenantId, CorrelationId, "cutting",
        null, null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4),
        SpaceOS.Modules.Contracts.Inventory.Enums.ReservationStatus.Active,
        new List<ReservationItemDto> { new(Guid.NewGuid(), StockItemId, "MDF18mm", 2m, 0m) });

    private static object ValidReserveBody() => new
    {
        correlationId  = CorrelationId,
        consumerModule = "cutting",
        consumerContextJson = (string?)null,
        createdByUserId = (Guid?)null,
        items = new[] { new { stockItemId = StockItemId, materialCode = "MDF18mm", quantity = 2m } },
        ttl = "04:00:00"
    };

    // ── POST /reservations ────────────────────────────────────────────────────

    [Fact]
    public async Task PostReservation_Success_Returns201()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReservationDto>.Success(MakeDto()));

        var response = await CreateAuthenticatedClient(m)
            .PostAsJsonAsync("/api/inventory/reservations", ValidReserveBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostReservation_DuplicateCorrelationId_HandlerReturnsSuccess_Returns201()
    {
        // Handler is idempotent — returns existing reservation on duplicate
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReservationDto>.Success(MakeDto()));

        var client = CreateAuthenticatedClient(m);
        await client.PostAsJsonAsync("/api/inventory/reservations", ValidReserveBody());
        var response = await client.PostAsJsonAsync("/api/inventory/reservations", ValidReserveBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        m.Verify(x => x.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PostReservation_NoAuth_Returns401()
    {
        var m = new Mock<IMediator>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(m.Object);
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

        var client = (app.Services.GetRequiredService<IServer>() as TestServer)!.CreateClient();
        var response = await client.PostAsJsonAsync("/api/inventory/reservations", ValidReserveBody());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DELETE /reservations/{correlationId} ──────────────────────────────────

    [Fact]
    public async Task DeleteReservation_Found_Returns200()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReleaseReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var response = await CreateAuthenticatedClient(m)
            .DeleteAsync($"/api/inventory/reservations/{CorrelationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReservation_NotFound_Returns404()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReleaseReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFound("Reservation not found."));

        var response = await CreateAuthenticatedClient(m)
            .DeleteAsync($"/api/inventory/reservations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /reservations ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetReservations_Returns200WithList()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetReservationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ReservationDto>>.Success(
                new List<ReservationDto> { MakeDto() }));

        var response = await CreateAuthenticatedClient(m)
            .GetAsync("/api/inventory/reservations?consumerModule=cutting");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
