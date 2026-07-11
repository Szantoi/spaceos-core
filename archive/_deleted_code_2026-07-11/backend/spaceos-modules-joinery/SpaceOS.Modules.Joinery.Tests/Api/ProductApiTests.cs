using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// HTTP integration tests for the /api/products/configure and /api/work-orders resources.
/// Tests the new configurator flow: configure → work order → PDF.
/// </summary>
[Collection("Integration")]
public sealed class ProductApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProductApiTests(JoineryWebFactory factory)
    {
        _factory = factory;
        SeedProductTemplates();
    }

    private void SeedProductTemplates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.JoineryDbContext>();

        if (!db.ProductTemplates.Any())
        {
            db.ProductTemplates.Add(new ProductTemplate
            {
                Id = "standard_door",
                Name = "Standard beltéri ajtó",
                Category = "doors",
                DimensionRules = "{\"minWidth\": 700, \"maxWidth\": 1100, \"minHeight\": 1900, \"maxHeight\": 2200, \"allowedThickness\": [40, 45]}",
                AllowedMaterials = "[{\"id\": \"chipboard_18mm\", \"name\": \"Forgácslap 18mm\", \"type\": \"core\", \"unitPrice\": 8500},{\"id\": \"oak_veneer\", \"name\": \"Tölgy furnér\", \"type\": \"veneer\", \"unitPrice\": 5200},{\"id\": \"pvc_edge_2mm\", \"name\": \"PVC élzáró 2mm\", \"type\": \"edge\", \"unitPrice\": 450}]",
                AllowedFittings = "[{\"id\": \"hidden_3d\", \"name\": \"Rejtett 3D zsanér\", \"category\": \"hinge\", \"unitPrice\": 1200},{\"id\": \"modern_steel\", \"name\": \"Modern acél kilincs\", \"category\": \"handle\", \"unitPrice\": 4500},{\"id\": \"standard_cylinder\", \"name\": \"Standard henger zár\", \"category\": \"lock\", \"unitPrice\": 3200}]",
                PricingRules = "{\"laborRate\": 5000, \"marginPercent\": 15, \"setupCost\": 2000}",
                LeadTimeDays = 7
            });
            db.SaveChanges();
        }
    }

    private HttpClient Client(string? tenantId = null, string tenantType = "Manufacturer") =>
        _factory.CreateAuthenticatedClient(tenantId ?? Guid.NewGuid().ToString(), tenantType);

    private static object ConfigureProductBody() => new
    {
        productType = "standard_door",
        dimensions = new
        {
            width = 900,
            height = 2100,
            thickness = 40
        },
        materials = new
        {
            core = "chipboard_18mm",
            veneer = "oak_veneer",
            edge = "pvc_edge_2mm"
        },
        fittings = new
        {
            hinge = "hidden_3d",
            handle = "modern_steel",
            @lock = "standard_cylinder"
        }
    };

    // ─── Configure Product Tests ─────────────────────────────────────────────

    [Fact]
    public async Task ConfigureProduct_Valid_ReturnsOkWithConfigId()
    {
        // Arrange
        var client = Client();

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var configId = body.GetProperty("configId").GetString();
        Guid.TryParse(configId, out var guidValue).Should().BeTrue();
        guidValue.Should().NotBe(Guid.Empty);
        body.GetProperty("estimatedPrice").GetDecimal().Should().BeGreaterThan(0);
        body.GetProperty("bomPreview").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConfigureProduct_InvalidProductType_ReturnsNotFound()
    {
        // Arrange
        var client = Client();
        var body = new
        {
            productType = "unknown_door_type",
            dimensions = new { width = 900, height = 2100, thickness = 40 },
            materials = new { core = "chipboard_18mm", veneer = "oak_veneer", edge = "pvc_edge_2mm" },
            fittings = new { hinge = "hidden_3d", handle = "modern_steel", @lock = "standard_cylinder" }
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfigureProduct_InvalidDimensions_ReturnsBadRequest()
    {
        // Arrange
        var client = Client();
        var body = new
        {
            productType = "standard_door",
            dimensions = new { width = 500, height = 2100, thickness = 40 }, // 500 < 700 min
            materials = new { core = "chipboard_18mm", veneer = "oak_veneer", edge = "pvc_edge_2mm" },
            fittings = new { hinge = "hidden_3d", handle = "modern_steel", @lock = "standard_cylinder" }
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfigureProduct_InvalidMaterial_ReturnsBadRequest()
    {
        // Arrange
        var client = Client();
        var body = new
        {
            productType = "standard_door",
            dimensions = new { width = 900, height = 2100, thickness = 40 },
            materials = new { core = "unknown_material", veneer = "oak_veneer", edge = "pvc_edge_2mm" },
            fittings = new { hinge = "hidden_3d", handle = "modern_steel", @lock = "standard_cylinder" }
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfigureProduct_NoAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // No auth

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── BOM Calculation Tests ───────────────────────────────────────────────

    [Fact]
    public async Task ConfigureProduct_BomContainsAllExpectedItems()
    {
        // Arrange
        var client = Client();

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var bom = body.GetProperty("bomPreview");

        // Should have core, veneer, edge, hinge, handle, lock = 6 items
        bom.GetArrayLength().Should().Be(6);

        // Verify item types are present
        var itemTypes = bom.EnumerateArray()
            .Select(i => i.GetProperty("itemType").GetString())
            .ToList();
        itemTypes.Should().Contain("material");
        itemTypes.Should().Contain("veneer");
        itemTypes.Should().Contain("edge");
        itemTypes.Should().Contain("fitting");
    }

    [Fact]
    public async Task ConfigureProduct_VeneerAreaCalculatedCorrectly()
    {
        // Arrange
        var client = Client();

        // Act
        var resp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());

        // Assert
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var veneer = body.GetProperty("bomPreview").EnumerateArray()
            .FirstOrDefault(i => i.GetProperty("itemType").GetString() == "veneer");

        veneer.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        var quantity = veneer.GetProperty("quantity").GetDecimal();
        // Expected: (0.9m * 2.1m) * 2 = 3.78 m²
        quantity.Should().BeApproximately(3.78m, 0.01m);
    }

    // ─── Tenant Isolation Tests ──────────────────────────────────────────────

    [Fact]
    public async Task ConfigureProduct_DifferentTenants_IsolateConfigurations()
    {
        // Arrange
        var tenant1 = Guid.NewGuid().ToString();
        var tenant2 = Guid.NewGuid().ToString();
        var client1 = Client(tenant1);
        var client2 = Client(tenant2);

        // Act - both tenants configure
        var resp1 = await client1.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());
        var resp2 = await client2.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());

        // Assert - both should succeed independently
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var body2 = await resp2.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        body1.GetProperty("configId").GetString()
            .Should().NotBe(body2.GetProperty("configId").GetString());
    }

    // ─── Work Order Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkOrder_ValidConfigId_ReturnsCreated()
    {
        // Arrange
        var client = Client();

        // First, configure a product
        var configResp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());
        configResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var configBody = await configResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var configId = configBody.GetProperty("configId").GetString();

        var workOrderBody = new
        {
            configId,
            quantity = 5,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd"),
            customerRef = "CUST-12345",
            notes = "Test work order"
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/work-orders", workOrderBody);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var workOrderId = body.GetProperty("workOrderId").GetString();
        Guid.TryParse(workOrderId, out var guidValue).Should().BeTrue();
        body.GetProperty("pdfUrl").GetString().Should().Contain("/api/work-orders/");
        body.GetProperty("bomItems").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("totalCost").GetDecimal().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateWorkOrder_InvalidConfigId_ReturnsNotFound()
    {
        // Arrange
        var client = Client();
        var fakeConfigId = Guid.NewGuid().ToString();
        var workOrderBody = new
        {
            configId = fakeConfigId,
            quantity = 5,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd"),
            customerRef = "CUST-12345"
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/work-orders", workOrderBody);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_BomItemsMultipliedByQuantity()
    {
        // Arrange
        var client = Client();

        // First, configure a product
        var configResp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());
        var configBody = await configResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var configId = configBody.GetProperty("configId").GetString();
        var singleBomItem = configBody.GetProperty("bomPreview").EnumerateArray().First();
        var singleQuantity = singleBomItem.GetProperty("quantity").GetDecimal();

        var quantity = 3;
        var workOrderBody = new
        {
            configId,
            quantity,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd")
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/work-orders", workOrderBody);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var bomItems = body.GetProperty("bomItems").EnumerateArray();
        var firstItem = bomItems.First();
        var workOrderQuantity = firstItem.GetProperty("quantity").GetDecimal();

        // Quantity should be multiplied
        workOrderQuantity.Should().Be(singleQuantity * quantity);
    }

    [Fact]
    public async Task CreateWorkOrder_NoAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // No auth
        var workOrderBody = new
        {
            configId = Guid.NewGuid().ToString(),
            quantity = 5,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd")
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/work-orders", workOrderBody);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Work Order PDF Tests ────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkOrderPdf_ValidId_ReturnsPdf()
    {
        // Arrange
        var client = Client();

        // Create configuration
        var configResp = await client.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());
        var configBody = await configResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var configId = configBody.GetProperty("configId").GetString();

        // Create work order
        var workOrderBody = new
        {
            configId,
            quantity = 2,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd")
        };
        var woResp = await client.PostAsJsonAsync("/api/work-orders", workOrderBody);
        var woBody = await woResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var workOrderId = woBody.GetProperty("workOrderId").GetString();

        // Act
        var pdfResp = await client.GetAsync($"/api/work-orders/{workOrderId}/sheet.pdf");

        // Assert
        pdfResp.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResp.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdfResp.Content.ReadAsByteArrayAsync();
        pdfBytes.Length.Should().BeGreaterThan(0);
        // PDF signature check
        pdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
    }

    [Fact]
    public async Task GetWorkOrderPdf_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var client = Client();
        var fakeWorkOrderId = Guid.NewGuid();

        // Act
        var resp = await client.GetAsync($"/api/work-orders/{fakeWorkOrderId}/sheet.pdf");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrderPdf_DifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var tenant1 = Guid.NewGuid().ToString();
        var tenant2 = Guid.NewGuid().ToString();
        var client1 = Client(tenant1);
        var client2 = Client(tenant2);

        // Create work order as tenant1
        var configResp = await client1.PostAsJsonAsync("/api/products/configure", ConfigureProductBody());
        var configBody = await configResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var configId = configBody.GetProperty("configId").GetString();

        var workOrderBody = new
        {
            configId,
            quantity = 1,
            deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)).ToString("yyyy-MM-dd")
        };
        var woResp = await client1.PostAsJsonAsync("/api/work-orders", workOrderBody);
        var woBody = await woResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var workOrderId = woBody.GetProperty("workOrderId").GetString();

        // Act - tenant2 tries to access tenant1's work order
        var resp = await client2.GetAsync($"/api/work-orders/{workOrderId}/sheet.pdf");

        // Assert - RLS should block access
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrderPdf_NoAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // No auth
        var fakeWorkOrderId = Guid.NewGuid();

        // Act
        var resp = await client.GetAsync($"/api/work-orders/{fakeWorkOrderId}/sheet.pdf");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
