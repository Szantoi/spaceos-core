using System.Net;
using FluentAssertions;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// API integration tests for hardware-list-pdf and material-req-pdf endpoints.
/// Tests auth (401) and not-found (404) scenarios.
/// </summary>
[Collection("Integration")]
public sealed class HardwareMaterialPdfApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;

    public HardwareMaterialPdfApiTests(JoineryWebFactory factory) => _factory = factory;

    private HttpClient Client(string? tenantId = null) =>
        _factory.CreateAuthenticatedClient(tenantId ?? Guid.NewGuid().ToString());

    // ── hardware-list-pdf ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetHardwareListPdf_NoAuth_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/orders/{Guid.NewGuid()}/hardware-list-pdf");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHardwareListPdf_NonExistentOrder_Returns404()
    {
        var resp = await Client().GetAsync($"/api/orders/{Guid.NewGuid()}/hardware-list-pdf");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── material-req-pdf ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMaterialReqPdf_NoAuth_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/orders/{Guid.NewGuid()}/material-req-pdf");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMaterialReqPdf_NonExistentOrder_Returns404()
    {
        var resp = await Client().GetAsync($"/api/orders/{Guid.NewGuid()}/material-req-pdf");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
