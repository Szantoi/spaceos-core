// SpaceOS.Kernel.IntegrationTests/Facilities/FacilityPipelineTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Facilities;

/// <summary>
/// End-to-end pipeline tests for the Facility aggregate.
/// Verifies the full vertical slice: HTTP → ValidationBehavior → Handler → EF Core → domain event dispatch.
/// Facility is created via the nested route POST /api/tenants/{tenantId}/facilities.
/// </summary>
public sealed class FacilityPipelineTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // POST /api/tenants/{tenantId}/facilities — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid POST to the nested facility route returns 201 Created with a location header.
    /// </summary>
    [Fact]
    public async Task CreateFacility_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Facility Owner")
            ;
        var request = new { Name = "Main Building" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/tenants/{tenant.Id.Value}/facilities", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    /// Verifies that a valid POST returns a non-empty Guid identifier in the response body.
    /// </summary>
    [Fact]
    public async Task CreateFacility_ValidRequest_ReturnsNonEmptyId()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Facility Owner 2")
            ;
        var request = new { Name = "Annex Building" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/tenants/{tenant.Id.Value}/facilities", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
    }

    // -------------------------------------------------------------------------
    // GET /api/facilities/{id} — happy path round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that after seeding a facility, a GET by its id returns 200 with matching DTO fields.
    /// </summary>
    [Fact]
    public async Task GetFacilityById_ExistingId_Returns200WithMatchingDto()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "Round Trip Facility")
            ;

        // Act
        var response = await Client
            .GetAsync($"/api/facilities/{facility.Id.Value}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<FacilityDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(facility.Id.Value, dto.Id);
        Assert.Equal("Round Trip Facility", dto.Name);
        Assert.Equal(SpaceOsApiFactory.TestTenantId.Value, dto.TenantId);
    }

    // -------------------------------------------------------------------------
    // GET /api/facilities/{id} — unknown id returns 404
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a GET with an unknown Guid returns 404 with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetFacilityById_UnknownId_Returns404WithProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client
            .GetAsync($"/api/facilities/{unknownId}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // -------------------------------------------------------------------------
    // POST /api/tenants/{tenantId}/facilities — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a POST with an empty facility name returns 422 with a field-level validation error on "Name".
    /// </summary>
    [Fact]
    public async Task CreateFacility_EmptyName_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Validation Tenant")
            ;
        var request = new { Name = "" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/tenants/{tenant.Id.Value}/facilities", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Name"), "Expected a validation error for the 'Name' field.");
    }

    // -------------------------------------------------------------------------
    // PUT /api/facilities/{id} — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid PUT to /api/facilities/{id} renames the facility and returns 200.
    /// </summary>
    [Fact]
    public async Task RenameFacility_ValidRequest_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "Old Facility Name")
            ;
        var request = new { Name = "New Facility Name" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/facilities/{facility.Id.Value}", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/facilities/{id} — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a PUT with an empty name returns 422 with a field-level validation error.
    /// </summary>
    [Fact]
    public async Task RenameFacility_EmptyName_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "Facility To Rename")
            ;
        var request = new { Name = "" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/facilities/{facility.Id.Value}", request, TestContext.Current.CancellationToken)
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
    // Domain event dispatch
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a successful POST causes a FacilityCreatedEvent to be captured.
    /// </summary>
    [Fact]
    public async Task CreateFacility_ValidRequest_DispatchesFacilityCreatedEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Event Tenant")
            ;
        var request = new { Name = "Event Facility" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/tenants/{tenant.Id.Value}/facilities", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is FacilityCreatedEvent);
    }
}
