// SpaceOS.Kernel.IntegrationTests/WorkStations/WorkStationPipelineTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.WorkStations;

/// <summary>
/// End-to-end pipeline tests for the WorkStation aggregate.
/// Verifies the full vertical slice: HTTP → ValidationBehavior → Handler → EF Core → domain event dispatch.
/// WorkStation is created via the nested route POST /api/facilities/{facilityId}/work-stations.
/// </summary>
public sealed class WorkStationPipelineTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/work-stations — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid POST to the nested work-station route returns 201 Created with a location header.
    /// </summary>
    [Fact]
    public async Task RegisterWorkStation_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "WS Owner Facility")
            ;
        var request = new { Name = "Desk A1", Type = "Desk" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/work-stations", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    /// Verifies that a valid POST returns a non-empty Guid identifier in the response body.
    /// </summary>
    [Fact]
    public async Task RegisterWorkStation_ValidRequest_ReturnsNonEmptyId()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "WS Id Facility")
            ;
        var request = new { Name = "Station B2", Type = "Lab" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/work-stations", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
    }

    // -------------------------------------------------------------------------
    // GET /api/work-stations/{id} — happy path round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that after seeding a work station, a GET by its id returns 200 with matching DTO fields.
    /// </summary>
    [Fact]
    public async Task GetWorkStationById_ExistingId_Returns200WithMatchingDto()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "WS DTO Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "WS DTO Facility")
            ;
        var workStation = await DatabaseSeedHelper
            .SeedWorkStationAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Round Trip WS", "Desk")
            ;

        // Act
        var response = await Client
            .GetAsync($"/api/work-stations/{workStation.Id.Value}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkStationDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(workStation.Id.Value, dto.Id);
        Assert.Equal("Round Trip WS", dto.Name);
        Assert.Equal(facility.Id.Value, dto.FacilityId);
    }

    // -------------------------------------------------------------------------
    // GET /api/work-stations/{id} — unknown id returns 404
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a GET with an unknown Guid returns 404 with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetWorkStationById_UnknownId_Returns404WithProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client
            .GetAsync($"/api/work-stations/{unknownId}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/work-stations — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a POST with an empty work station name returns 422 with a field-level validation error on "Name".
    /// </summary>
    [Fact]
    public async Task RegisterWorkStation_EmptyName_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "WS Validation Facility")
            ;
        var request = new { Name = "", Type = "Desk" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/work-stations", request, TestContext.Current.CancellationToken)
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
    // PUT /api/work-stations/{id}/status — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid PUT to /api/work-stations/{id}/status updates the status and returns 200.
    /// </summary>
    [Fact]
    public async Task UpdateWorkStationStatus_ValidTransition_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "WS Status Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "WS Status Facility")
            ;
        var workStation = await DatabaseSeedHelper
            .SeedWorkStationAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Status WS", "Desk")
            ;
        var request = new { Status = (int)WorkStationStatus.Occupied };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/work-stations/{workStation.Id.Value}/status", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that a PUT with an out-of-range status integer returns 422 with a validation error.
    /// </summary>
    [Fact]
    public async Task UpdateWorkStationStatus_InvalidStatus_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "WS Invalid Status Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "WS Invalid Status Facility")
            ;
        var workStation = await DatabaseSeedHelper
            .SeedWorkStationAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Invalid Status WS", "Desk")
            ;
        // 999 is outside the enum range
        var request = new { Status = 999 };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/work-stations/{workStation.Id.Value}/status", request, TestContext.Current.CancellationToken)
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
    /// Verifies that a successful POST causes a WorkStationRegisteredEvent to be captured.
    /// </summary>
    [Fact]
    public async Task RegisterWorkStation_ValidRequest_DispatchesWorkStationRegisteredEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "WS Event Facility")
            ;
        var request = new { Name = "Event Station", Type = "Lab" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/work-stations", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is WorkStationRegisteredEvent);
    }
}
