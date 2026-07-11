using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

/// <summary>
/// Integration tests for DELETE /internal/panel-stocks/by-tenant/{tenantId}
/// Tests: header guard, confirm param, allowlist, happy path.
/// </summary>
public sealed class InternalDeleteByTenantTests : IClassFixture<InventoryWebFactory>
{
    private readonly InventoryWebFactory _factory;

    public InternalDeleteByTenantTests(InventoryWebFactory factory) => _factory = factory;

    private static string AllowedTenantId => "11111111-1111-1111-1111-111111111111";

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> FactoryWithAllowlist(string tenantId)
        => _factory.WithWebHostBuilder(b => b.UseSetting("TEST_TENANT_ALLOWLIST", tenantId));

    // ── 1. Missing X-SpaceOS-Internal header → 403 ───────────────────────────

    [Fact]
    public async Task Delete_WithoutInternalHeader_Returns403()
    {
        var client = _factory.CreateClient(); // no internal header
        var resp = await client.DeleteAsync(
            $"/internal/panel-stocks/by-tenant/{AllowedTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 2. Missing confirm=true → 400 ────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutConfirm_Returns400()
    {
        var factory = FactoryWithAllowlist(AllowedTenantId);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/panel-stocks/by-tenant/{AllowedTenantId}"); // no ?confirm=true

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 3. Tenant not in allowlist → 403 ─────────────────────────────────────

    [Fact]
    public async Task Delete_TenantNotInAllowlist_Returns403()
    {
        var factory = FactoryWithAllowlist("99999999-9999-9999-9999-999999999999");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/panel-stocks/by-tenant/{AllowedTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("Tenant not in test allowlist");
    }

    // ── 4. Happy path → 200 + deletedCounts ──────────────────────────────────

    [Fact]
    public async Task Delete_ValidRequest_Returns200WithCounts()
    {
        var tenantId = Guid.Parse(AllowedTenantId);
        var factory = FactoryWithAllowlist(AllowedTenantId);

        // Seed one PanelStock and one Offcut for the tenant
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var materialId = Guid.NewGuid();

            db.PanelStocks.Add(PanelStock.Create(
                tenantId, materialId, 2800m, 2070m, StockType.FullPanel, 5, "A1"));
            db.Offcuts.Add(Offcut.Register(
                tenantId, materialId, 500m, 300m, null));
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/panel-stocks/by-tenant/{AllowedTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<DeleteResponse>();
        body.Should().NotBeNull();
        body!.TenantId.Should().Be(AllowedTenantId);
        body.DeletedCounts.PanelStocks.Should().BeGreaterThanOrEqualTo(1);
        body.DeletedCounts.Offcuts.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── 5. No existing data → 200 with zeros ─────────────────────────────────

    [Fact]
    public async Task Delete_NoExistingData_Returns200WithZeroCounts()
    {
        var emptyTenantId = Guid.NewGuid().ToString();
        var factory = FactoryWithAllowlist(emptyTenantId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/panel-stocks/by-tenant/{emptyTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<DeleteResponse>();
        body!.DeletedCounts.PanelStocks.Should().Be(0);
        body.DeletedCounts.Offcuts.Should().Be(0);
        body.DeletedCounts.StockMovements.Should().Be(0);
    }

    // ── 6. Invalid GUID format → 400 ─────────────────────────────────────────

    [Fact]
    public async Task Delete_InvalidTenantIdFormat_Returns400()
    {
        var factory = FactoryWithAllowlist("not-a-guid");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            "/internal/panel-stocks/by-tenant/not-a-guid?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid tenantId format");
    }

    // ── DTOs for response deserialization ─────────────────────────────────────

    private sealed class DeleteResponse
    {
        public string TenantId { get; set; } = string.Empty;
        public DeletedCountsDto DeletedCounts { get; set; } = new();
    }

    private sealed class DeletedCountsDto
    {
        public int PanelStocks { get; set; }
        public int Offcuts { get; set; }
        public int StockMovements { get; set; }
    }
}
