using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// HTTP integration tests for the /api/orders resource:
/// full CRUD flow + tenant isolation + domain rule enforcement via HTTP.
/// </summary>
[Collection("Integration")]
public sealed class OrdersApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;
    private static readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web);

    public OrdersApiTests(JoineryWebFactory factory) => _factory = factory;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private HttpClient Client(string? tenantId = null, string tenantType = "Manufacturer") =>
        _factory.CreateAuthenticatedClient(tenantId ?? Guid.NewGuid().ToString(), tenantType);

    private static object CreateOrderBody(string projectId = "E2E-001") => new
    {
        flowEpicId   = Guid.NewGuid(),
        projectId,
        projectName  = $"Integrációs Teszt {projectId}",
        clientName   = (string?)null,
        clientAddress = (string?)null,
        clientPhone  = (string?)null,
        deliveryDate = (string?)null,
    };

    private static object AddItemBody(string sorszam = "A001") => new
    {
        sorszam,
        name              = "Falcos ajtó",
        quantity          = 1,
        doorType          = "Falcos",
        openingDirection  = "Left",
        wallOpeningWidth  = 900m,
        doorWidth         = 860m,
        wallOpeningHeight = 2100m,
        doorHeight        = 2060m,
        wallOpeningThickness = 120m,
        doorThickness     = 40m,
    };

    // ─── 1. List orders — empty for new tenant ────────────────────────────────

    [Fact]
    public async Task ListOrders_NewTenant_ReturnsEmptyPage()
    {
        var resp = await Client().GetAsync("/api/orders");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
        body.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ─── 2. Create order — 201 with Location header ──────────────────────────

    [Fact]
    public async Task CreateOrder_Valid_Returns201WithId()
    {
        var resp = await Client().PostAsJsonAsync("/api/orders", CreateOrderBody());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await resp.Content.ReadFromJsonAsync<Guid>(_json);
        id.Should().NotBeEmpty();
    }

    // ─── 3. Create → Get by ID ───────────────────────────────────────────────

    [Fact]
    public async Task GetOrder_AfterCreate_ReturnsDraftStatus()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var createResp = await client.PostAsJsonAsync("/api/orders", CreateOrderBody("PROJ-GET"));
        var id = await createResp.Content.ReadFromJsonAsync<Guid>(_json);

        var getResp = await client.GetAsync($"/api/orders/{id}");

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>(_json);
        body.GetProperty("status").GetString().Should().Be("Draft");
        body.GetProperty("id").GetGuid().Should().Be(id);
    }

    // ─── 4. Create → List shows the order ────────────────────────────────────

    [Fact]
    public async Task ListOrders_AfterCreate_ReturnsOne()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        await client.PostAsJsonAsync("/api/orders", CreateOrderBody("LIST-TEST"));

        var resp = await client.GetAsync("/api/orders");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    // ─── 5. Add item → 201 ───────────────────────────────────────────────────

    [Fact]
    public async Task AddItem_ToDraftOrder_Returns201()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody()))
            .Content.ReadFromJsonAsync<Guid>(_json);

        var resp = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/items", AddItemBody());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var itemId = await resp.Content.ReadFromJsonAsync<Guid>(_json);
        itemId.Should().NotBeEmpty();
    }

    // ─── 6. Submit → 200, status becomes Submitted ───────────────────────────

    [Fact]
    public async Task SubmitOrder_WithItem_Returns200AndStatusSubmitted()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("SUBMIT-TEST")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());

        var submitResp = await client.PostAsync($"/api/orders/{orderId}/submit", null);

        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await (await client.GetAsync($"/api/orders/{orderId}"))
            .Content.ReadFromJsonAsync<JsonElement>(_json);
        order.GetProperty("status").GetString().Should().Be("Submitted");
    }

    // ─── 7. Submit empty order → domain error ────────────────────────────────

    [Fact]
    public async Task SubmitOrder_WithNoItems_ReturnsBadRequest()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("NO-ITEM")))
            .Content.ReadFromJsonAsync<Guid>(_json);

        var resp = await client.PostAsync($"/api/orders/{orderId}/submit", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── 8. Add item to submitted order → domain error (BE-04) ──────────────

    [Fact]
    public async Task AddItem_ToSubmittedOrder_ReturnsBadRequest()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("BE04")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());
        await client.PostAsync($"/api/orders/{orderId}/submit", null);

        // Try to add another item after submit
        var resp = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/items", AddItemBody("A002"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── 9. Tenant isolation — other tenant's order is 404 ───────────────────

    [Fact]
    public async Task GetOrder_BelongingToOtherTenant_Returns404()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();

        // Tenant A creates order
        var clientA = Client(tenantA);
        var orderId = await (await clientA.PostAsJsonAsync("/api/orders", CreateOrderBody("TENANT-A")))
            .Content.ReadFromJsonAsync<Guid>(_json);

        // Tenant B tries to GET it → repository filters by tenantId → not found
        var clientB = Client(tenantB);
        var resp = await clientB.GetAsync($"/api/orders/{orderId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "order belongs to tenant A — tenant B cannot see it (SEC-01)");
    }

    // ─── 10. Tenant isolation — list only shows own orders ───────────────────

    [Fact]
    public async Task ListOrders_TenantBSeesOnlyOwnOrders()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();

        var clientA = Client(tenantA);
        var clientB = Client(tenantB);

        // A creates 2 orders, B creates 1
        await clientA.PostAsJsonAsync("/api/orders", CreateOrderBody("A1"));
        await clientA.PostAsJsonAsync("/api/orders", CreateOrderBody("A2"));
        await clientB.PostAsJsonAsync("/api/orders", CreateOrderBody("B1"));

        var respB = await clientB.GetAsync("/api/orders");
        var body  = await respB.Content.ReadFromJsonAsync<JsonElement>(_json);

        body.GetProperty("totalCount").GetInt32().Should().Be(1,
            because: "tenant B created 1 order and should see only that");
    }

    // ─── 11. Cutting list — 200 (no rules → empty parts) ────────────────────

    [Fact]
    public async Task GetCuttingList_AfterCalculate_Returns200()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("CL-TEST")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());
        await client.PostAsync($"/api/orders/{orderId}/calculate", null);

        var resp = await client.GetAsync($"/api/orders/{orderId}/cutting-list");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.CacheControl?.NoStore.Should().BeTrue(
            because: "cutting list must never be cached (SEC-05)");
    }

    // ─── 12. Process plan — 200 ──────────────────────────────────────────────

    [Fact]
    public async Task GetProcessPlan_Returns200()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("PP-TEST")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());

        var resp = await client.GetAsync($"/api/orders/{orderId}/process-plan");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── 13. Hardware list — 200 ─────────────────────────────────────────────

    [Fact]
    public async Task GetHardwareList_Returns200()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("HW-TEST")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());

        var resp = await client.GetAsync($"/api/orders/{orderId}/hardware-list");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── 14. Material requirements — 200 ─────────────────────────────────────

    [Fact]
    public async Task GetMaterialReq_Returns200()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await (await client.PostAsJsonAsync("/api/orders", CreateOrderBody("MAT-TEST")))
            .Content.ReadFromJsonAsync<Guid>(_json);
        await client.PostAsJsonAsync($"/api/orders/{orderId}/items", AddItemBody());

        var resp = await client.GetAsync($"/api/orders/{orderId}/material-req");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── 15. Create with invalid body → 400 ──────────────────────────────────

    [Fact]
    public async Task CreateOrder_EmptyProjectId_ReturnsBadRequest()
    {
        var body = new
        {
            flowEpicId  = Guid.NewGuid(),
            projectId   = "",           // invalid: empty
            projectName = "Test",
        };

        var resp = await Client().PostAsJsonAsync("/api/orders", body);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
