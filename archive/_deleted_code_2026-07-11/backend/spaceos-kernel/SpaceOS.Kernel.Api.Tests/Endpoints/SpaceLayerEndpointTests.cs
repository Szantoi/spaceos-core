// SpaceOS.Kernel.Api.Tests/Endpoints/SpaceLayerEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.SpaceLayers;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the SpaceLayer GET and write endpoints.</summary>
public sealed class SpaceLayerEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public SpaceLayerEndpointTests()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateAuthorizedClient();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _factory.SeedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task GetSpaceLayerById_Returns200()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("Layer Tenant");
        var facility = Facility.Create("Layer Facility", ApiFactory.TestTenantId);
        var spaceLayer = SpaceLayer.CreateLocalLayer(
            intentDataJson: """{"walls": []}""",
            facilityId: facility.Id,
            tradeType: TradeType.Architecture,
            tenantId: ApiFactory.TestTenantId);

        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.SpaceLayers.Add(spaceLayer);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync($"/api/space-layers/{spaceLayer.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<SpaceLayerDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(spaceLayer.Id.Value, dto.Id);
        Assert.Equal(TradeType.Architecture, dto.TradeType);
    }

    [Fact]
    public async Task GetSpaceLayerById_UnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/space-layers/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSpaceLayersByFacility_DefaultPaging_Returns200WithPagedResult()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("Layer List Tenant");
        var facility = Facility.Create("Layer List Facility", ApiFactory.TestTenantId);
        var layer1 = SpaceLayer.CreateLocalLayer("""{"walls":[]}""", facility.Id, TradeType.Architecture, ApiFactory.TestTenantId);
        var layer2 = SpaceLayer.CreateLocalLayer("""{"pipes":[]}""", facility.Id, TradeType.Plumbing, ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.SpaceLayers.AddRange(layer1, layer2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client
            .GetAsync($"/api/facilities/{facility.Id.Value}/space-layers", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<SpaceLayerDto>>(TestJsonOptions.Default, TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task RegisterSpaceLayer_ValidRequest_Returns201()
    {
        // Arrange — facility TenantId must match TestTenantId so the create handler resolves the correct tenant.
        var tenant = Tenant.Create("Layer Owner Tenant");
        var facility = Facility.Create("Layer Owner Facility", ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            return Task.CompletedTask;
        });
        var request = new
        {
            tradeType = TradeType.Architecture,
            isExternalNode = false,
            externalSourceUrl = (string?)null,
            intentDataJson = """{"floorPlan":"A-101"}"""
        };

        // Act
        var response = await _client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    // -------------------------------------------------------------------------
    // T-06: PUT /api/space-layers/{id}/intent — 65 KB body → 413
    // The RequestBodySizeLimitFilter enforces 64 KB; Content-Length header triggers the check.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSpaceLayerIntent_BodyExceeds64Kb_Returns413()
    {
        // Arrange — seed a space layer to have a valid id to target
        var tenant  = Tenant.Create("Intent Size Tenant");
        var facility = Facility.Create("Intent Size Facility", ApiFactory.TestTenantId);
        var layer   = SpaceLayer.CreateLocalLayer(
            intentDataJson: """{"walls":[]}""",
            facilityId:     facility.Id,
            tradeType:      TradeType.Architecture,
            tenantId:       ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.SpaceLayers.Add(layer);
            return Task.CompletedTask;
        });

        // Build a JSON body whose Content-Length exceeds 64 KB (65 537 ASCII chars).
        // Wrapping in a valid JSON object ensures the Content-Type: application/json header is set
        // and the content-length is accurate so the filter can read it.
        var largeValue = new string('x', 65_537);
        var largeBody  = $$$"""{"intentDataJson":"{{{largeValue}}}"}""";
        using var content = new StringContent(largeBody, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync(
            $"/api/space-layers/{layer.Id.Value}/intent",
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // T-06: PUT /api/space-layers/{id}/intent — nested object in parameters → 422
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSpaceLayerIntent_NestedObjectInParameters_Returns422()
    {
        // Arrange — seed a space layer to have a valid id to target
        var tenant  = Tenant.Create("Intent Nested Tenant");
        var facility = Facility.Create("Intent Nested Facility", ApiFactory.TestTenantId);
        var layer   = SpaceLayer.CreateLocalLayer(
            intentDataJson: """{"walls":[]}""",
            facilityId:     facility.Id,
            tradeType:      TradeType.Architecture,
            tenantId:       ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.SpaceLayers.Add(layer);
            return Task.CompletedTask;
        });

        var request = new { intentDataJson = """{"parameters": {"nested": {"a": 1}}}""" };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/space-layers/{layer.Id.Value}/intent",
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // T-06: PUT /api/space-layers/{id}/intent — null parameters → passes validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSpaceLayerIntent_NullParameters_ReturnsSuccess()
    {
        // Arrange
        var tenant  = Tenant.Create("Intent Null Params Tenant");
        var facility = Facility.Create("Intent Null Params Facility", ApiFactory.TestTenantId);
        var layer   = SpaceLayer.CreateLocalLayer(
            intentDataJson: """{"walls":[]}""",
            facilityId:     facility.Id,
            tradeType:      TradeType.Architecture,
            tenantId:       ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.SpaceLayers.Add(layer);
            return Task.CompletedTask;
        });

        var request = new { intentDataJson = """{"floorPlan": "planA", "parameters": null}""", tradeType = TradeType.Architecture };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/space-layers/{layer.Id.Value}/intent",
            request,
            TestContext.Current.CancellationToken);

        // Assert — 200 OK (SpaceLayer not found → 404 possible if handler fails; but we seeded it above)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
