// SpaceOS.Kernel.IntegrationTests/SpaceLayers/SpaceLayerPipelineTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Application.SpaceLayers;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.SpaceLayers;

/// <summary>
/// End-to-end pipeline tests for the SpaceLayer aggregate.
/// Verifies the full vertical slice: HTTP → ValidationBehavior → Handler → EF Core → domain event dispatch.
/// SpaceLayer is created via the nested route POST /api/facilities/{facilityId}/space-layers.
/// </summary>
public sealed class SpaceLayerPipelineTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/space-layers — local layer happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid local-layer POST returns 201 Created with a location header.
    /// </summary>
    [Fact]
    public async Task RegisterSpaceLayer_LocalLayer_Returns201WithLocationHeader()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "SL Owner Facility")
            ;
        var request = new
        {
            TradeType = (int)TradeType.Architecture,
            IsExternalNode = false,
            ExternalSourceUrl = (string?)null,
            IntentDataJson = "{\"floorPlan\":\"A-101\"}"
        };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    /// Verifies that a valid local-layer POST returns a non-empty Guid identifier.
    /// </summary>
    [Fact]
    public async Task RegisterSpaceLayer_LocalLayer_ReturnsNonEmptyId()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "SL Id Facility")
            ;
        var request = new
        {
            TradeType = (int)TradeType.Electrical,
            IsExternalNode = false,
            ExternalSourceUrl = (string?)null,
            IntentDataJson = "{\"voltage\":230,\"circuitCount\":8}"
        };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
    }

    // -------------------------------------------------------------------------
    // GET /api/space-layers/{id} — happy path round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that after seeding a SpaceLayer, a GET by its id returns 200 with matching DTO fields.
    /// </summary>
    [Fact]
    public async Task GetSpaceLayerById_ExistingId_Returns200WithMatchingDto()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "SL DTO Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "SL DTO Facility")
            ;
        var layer = await DatabaseSeedHelper
            .SeedSpaceLayerAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "{\"level\":2}", TradeType.Plumbing)
            ;

        // Act
        var response = await Client
            .GetAsync($"/api/space-layers/{layer.Id.Value}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<SpaceLayerDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(layer.Id.Value, dto.Id);
        Assert.Equal(facility.Id.Value, dto.FacilityId);
        Assert.Equal(TradeType.Plumbing, dto.TradeType);
        Assert.False(dto.IsExternalNode);
    }

    // -------------------------------------------------------------------------
    // GET /api/space-layers/{id} — unknown id returns 404
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a GET with an unknown Guid returns 404 with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetSpaceLayerById_UnknownId_Returns404WithProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client
            .GetAsync($"/api/space-layers/{unknownId}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/space-layers — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a local-layer POST with invalid JSON in IntentDataJson returns 422
    /// with a field-level validation error on "IntentDataJson".
    /// Uses a non-empty but structurally invalid JSON string so that only the
    /// <c>Must(IsValidJson)</c> rule fires (avoiding duplicate-key behaviour
    /// that would occur if both <c>NotEmpty</c> and <c>Must</c> triggered simultaneously).
    /// </summary>
    [Fact]
    public async Task RegisterSpaceLayer_LocalLayerInvalidIntentJson_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "SL Validation Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "SL Validation Facility")
            ;
        var request = new
        {
            TradeType = (int)TradeType.Architecture,
            IsExternalNode = false,
            ExternalSourceUrl = (string?)null,
            IntentDataJson = "NOT_VALID_JSON"   // non-empty but invalid JSON
        };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey("IntentDataJson"),
            "Expected a validation error for the 'IntentDataJson' field.");
    }

    /// <summary>
    /// Verifies that a local-layer POST with malformed JSON (brace syntax error) also returns 422.
    /// </summary>
    [Fact]
    public async Task RegisterSpaceLayer_InvalidIntentJson_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "SL Invalid JSON Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "SL Invalid JSON Facility")
            ;
        var request = new
        {
            TradeType = (int)TradeType.Architecture,
            IsExternalNode = false,
            ExternalSourceUrl = (string?)null,
            IntentDataJson = "NOT_VALID_JSON{{{"
        };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }

    // -------------------------------------------------------------------------
    // PUT /api/space-layers/{id}/intent — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid PUT to /api/space-layers/{id}/intent updates the intent data and returns 200.
    /// </summary>
    [Fact]
    public async Task UpdateSpaceLayerIntentData_ValidJson_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "SL Update Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "SL Update Facility")
            ;
        var layer = await DatabaseSeedHelper
            .SeedSpaceLayerAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "{}", TradeType.Architecture)
            ;
        var request = new { IntentDataJson = "{\"updated\":true}" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/space-layers/{layer.Id.Value}/intent", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/space-layers/{id}/intent — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a PUT with invalid JSON returns 422 with a field-level validation error on "IntentDataJson".
    /// </summary>
    [Fact]
    public async Task UpdateSpaceLayerIntentData_InvalidJson_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "SL Intent Validation Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "SL Intent Validation Facility")
            ;
        var layer = await DatabaseSeedHelper
            .SeedSpaceLayerAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "{}", TradeType.Architecture)
            ;
        var request = new { IntentDataJson = "INVALID{{" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/space-layers/{layer.Id.Value}/intent", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey("IntentDataJson"),
            "Expected a validation error for the 'IntentDataJson' field.");
    }

    // -------------------------------------------------------------------------
    // Domain event dispatch
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a successful POST causes a SpaceLayerRegisteredEvent to be captured.
    /// </summary>
    [Fact]
    public async Task RegisterSpaceLayer_ValidRequest_DispatchesSpaceLayerRegisteredEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "SL Event Facility")
            ;
        var request = new
        {
            TradeType = (int)TradeType.Joinery,
            IsExternalNode = false,
            ExternalSourceUrl = (string?)null,
            IntentDataJson = "{\"material\":\"oak\",\"dimensions\":{\"width\":90,\"height\":210}}"
        };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/space-layers", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is SpaceLayerRegisteredEvent);
    }
}
