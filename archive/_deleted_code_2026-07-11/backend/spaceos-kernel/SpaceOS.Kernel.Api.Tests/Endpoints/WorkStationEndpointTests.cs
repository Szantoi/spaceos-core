// SpaceOS.Kernel.Api.Tests/Endpoints/WorkStationEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the WorkStation GET and write endpoints.</summary>
public sealed class WorkStationEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public WorkStationEndpointTests()
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
    public async Task GetWorkStationById_Returns200()
    {
        // Arrange — facility/workstation TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("WS Tenant");
        var facility = Facility.Create("WS Facility", ApiFactory.TestTenantId);
        var workStation = WorkStation.Create("Station Alpha", "Electrical", facility.Id, ApiFactory.TestTenantId);

        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.WorkStations.Add(workStation);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync($"/api/work-stations/{workStation.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkStationDto>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(workStation.Id.Value, dto.Id);
        Assert.Equal("Station Alpha", dto.Name);
    }

    [Fact]
    public async Task GetWorkStationById_UnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/work-stations/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkStationsByFacility_DefaultPaging_Returns200WithPagedResult()
    {
        // Arrange — TenantId must match TestTenantId so EF Core query filters pass.
        var tenant = Tenant.Create("WS List Tenant");
        var facility = Facility.Create("WS List Facility", ApiFactory.TestTenantId);
        var ws1 = WorkStation.Create("Station List A", "Electrical", facility.Id, ApiFactory.TestTenantId);
        var ws2 = WorkStation.Create("Station List B", "Mechanical", facility.Id, ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            db.WorkStations.AddRange(ws1, ws2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client
            .GetAsync($"/api/facilities/{facility.Id.Value}/work-stations", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<WorkStationDto>>(TestJsonOptions.Default, TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task CreateWorkStation_ValidRequest_Returns201()
    {
        // Arrange — facility TenantId must match TestTenantId so the create handler resolves the correct tenant.
        var tenant = Tenant.Create("WS Owner Tenant");
        var facility = Facility.Create("WS Owner Facility", ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            db.Facilities.Add(facility);
            return Task.CompletedTask;
        });
        var request = new { name = "Station One", type = "Electrical" };

        // Act
        var response = await _client
            .PostAsJsonAsync($"/api/facilities/{facility.Id.Value}/work-stations", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}
