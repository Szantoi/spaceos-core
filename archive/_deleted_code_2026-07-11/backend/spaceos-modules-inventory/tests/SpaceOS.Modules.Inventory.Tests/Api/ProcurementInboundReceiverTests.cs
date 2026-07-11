using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

/// <summary>
/// Integration tests for POST /internal/inbound (Procurement → Inventory receiver).
/// Covers: Bearer auth, loopback guard, TenantId mismatch, idempotency,
/// unknown materialCode (422), and happy-path stock mutation.
/// </summary>
public sealed class ProcurementInboundReceiverTests : IClassFixture<InventoryWebFactory>
{
    private readonly InventoryWebFactory _factory;

    private static readonly Guid TenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DeliveryLineId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SupplierId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string KnownMaterialCode = "MDF 18mm";
    private const string ValidSecret = "test-secret-value";

    public ProcurementInboundReceiverTests(InventoryWebFactory factory)
    {
        _factory = factory;
        // EnsureCreated applies HasData seed on InMemory provider (migrations don't run in tests)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Database.EnsureCreated();
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> FactoryWithSecret(string secret = ValidSecret)
        => _factory.WithWebHostBuilder(b => b.UseSetting("SPACEOS_INTERNAL_SECRET", secret));

    private static ProcurementInboundPayload ValidPayload(Guid? deliveryLineId = null) => new(
        TenantId: TenantId,
        DeliveryLineId: deliveryLineId ?? DeliveryLineId,
        MaterialCode: KnownMaterialCode,
        Quantity: 3.0m,
        UnitOfMeasure: "pcs",
        SupplierId: SupplierId,
        ReceivedAt: DateTimeOffset.UtcNow);

    // ── 1. Missing Bearer token → 401 ────────────────────────────────────────

    [Fact]
    public async Task Post_NoBearer_Returns401()
    {
        var factory = FactoryWithSecret();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/internal/inbound", ValidPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 2. Wrong Bearer token → 401 ──────────────────────────────────────────

    [Fact]
    public async Task Post_WrongBearer_Returns401()
    {
        var factory = FactoryWithSecret();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer wrong-secret");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", TenantId.ToString());

        var resp = await client.PostAsJsonAsync("/internal/inbound", ValidPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 3. TenantId header/body mismatch → 403 ───────────────────────────────

    [Fact]
    public async Task Post_TenantIdMismatch_Returns403()
    {
        var factory = FactoryWithSecret();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", Guid.NewGuid().ToString()); // different from body

        var resp = await client.PostAsJsonAsync("/internal/inbound", ValidPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 4. Unknown materialCode → 422 ────────────────────────────────────────

    [Fact]
    public async Task Post_UnknownMaterialCode_Returns422()
    {
        var factory = FactoryWithSecret();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", TenantId.ToString());

        var payload = ValidPayload() with { MaterialCode = "UNKNOWN_MATERIAL_XYZ" };
        var resp = await client.PostAsJsonAsync("/internal/inbound", payload);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── 5. Happy path — stock created and inbox record inserted → 200 ─────────

    [Fact]
    public async Task Post_ValidRequest_Returns200AndCreatesStockMovement()
    {
        var factory = FactoryWithSecret();
        var uniqueDelivery = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", TenantId.ToString());

        var resp = await client.PostAsJsonAsync("/internal/inbound", ValidPayload(uniqueDelivery));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("\"processed\":true");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // Inbox record created
        var inboxExists = db.InventoryInboundInboxes
            .Any(x => x.TenantId == TenantId && x.DeliveryLineId == uniqueDelivery);
        inboxExists.Should().BeTrue();

        // StockMovement recorded
        var movement = db.StockMovements
            .FirstOrDefault(m => m.TenantId == TenantId && m.Reference.Contains(uniqueDelivery.ToString()));
        movement.Should().NotBeNull();
        movement!.MovementType.Should().Be(MovementType.Inbound);
    }

    // ── 6. Duplicate delivery → 200 idempotent (not 409, not 5xx) ────────────

    [Fact]
    public async Task Post_DuplicateDeliveryLine_Returns200Idempotent()
    {
        var factory = FactoryWithSecret();
        var uniqueDelivery = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", TenantId.ToString());

        var payload = ValidPayload(uniqueDelivery);

        // First call — process
        var first = await client.PostAsJsonAsync("/internal/inbound", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second call — duplicate → still 200
        var second = await client.PostAsJsonAsync("/internal/inbound", payload);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("\"reason\":\"duplicate\"");

        // Exactly one inbox record
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var count = db.InventoryInboundInboxes
            .Count(x => x.TenantId == TenantId && x.DeliveryLineId == uniqueDelivery);
        count.Should().Be(1);
    }

    // ── 7. Existing PanelStock gets AddQuantity (not duplicate Create) ─────────

    [Fact]
    public async Task Post_ExistingStock_AddsQuantityToExistingRecord()
    {
        var factory = FactoryWithSecret();
        var tenantId = Guid.NewGuid();
        var materialCatalogId = new Guid("10000000-0000-0000-0000-000000000001");

        // Pre-seed a PanelStock for this tenant
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.PanelStocks.Add(PanelStock.Create(tenantId, materialCatalogId, 2800m, 2070m, StockType.FullPanel, 10, "EXISTING"));
            await db.SaveChangesAsync();
        }

        var client = factory.WithWebHostBuilder(b => b.UseSetting("SPACEOS_INTERNAL_SECRET", ValidSecret)).CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-SpaceOS-TenantId", tenantId.ToString());

        var payload = new ProcurementInboundPayload(
            TenantId: tenantId,
            DeliveryLineId: Guid.NewGuid(),
            MaterialCode: KnownMaterialCode,
            Quantity: 5.0m,
            UnitOfMeasure: "pcs",
            SupplierId: SupplierId,
            ReceivedAt: DateTimeOffset.UtcNow);

        var resp = await client.PostAsJsonAsync("/internal/inbound", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var stock = verifyDb.PanelStocks
            .FirstOrDefault(s => s.TenantId == tenantId && s.MaterialCatalogId == materialCatalogId);
        stock.Should().NotBeNull();
        stock!.Quantity.Should().Be(15); // 10 existing + 5 new
    }

    // ── 8. Missing X-SpaceOS-TenantId header → 403 ───────────────────────────

    [Fact]
    public async Task Post_MissingTenantIdHeader_Returns403()
    {
        var factory = FactoryWithSecret();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ValidSecret}");
        // No X-SpaceOS-TenantId header

        var resp = await client.PostAsJsonAsync("/internal/inbound", ValidPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ProcurementInboundPayload(
        Guid TenantId,
        Guid DeliveryLineId,
        string MaterialCode,
        decimal Quantity,
        string UnitOfMeasure,
        Guid SupplierId,
        DateTimeOffset ReceivedAt);
}
