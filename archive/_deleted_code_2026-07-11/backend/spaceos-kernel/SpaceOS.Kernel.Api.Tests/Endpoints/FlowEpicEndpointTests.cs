// SpaceOS.Kernel.Api.Tests/Endpoints/FlowEpicEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the FlowEpic GET and write endpoints.</summary>
public sealed class FlowEpicEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public FlowEpicEndpointTests()
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
    public async Task GetFlowEpicById_Returns200()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("Epic Tenant");
        var facility = Facility.Create("Epic Facility", ApiFactory.TestTenantId);
        var flowEpic = FlowEpic.Create("Office Renovation Q1", facility.Id, ApiFactory.TestTenantId);

        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.FlowEpics.Add(flowEpic);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync($"/api/flow-epics/{flowEpic.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<FlowEpicDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(flowEpic.Id.Value, dto.Id);
        Assert.Equal("Office Renovation Q1", dto.Title);
    }

    [Fact]
    public async Task GetFlowEpicById_UnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/flow-epics/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFlowEpicsByFacility_DefaultPaging_Returns200WithPagedResult()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("Epic List Tenant");
        var facility = Facility.Create("Epic List Facility", ApiFactory.TestTenantId);
        var epic1 = FlowEpic.Create("Epic List One", facility.Id, ApiFactory.TestTenantId);
        var epic2 = FlowEpic.Create("Epic List Two", facility.Id, ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.FlowEpics.AddRange(epic1, epic2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client
            .GetAsync($"/api/facilities/{facility.Id.Value}/flow-epics", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<FlowEpicDto>>(TestJsonOptions.Default, TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task CreateFlowEpic_ValidRequest_Returns201()
    {
        // Arrange — facility TenantId must match TestTenantId so the create handler resolves the correct tenant.
        var tenant = Tenant.Create("Epic Owner Tenant");
        var facility = Facility.Create("Epic Owner Facility", ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            return Task.CompletedTask;
        });
        var request = new { title = "Office Renovation Q2" };

        // Act
        var response = await _client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/flow-epics", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task DelegateFlowEpic_ValidRequest_Returns200()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var ownerTenant = Tenant.Create("Owner Tenant");
        var guestTenant = Tenant.Create("Guest Tenant");
        var facility = Facility.Create("Delegation Facility", ApiFactory.TestTenantId);
        var flowEpic = FlowEpic.Create("B2B Delegation Epic", facility.Id, ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.AddRange(ownerTenant, guestTenant);
            db.Facilities.Add(facility);
            db.FlowEpics.Add(flowEpic);
            return Task.CompletedTask;
        });
        var request = new { guestTenantId = guestTenant.Id.Value };

        // Act
        var response = await _client
            .PutAsJsonAsync($"/api/flow-epics/{flowEpic.Id.Value}/delegate", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
