using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// Integration tests for DELETE /internal/orders/by-tenant/{tenantId}
/// Tests: header guard, confirm param, allowlist, happy path.
/// </summary>
[Collection("Integration")]
public sealed class InternalDeleteByTenantTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;

    public InternalDeleteByTenantTests(JoineryWebFactory factory) => _factory = factory;

    private HttpClient InternalClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");
        return client;
    }

    private static string AllowedTenantId => "11111111-1111-1111-1111-111111111111";

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> FactoryWithAllowlist(string tenantId)
        => _factory.WithWebHostBuilder(b => b.UseSetting("TEST_TENANT_ALLOWLIST", tenantId));

    // ── 1. Missing X-SpaceOS-Internal header → 403 ───────────────────────────

    [Fact]
    public async Task Delete_WithoutInternalHeader_Returns403()
    {
        var client = _factory.CreateClient(); // no internal header
        var resp = await client.DeleteAsync(
            $"/internal/orders/by-tenant/{AllowedTenantId}?confirm=true");

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
            $"/internal/orders/by-tenant/{AllowedTenantId}"); // no ?confirm=true

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 3. Tenant not in allowlist → 403 ─────────────────────────────────────

    [Fact]
    public async Task Delete_TenantNotInAllowlist_Returns403()
    {
        // Allowlist contains a DIFFERENT tenant
        var factory = FactoryWithAllowlist("99999999-9999-9999-9999-999999999999");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/orders/by-tenant/{AllowedTenantId}?confirm=true");

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

        // Seed one DoorOrder for the tenant
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
            var order = DoorOrder.Create(tenantId, "PRJ-DEL", "Delete Test", Guid.NewGuid()).Value;
            var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
            order.AddItem(DoorItem.Create(order.Id, "D01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
            db.DoorOrders.Add(order);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/orders/by-tenant/{AllowedTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<DeleteResponse>();
        body.Should().NotBeNull();
        body!.TenantId.Should().Be(AllowedTenantId);
        body.DeletedCounts.DoorOrders.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── 5. Happy path with no data → 200 with zeros ──────────────────────────

    [Fact]
    public async Task Delete_NoExistingData_Returns200WithZeroCounts()
    {
        var emptyTenantId = Guid.NewGuid().ToString();
        var factory = FactoryWithAllowlist(emptyTenantId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", "true");

        var resp = await client.DeleteAsync(
            $"/internal/orders/by-tenant/{emptyTenantId}?confirm=true");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<DeleteResponse>();
        body!.DeletedCounts.DoorOrders.Should().Be(0);
        body.DeletedCounts.CuttingListSnapshots.Should().Be(0);
    }

    // ── DTOs for response deserialization ─────────────────────────────────────

    private sealed class DeleteResponse
    {
        public string TenantId { get; set; } = string.Empty;
        public DeletedCountsDto DeletedCounts { get; set; } = new();
    }

    private sealed class DeletedCountsDto
    {
        public int DoorOrders { get; set; }
        public int CuttingListSnapshots { get; set; }
    }
}
