// SpaceOS.Kernel.Api.Tests/Endpoints/TenantEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Tenants;
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for the Tenant GET and write endpoints.</summary>
public sealed class TenantEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises the factory and HTTP client for this test class.</summary>
    public TenantEndpointTests()
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
    public async Task GetTenantById_ExistingId_Returns200()
    {
        // Arrange
        var tenant = Tenant.Create("ACME Corp");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync($"/api/tenants/{tenant.Id.Value}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TenantDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(tenant.Id.Value, dto.Id);
        Assert.Equal("ACME Corp", dto.Name);
    }

    [Fact]
    public async Task GetTenantById_UnknownId_Returns404()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{unknownId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTenants_Returns200WithList()
    {
        // Arrange
        var tenant1 = Tenant.Create("Tenant Alpha");
        var tenant2 = Tenant.Create("Tenant Beta");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.AddRange(tenant1, tenant2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedList<TenantDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(paged);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task GetAllTenants_DefaultPaging_Returns200WithPagedResult()
    {
        // Arrange
        var t1 = Tenant.Create("Paged Alpha");
        var t2 = Tenant.Create("Paged Beta");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.AddRange(t1, t2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedList<TenantDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.True(paged.TotalCount >= 2);
        Assert.NotNull(paged.Items);
    }

    [Fact]
    public async Task GetAllTenants_PageSize1_Returns200WithOneItem()
    {
        // Arrange — seed 3 tenants to confirm pagination slices correctly
        var t1 = Tenant.Create("Slice Tenant 1");
        var t2 = Tenant.Create("Slice Tenant 2");
        var t3 = Tenant.Create("Slice Tenant 3");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.AddRange(t1, t2, t3);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client.GetAsync("/api/tenants?page=1&pageSize=1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedList<TenantDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(paged);
        Assert.Single(paged.Items);
        Assert.Equal(1, paged.Page);
        Assert.Equal(1, paged.PageSize);
        Assert.True(paged.TotalCount >= 3);
    }

    [Fact]
    public async Task GetAllTenants_PageSizeExceeds100_Returns422()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants?pageSize=101", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTenants_PageZero_Returns422()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants?page=0", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetFacilitiesByTenant_DefaultPaging_Returns200WithPagedResult()
    {
        // Arrange — both the route tenantId and facility.TenantId must equal TestTenantId.
        // The EF Core query filter restricts to TestTenantId, and the spec also filters by routeTenantId.
        // Both must be the same value for facilities to appear in the result.
        var f1 = Facility.Create("Facility One", ApiFactory.TestTenantId);
        var f2 = Facility.Create("Facility Two", ApiFactory.TestTenantId);
        await _factory.SeedAsync(db =>
        {
            db.Facilities.AddRange(f1, f2);
            return Task.CompletedTask;
        });

        // Act
        var response = await _client
            .GetAsync($"/api/tenants/{ApiFactory.TestTenantId.Value}/facilities", TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content
            .ReadFromJsonAsync<PagedList<FacilityDto>>(TestContext.Current.CancellationToken)
            ;
        Assert.NotNull(paged);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
    }

    [Fact]
    public async Task CreateTenant_ValidRequest_Returns201WithLocation()
    {
        // Arrange
        var request = new { name = "New Tenant" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, id);
        Assert.Contains(id.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateTenant_EmptyName_Returns422()
    {
        // Arrange
        var request = new { name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTenantName_ExistingId_Returns200()
    {
        // Arrange
        var tenant = Tenant.Create("Old Name");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            return Task.CompletedTask;
        });
        var request = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tenants/{tenant.Id.Value}", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTenantName_UnknownId_Returns404()
    {
        // Arrange
        var request = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateFacility_ValidRequest_Returns201()
    {
        // Arrange
        var tenant = Tenant.Create("Facility Owner");
        await _factory.SeedAsync(db =>
        {
            db.Tenants.Add(tenant);
            return Task.CompletedTask;
        });
        var request = new { name = "Main Building" };

        // Act
        var response = await _client
            .PostAsJsonAsync($"/api/tenants/{tenant.Id.Value}/facilities", request, TestContext.Current.CancellationToken)
            ;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}