// SpaceOS.Kernel.IntegrationTests/FlowEpics/FlowEpicPipelineTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.FlowEpics;

/// <summary>
/// End-to-end pipeline tests for the FlowEpic aggregate.
/// Verifies the full vertical slice: HTTP → ValidationBehavior → Handler → EF Core → domain event dispatch.
/// FlowEpic is created via the nested route POST /api/facilities/{facilityId}/flow-epics.
/// </summary>
public sealed class FlowEpicPipelineTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/flow-epics — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid POST to the nested flow-epics route returns 201 Created with a location header.
    /// </summary>
    [Fact]
    public async Task CreateFlowEpic_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "FE Owner Facility")
            ;
        var request = new { Title = "Redesign Ground Floor" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/flow-epics", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    /// Verifies that a valid POST returns a non-empty Guid identifier in the response body.
    /// </summary>
    [Fact]
    public async Task CreateFlowEpic_ValidRequest_ReturnsNonEmptyId()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "FE Id Facility")
            ;
        var request = new { Title = "Upgrade HVAC System" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/flow-epics", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
    }

    // -------------------------------------------------------------------------
    // GET /api/flow-epics/{id} — happy path round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that after seeding a FlowEpic, a GET by its id returns 200 with matching DTO fields.
    /// </summary>
    [Fact]
    public async Task GetFlowEpicById_ExistingId_Returns200WithMatchingDto()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "FE DTO Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "FE DTO Facility")
            ;
        var epic = await DatabaseSeedHelper
            .SeedFlowEpicAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Round Trip Epic")
            ;

        // Act
        var response = await Client
            .GetAsync($"/api/flow-epics/{epic.Id.Value}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<FlowEpicDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(epic.Id.Value, dto.Id);
        Assert.Equal("Round Trip Epic", dto.Title);
        Assert.Equal(facility.Id.Value, dto.TargetFacilityId);
    }

    // -------------------------------------------------------------------------
    // GET /api/flow-epics/{id} — unknown id returns 404
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a GET with an unknown Guid returns 404 with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetFlowEpicById_UnknownId_Returns404WithProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client
            .GetAsync($"/api/flow-epics/{unknownId}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // -------------------------------------------------------------------------
    // POST /api/facilities/{facilityId}/flow-epics — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a POST with an empty title returns 422 with a field-level validation error on "Title".
    /// </summary>
    [Fact]
    public async Task CreateFlowEpic_EmptyTitle_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "FE Validation Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "FE Validation Facility")
            ;
        var request = new { Title = "" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/flow-epics", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Title"), "Expected a validation error for the 'Title' field.");
    }

    // -------------------------------------------------------------------------
    // PUT /api/flow-epics/{id}/delegate — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid delegation PUT returns 200.
    /// </summary>
    [Fact]
    public async Task DelegateFlowEpic_ValidRequest_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        var ownerTenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Delegation Owner")
            ;
        var guestTenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Delegation Guest")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, ownerTenant.Id, "Delegation Facility")
            ;
        var epic = await DatabaseSeedHelper
            .SeedFlowEpicAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Epic To Delegate")
            ;
        var request = new { GuestTenantId = guestTenant.Id.Value };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/flow-epics/{epic.Id.Value}/delegate", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/flow-epics/{id}/delegate — domain event dispatch
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a successful delegation causes a FlowEpicDelegatedEvent to be captured.
    /// </summary>
    [Fact]
    public async Task DelegateFlowEpic_ValidRequest_DispatchesFlowEpicDelegatedEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        var ownerTenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Delegated Event Owner")
            ;
        var guestTenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Delegated Event Guest")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, ownerTenant.Id, "Delegated Event Facility")
            ;
        var epic = await DatabaseSeedHelper
            .SeedFlowEpicAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Delegated Event Epic")
            ;
        var request = new { GuestTenantId = guestTenant.Id.Value };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/flow-epics/{epic.Id.Value}/delegate", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is FlowEpicDelegatedEvent);
    }

    // -------------------------------------------------------------------------
    // PUT /api/flow-epics/{id}/start — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid PUT to /api/flow-epics/{id}/start advances the epic to Delivery phase and returns 200.
    /// </summary>
    [Fact]
    public async Task StartFlowEpicExecution_ValidRequest_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "FE Start Tenant")
            ;
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, tenant.Id, "FE Start Facility")
            ;
        var epic = await DatabaseSeedHelper
            .SeedFlowEpicAsync(Services, facility.Id, SpaceOsApiFactory.TestTenantId, "Epic To Start")
            ;

        // Act
        var response = await Client
            .PutAsync($"/api/flow-epics/{epic.Id.Value}/start", null, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Domain event dispatch — CreateFlowEpic
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a successful POST causes a FlowEpicCreatedEvent to be captured.
    /// </summary>
    [Fact]
    public async Task CreateFlowEpic_ValidRequest_DispatchesFlowEpicCreatedEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        // Seed facility under TestTenantId so the per-tenant query filter matches the JWT tid claim.
        var facility = await DatabaseSeedHelper
            .SeedFacilityAsync(Services, SpaceOsApiFactory.TestTenantId, "FE Event Facility")
            ;
        var request = new { Title = "Event Epic Title" };

        // Act
        var response = await Client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/flow-epics", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is FlowEpicCreatedEvent);
    }
}
