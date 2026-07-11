// SpaceOS.Kernel.IntegrationTests/Tenants/TenantPipelineTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Application.Tenants;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Tenants;

/// <summary>
/// End-to-end pipeline tests for the Tenant aggregate.
/// Verifies the full vertical slice: HTTP → ValidationBehavior → Handler → EF Core → domain event dispatch.
/// </summary>
public sealed class TenantPipelineTests : ApiTestBase
{
    // -------------------------------------------------------------------------
    // POST /api/tenants — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid POST to /api/tenants returns 201 Created with a non-empty
    /// location header and a non-empty Guid body.
    /// </summary>
    [Fact]
    public async Task CreateTenant_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange
        Factory.Capture.Reset();
        var request = new { Name = "Acme Corp" };

        // Act
        var response = await Client
            .PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    /// Verifies that a valid POST to /api/tenants returns a non-empty Guid identifier in the response body.
    /// </summary>
    [Fact]
    public async Task CreateTenant_ValidRequest_ReturnsNonEmptyId()
    {
        // Arrange
        Factory.Capture.Reset();
        var request = new { Name = "Galaxy Corp" };

        // Act
        var response = await Client
            .PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
    }

    // -------------------------------------------------------------------------
    // POST /api/tenants → GET /api/tenants/{id} — round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that after creating a tenant, a GET by its id returns 200 with matching DTO fields.
    /// </summary>
    [Fact]
    public async Task GetTenantById_AfterCreate_Returns200WithMatchingDto()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Round Trip Tenant")
            ;

        // Act
        var response = await Client
            .GetAsync($"/api/tenants/{tenant.Id.Value}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TenantDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(tenant.Id.Value, dto.Id);
        Assert.Equal("Round Trip Tenant", dto.Name);
    }

    // -------------------------------------------------------------------------
    // GET /api/tenants/{id} — unknown id returns 404
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a GET with an unknown Guid returns 404 with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetTenantById_UnknownId_Returns404WithProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client
            .GetAsync($"/api/tenants/{unknownId}", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // -------------------------------------------------------------------------
    // POST /api/tenants — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a POST with an empty name returns 422 with a field-level validation error on "Name".
    /// </summary>
    [Fact]
    public async Task CreateTenant_EmptyName_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var request = new { Name = "" };

        // Act
        var response = await Client
            .PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken)
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
    // PUT /api/tenants/{id} — happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid PUT to /api/tenants/{id} updates the tenant name and returns 200.
    /// </summary>
    [Fact]
    public async Task UpdateTenantName_ValidRequest_Returns200()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Original Name")
            ;
        var request = new { Name = "Updated Name" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/tenants/{tenant.Id.Value}", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/tenants/{id} — validation failure → 422
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a PUT with an empty name returns 422 with a field-level validation error on "NewName".
    /// </summary>
    [Fact]
    public async Task UpdateTenantName_EmptyName_Returns422WithValidationError()
    {
        // Arrange
        Factory.Capture.Reset();
        var tenant = await DatabaseSeedHelper
            .SeedTenantAsync(Services, "Tenant To Update")
            ;
        var request = new { Name = "" };

        // Act
        var response = await Client
            .PutAsJsonAsync($"/api/tenants/{tenant.Id.Value}", request, TestContext.Current.CancellationToken)
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
    /// Verifies that a successful POST to /api/tenants causes a TenantCreatedEvent to be captured.
    /// </summary>
    [Fact]
    public async Task CreateTenant_ValidRequest_DispatchesTenantCreatedEvent()
    {
        // Arrange
        Factory.Capture.Reset();
        var request = new { Name = "Event Tenant" };

        // Act
        var response = await Client
            .PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Capture.Events, e => e is TenantCreatedEvent);
    }
}
