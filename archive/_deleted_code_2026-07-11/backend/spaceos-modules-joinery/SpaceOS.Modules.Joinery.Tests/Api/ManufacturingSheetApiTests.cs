using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// API integration tests for GET /api/orders/{id}/manufacturing-sheet.
/// 200 application/pdf, 401 unauthenticated, 404 not found.
/// </summary>
[Collection("Integration")]
public sealed class ManufacturingSheetApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ManufacturingSheetApiTests(JoineryWebFactory factory) => _factory = factory;

    private HttpClient Client(string? tenantId = null) =>
        _factory.CreateAuthenticatedClient(tenantId ?? Guid.NewGuid().ToString());

    // Helper: create an order and return its id
    private async Task<Guid> CreateOrderAsync(HttpClient client, string projectId)
    {
        var body = new
        {
            flowEpicId   = Guid.NewGuid(),
            projectId,
            projectName  = "Gyártásilap API Teszt",
            clientName   = "Teszt Kft.",
            clientAddress = "Budapest, Teszt u. 1.",
            clientPhone  = (string?)null,
            deliveryDate = (string?)null,
        };
        var resp = await client.PostAsJsonAsync("/api/orders", body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Guid>(_json);
    }

    [Fact]
    public async Task GetManufacturingSheet_NoAuth_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var orderId = Guid.NewGuid();

        var resp = await unauthClient.GetAsync($"/api/orders/{orderId}/manufacturing-sheet");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetManufacturingSheet_NonExistentOrder_Returns404()
    {
        var client = Client();
        var orderId = Guid.NewGuid();

        var resp = await client.GetAsync($"/api/orders/{orderId}/manufacturing-sheet");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetManufacturingSheet_DraftOrder_Returns200WithPdf()
    {
        var tenantId = Guid.NewGuid().ToString();
        var client = Client(tenantId);

        var orderId = await CreateOrderAsync(client, "MFG-API-001");

        var resp = await client.GetAsync($"/api/orders/{orderId}/manufacturing-sheet");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0, because: "a real PDF must have content");
        bytes.Should().StartWith(new byte[] { 0x25, 0x50, 0x44, 0x46 },
            because: "PDF files start with the %PDF magic bytes");
    }
}
