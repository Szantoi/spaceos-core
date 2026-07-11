using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Application.Handlers;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Services;
using SpaceOS.Modules.Inventory.Infrastructure.Handlers;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Application;

/// <summary>
/// Handler tests for reserve, release, and get-reservations.
/// Use InMemory EF Core provider — no PostgreSQL required.
/// </summary>
public class ReserveStockCommandHandlerTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly IModuleRegistry _registry = new HardcodedModuleRegistry();
    private readonly ConsumerContextValidator _validator = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // Reusable stock item seeded in InMemory DB
    private Guid _stockItemId = Guid.Empty;

    public ReserveStockCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<Guid> SeedPanelStockAsync()
    {
        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        _db.MaterialCatalogs.Add(catalog);

        var stock = PanelStock.Create(_tenantId, catalog.Id, 2800, 2070, StockType.FullPanel, 50, "A1");
        _db.PanelStocks.Add(stock);
        await _db.SaveChangesAsync();
        return stock.Id;
    }

    private ReserveStockCommandHandler BuildHandler() =>
        new(_db, _registry, _validator, NullLogger<ReserveStockCommandHandler>.Instance);

    private ReleaseReservationCommandHandler BuildReleaseHandler() =>
        new(_db, NullLogger<ReleaseReservationCommandHandler>.Instance);

    private GetReservationsQueryHandler BuildGetHandler() =>
        new(_db);

    private ReserveStockCommand BuildCommand(
        Guid? tenantId = null,
        Guid? correlationId = null,
        string module = "Cutting",
        string? contextJson = null,
        Guid? stockItemId = null) =>
        new(
            tenantId ?? _tenantId,
            correlationId ?? Guid.NewGuid(),
            module,
            contextJson,
            null,
            [(stockItemId ?? _stockItemId, "MDF-18", 5m)],
            TimeSpan.FromHours(4));

    // ------------------------------------------------------------------
    // ReserveStock — happy path
    // ------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsSuccessAndCreatesReservation()
    {
        _stockItemId = await SeedPanelStockAsync();
        var handler = BuildHandler();

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.Status.Should().Be(SpaceOS.Modules.Contracts.Inventory.Enums.ReservationStatus.Active);
        result.Value.Items.Should().HaveCount(1);

        // Verify persisted
        var stored = await _db.Reservations.Include(r => r.Items).FirstOrDefaultAsync();
        stored.Should().NotBeNull();
        stored!.Items.Should().HaveCount(1);
    }

    // ------------------------------------------------------------------
    // ReserveStock — unknown module
    // ------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithUnknownModule_ReturnsInvalid()
    {
        _stockItemId = await SeedPanelStockAsync();
        var handler = BuildHandler();

        var result = await handler.Handle(BuildCommand(module: "UnknownModule"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("UnknownModule"));
    }

    // ------------------------------------------------------------------
    // ReserveStock — XSS in context JSON
    // ------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithXssInContext_ReturnsInvalid()
    {
        _stockItemId = await SeedPanelStockAsync();
        var handler = BuildHandler();
        const string xssJson = """{"msg": "<script>alert(1)</script>"}""";

        var result = await handler.Handle(BuildCommand(contextJson: xssJson), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("XSS"));
    }

    // ------------------------------------------------------------------
    // ReserveStock — stock item not found
    // ------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithMissingStockItem_ReturnsNotFound()
    {
        // Do NOT seed any stock — StockItemId will not resolve
        _stockItemId = Guid.NewGuid();
        var handler = BuildHandler();

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ------------------------------------------------------------------
    // ReserveStock — idempotency: second call returns existing
    // ------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithDuplicateActiveCorrelation_ReturnsExistingReservation()
    {
        _stockItemId = await SeedPanelStockAsync();
        var handler = BuildHandler();
        var correlationId = Guid.NewGuid();

        var first = await handler.Handle(BuildCommand(correlationId: correlationId), CancellationToken.None);
        var second = await handler.Handle(BuildCommand(correlationId: correlationId), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        // Same reservation returned — IDs match
        second.Value.Id.Should().Be(first.Value.Id);

        // Only one record in DB
        var count = await _db.Reservations.CountAsync();
        count.Should().Be(1);
    }

    // ------------------------------------------------------------------
    // ReleaseReservation — happy path
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReleaseReservationHandler_WithActiveReservation_ReleasesIt()
    {
        _stockItemId = await SeedPanelStockAsync();
        var reserveHandler = BuildHandler();
        var releaseHandler = BuildReleaseHandler();

        var correlationId = Guid.NewGuid();
        await reserveHandler.Handle(BuildCommand(correlationId: correlationId), CancellationToken.None);

        var result = await releaseHandler.Handle(
            new ReleaseReservationCommand(_tenantId, correlationId, "test release"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stored = await _db.Reservations.FirstOrDefaultAsync();
        stored!.Status.Should().Be(ReservationStatus.Released);
    }

    // ------------------------------------------------------------------
    // ReleaseReservation — no active reservation
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReleaseReservationHandler_WithNoActiveReservation_ReturnsNotFound()
    {
        var releaseHandler = BuildReleaseHandler();

        var result = await releaseHandler.Handle(
            new ReleaseReservationCommand(_tenantId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ------------------------------------------------------------------
    // GetReservations — empty filter is rejected
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetReservationsHandler_WithEmptyFilter_ReturnsInvalid()
    {
        var handler = BuildGetHandler();
        var emptyFilter = new ReservationFilter(null, null, null, null, null);

        var result = await handler.Handle(
            new GetReservationsQuery(_tenantId, emptyFilter),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle();
    }

    // ------------------------------------------------------------------
    // GetReservations — valid filter returns paginated results
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetReservationsHandler_WithValidFilter_ReturnsPaginatedResults()
    {
        _stockItemId = await SeedPanelStockAsync();
        var reserveHandler = BuildHandler();
        var getHandler = BuildGetHandler();

        // Create 3 reservations for the same module
        for (var i = 0; i < 3; i++)
        {
            await reserveHandler.Handle(BuildCommand(), CancellationToken.None);
        }

        var filter = new ReservationFilter(ConsumerModule: "Cutting", null, null, null, null, Skip: 0, Take: 2);
        var result = await getHandler.Handle(
            new GetReservationsQuery(_tenantId, filter),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // paginated to Take=2
    }

    public void Dispose() => _db.Dispose();
}
