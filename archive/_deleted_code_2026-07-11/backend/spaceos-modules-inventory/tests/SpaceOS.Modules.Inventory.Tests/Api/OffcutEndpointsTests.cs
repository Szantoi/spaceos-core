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
using SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;
using SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;
using SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutDetail;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutList;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutStatsSummary;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

public class OffcutEndpointsTests
{
    private static readonly Guid OffcutId     = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();
    private static readonly Guid JobId         = Guid.NewGuid();

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

    // ── GET /offcuts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_Returns200WithPagedResult()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetOffcutListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetOffcutListResponse>.Success(
                new GetOffcutListResponse(new List<OffcutListItem>(), 0, 1, 20)));

        var response = await CreateClient(m).GetAsync("/api/inventory/offcuts/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetList_FilterByStatus_PassesFilterToQuery()
    {
        GetOffcutListQuery? captured = null;
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetOffcutListQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<GetOffcutListResponse>>, CancellationToken>((q, _) =>
                captured = q as GetOffcutListQuery)
            .ReturnsAsync(Result<GetOffcutListResponse>.Success(
                new GetOffcutListResponse(new List<OffcutListItem>(), 0, 1, 20)));

        await CreateClient(m).GetAsync("/api/inventory/offcuts/?status=Available&materialCode=MDF18mm");
        captured!.Status.Should().Be("Available");
        captured.MaterialCode.Should().Be("MDF18mm");
    }

    // ── GET /offcuts/stats/summary ────────────────────────────────────────────

    [Fact]
    public async Task GetStats_Returns200WithAggregation()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetOffcutStatsSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetOffcutStatsSummaryResponse>.Success(
                new GetOffcutStatsSummaryResponse(
                    0.5m, 375m,
                    new Dictionary<string, MaterialOffcutStats> { ["MDF18mm"] = new(0.5m, 375m) },
                    2, 10, 1)));

        var response = await CreateClient(m).GetAsync("/api/inventory/offcuts/stats/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /offcuts/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetail_ExistingOffcut_Returns200()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetOffcutDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetOffcutDetailResponse>.Success(
                new GetOffcutDetailResponse(OffcutId, "MDF18mm", 500, 400, 18, 0.0036m, 2.7m,
                    "Available", DateTime.UtcNow, null, null, null,
                    new List<ReservationHistoryItem>())));

        var response = await CreateClient(m).GetAsync($"/api/inventory/offcuts/{OffcutId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDetail_NotFound_Returns404()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetOffcutDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetOffcutDetailResponse>.NotFound("not found"));

        var response = await CreateClient(m).GetAsync($"/api/inventory/offcuts/{OffcutId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /reserve ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Reserve_Success_Returns201()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReserveOffcutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReserveOffcutResponse>.Success(
                new ReserveOffcutResponse(ReservationId, DateTime.UtcNow.AddDays(7))));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/reserve", new { jobId = JobId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Reserve_NotFound_Returns404()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReserveOffcutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReserveOffcutResponse>.NotFound("not found"));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/reserve", new { jobId = JobId });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reserve_Conflict_Returns409()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ReserveOffcutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReserveOffcutResponse>.Conflict("not available"));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/reserve", new { jobId = JobId });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /approve-reservation ─────────────────────────────────────────────

    [Fact]
    public async Task Approve_Success_Returns200()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ApproveOffcutReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApproveOffcutReservationResponse>.Success(
                new ApproveOffcutReservationResponse("Approved")));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/approve-reservation",
            new { reservationId = ReservationId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_Expired_Returns410()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<ApproveOffcutReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApproveOffcutReservationResponse>.Error("Reservation has expired."));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/approve-reservation",
            new { reservationId = ReservationId });
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    // ── POST /use ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Use_Success_Returns200()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<UseOffcutInJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UseOffcutInJobResponse>.Success(
                new UseOffcutInJobResponse("Used", JobId, DateTime.UtcNow)));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/use", new { jobId = JobId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Use_NotReserved_Returns409()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<UseOffcutInJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UseOffcutInJobResponse>.Conflict("not reserved"));

        var response = await CreateClient(m).PostAsJsonAsync(
            $"/api/inventory/offcuts/{OffcutId}/use", new { jobId = JobId });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
